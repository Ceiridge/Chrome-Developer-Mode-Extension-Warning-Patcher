#include "stdafx.h"
#include "patches.hpp"
#include "simplepatternsearcher.hpp"
#include "simdpatternsearcher.hpp"
#include "threads.hpp"

#define ReadVar(variable) file.read(reinterpret_cast<char*>(&variable), sizeof(variable)); // Makes everything easier to read

namespace ChromePatch {
	std::ostream& operator<<(std::ostream& os, const Patch& patch) { // Write identifiable data to the output stream for debugging
		const PatchPattern& firstPattern = patch.patterns[0];

		os << "(First Pattern: " << std::hex;
		for (byte b : firstPattern.pattern) {
			os << std::setw(2) << std::setfill('0') << (int)b << " ";
		}

		os << "with PatchByte " << static_cast<int>(patch.patchByte) << ")" << std::dec;
		return os;
	}


	ReadPatchResult Patches::ReadPatchFile() {
		static const unsigned int FILE_HEADER = 0xCE161D6E; // Magic values
		static const unsigned int PATCH_HEADER = 0x8A7C5000;
		ReadPatchResult result{};

		bool firstFile = true;
	RETRY_FILE_LABEL:
		std::ifstream file(firstFile ? "ChromePatches.bin" : "..\\ChromePatches.bin", std::ios::binary);
		file.unsetf(std::ios::skipws); // Disable skipping of leading whitespaces while reading

		if (!file.good()) {
			if (firstFile) {
				firstFile = false;
				goto RETRY_FILE_LABEL; // Retry once if the file was not found
			}

			throw std::exception("ChromePatches.bin file not found or not accessible");
		}

		unsigned int header = ReadUInteger(file);
		if (header != FILE_HEADER) {
			throw std::runtime_error("Invalid file: Wrong header " + std::to_string(header));
		}

		std::wstring dllPath = MultibyteToWide(ReadString(file));
		if (dllPath != chromeDllPath) {
			result.UsingWrongVersion = true;
		}

		while (file.peek() != EOF) { // For each patch
			unsigned int patchHeader = ReadUInteger(file);
			if (patchHeader != PATCH_HEADER) {
				throw std::runtime_error("Invalid file: Wrong patch header " + std::to_string(patchHeader));
			}

			std::vector<PatchPattern> patterns;
			int patternsSize;
			ReadVar(patternsSize);
			for (int i = 0; i < patternsSize; i++) { // For each pattern, read its values
				int patternLength;
				ReadVar(patternLength);
				std::vector<byte> pattern;

				for (int i = 0; i < patternLength; i++) {
					byte patternByte;
					ReadVar(patternByte);
					pattern.push_back(patternByte);
				}

				patterns.push_back(PatchPattern{ pattern });
			}

			int offsetCount; // Read the Offsets list
			ReadVar(offsetCount);
			std::vector<int> offsets;
			for (int i = 0; i < offsetCount; i++) {
				int offset;
				ReadVar(offset);
				offsets.push_back(offset);
			}

			int newBytesCount; // Read the NewBytes array
			ReadVar(newBytesCount);
			std::vector<byte> newBytes;
			for(int i = 0; i < newBytesCount; i++) {
				byte newByte;
				ReadVar(newByte);
				newBytes.push_back(newByte);
			}

			int sigOffset; // Read the rest of the data
			byte origByte, patchByte, isSig;
			ReadVar(origByte);
			ReadVar(patchByte);
			ReadVar(isSig);
			ReadVar(sigOffset);

			Patch patch{ patterns, origByte, patchByte, offsets, newBytes, isSig > 0, sigOffset };
			patches.push_back(patch);
			
			std::cout << "Loaded patch: " << patch << std::endl;
		}

		file.close();
		return result;
	}

	// Convert a UTF8 string to a UTF16LE string
	std::wstring Patches::MultibyteToWide(const std::string& str) {
		if (str.empty()) {
			return std::wstring();
		}

		const size_t len = MultiByteToWideChar(CP_UTF8, NULL, str.c_str(), str.length(), nullptr, 0);
		std::wstring result(len, '\0');

		if (len > 0) {
			if (MultiByteToWideChar(CP_UTF8, NULL, str.c_str(), str.length(), result.data(), len)) {
				return result;
			}
		}

		return std::wstring();
	}

	std::string Patches::ReadString(std::ifstream& file) {
		int length;
		ReadVar(length);

		std::string str(length, '\0');
		file.read(str.data(), length);
		return str;
	}

	unsigned int Patches::ReadUInteger(std::ifstream& file) {
		unsigned int integer;
		ReadVar(integer);

		return _byteswap_ulong(integer); // Convert to Big Endian (for the magic values)
	}


	int Patches::ApplyPatches() {
		std::unique_ptr<PatternSearcher> patternSearcher;
		const bool simdCpuSupport = SimdPatternSearcher::IsCpuSupported();
		std::cout << "SIMD support: " << simdCpuSupport << std::endl;
		
		if(simdCpuSupport) {
			patternSearcher = std::make_unique<SimdPatternSearcher>();
		} else {
			patternSearcher = std::make_unique<SimplePatternSearcher>();
		}
		
		int successfulPatches = 0;
		std::vector<std::thread> patchThreads;
		std::cout << "Applying patches, please wait..." << std::endl;
		const HANDLE proc = GetCurrentProcess();
		MODULEINFO chromeDllInfo;

		GetModuleInformation(proc, chromeDll, &chromeDllInfo, sizeof(chromeDllInfo));
		MEMORY_BASIC_INFORMATION mbi{};

		for (uintptr_t i = (uintptr_t)chromeDll; i < (uintptr_t)chromeDll + (uintptr_t)chromeDllInfo.SizeOfImage; i++) {
			if (VirtualQuery((LPCVOID)i, &mbi, sizeof(mbi))) {
				if (mbi.Protect & (PAGE_GUARD | PAGE_NOCACHE | PAGE_NOACCESS) || !(mbi.State & MEM_COMMIT) || !(mbi.Protect & (PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE))) {
					i += mbi.RegionSize; // Skip these regions
				} else {
					for (Patch& patch : patches) {
						if (patch.finishedPatch) {
							continue;
						}

						std::thread patchThread(PatchThreadDelegate, &patch, patternSearcher.get(), &mbi);
						patchThreads.push_back(std::move(patchThread));
					}

					for (std::thread& patchThread : patchThreads) { // Make sure all threads have executed in this memory region
						// I cannot use patchThread.join here, because it causes deadlocks
						HANDLE patchHandle = patchThread.native_handle();
						const time_t waitTime = std::time(nullptr);
						
						while(WaitForSingleObject(patchHandle, 0) != WAIT_OBJECT_0) {
							// Active waiting required to prevent deadlocks
							if(waitTime + (simdCpuSupport ? 1 : 5) < std::time(nullptr)) { // 1 or 5 seconds timeout
								std::cout << "Patch Thread " << patchHandle << " timeouted! Resuming all other threads. (This is a race condition now)" << std::endl;
								ResumeOtherThreads();
							}
						}

						patchThread.detach();
					}
					patchThreads.clear();
					
					i = (uintptr_t)mbi.BaseAddress + mbi.RegionSize; // Skip to the next region after this one has been searched
				}
			}
		}
		
		for (Patch& patch : patches) {
			if (!patch.successfulPatch) {
				std::cerr << "Couldn't patch " << patch << std::endl;
			} else {
				successfulPatches++;
			}
		}

		CloseHandle(proc);
		return successfulPatches;
	}

	void Patches::PatchThreadDelegate(Patch* patch, PatternSearcher* patternSearcher, MEMORY_BASIC_INFORMATION* mbi) {
		byte* searchResult = patternSearcher->SearchBytePattern(*patch, static_cast<byte*>(mbi->BaseAddress), mbi->RegionSize);
		if (!searchResult) { // is null
			return;
		}

		int offsetAttempt = 0;
		while (!patch->successfulPatch) {
			byte* patchAddr = searchResult + patch->offsets[offsetAttempt];
			std::cout << "Reading address " << std::hex << (uintptr_t)patchAddr << std::endl;

			if (patch->isSig) { // Add the offset found at the patchAddr (with a 4 byte rel. addr. offset) to the patchAddr
				patchAddr += *reinterpret_cast<int*>(patchAddr) + 4 + patch->sigOffset;
				std::cout << "New aftersig address: " << std::hex << (uintptr_t)patchAddr << std::endl;
			}

			if (patch->origByte == 0xFF || *patchAddr == patch->origByte) {
				std::cout << "Patching byte " << std::hex << (int)*patchAddr << " to " << (int)patch->patchByte << " at " << (uintptr_t)patchAddr << std::endl;
				DWORD oldProtect;
				VirtualProtect(mbi->BaseAddress, mbi->RegionSize, PAGE_EXECUTE_READWRITE, &oldProtect);

				if (patch->newBytes.empty()) { // Patch a single byte
					*patchAddr = patch->patchByte;
				}
				else { // Write the newBytes array if it is filled instead
					const size_t newBytesSize = patch->newBytes.size();
					memcpy_s(patchAddr, newBytesSize, patch->newBytes.data(), newBytesSize);

					std::cout << newBytesSize << " NewBytes have been written" << std::endl;
				}

				VirtualProtect(mbi->BaseAddress, mbi->RegionSize, oldProtect, &oldProtect);
				patch->successfulPatch = true;
			}
			else {
				offsetAttempt++;
				std::cerr << "Byte (" << std::hex << (int)*patchAddr << ") not original (" << (int)patch->origByte << ") at " << (uintptr_t)patchAddr << std::endl;

				if (offsetAttempt == patch->offsets.size()) {
					break; // Abort trying out offsets if none worked
				}
			}
		}

		patch->finishedPatch = true;
	}

}
