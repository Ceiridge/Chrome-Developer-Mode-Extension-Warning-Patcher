#include "stdafx.h"
#include "patches.hpp"
#include "simplepatternsearcher.hpp"

#define ReadVar(variable) file.read(reinterpret_cast<char*>(&variable), sizeof(variable)); // Makes everything easier to read

namespace ChromePatch {
	std::ostream& operator<<(std::ostream& os, const Patch& patch) { // Write identifiable data to the output stream for debugging
		const PatchPattern& firstPattern = patch.patterns[0];

		os << "First Pattern: " << std::hex;
		for (byte b : firstPattern.pattern) {
			os << std::setw(2) << std::setfill('0') << (int)b << " ";
		}

		os << " with PatchByte " << static_cast<int>(patch.patchByte) << std::dec;
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

		int len = MultiByteToWideChar(CP_UTF8, NULL, str.c_str(), str.length(), NULL, 0);
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


	auto simpleSearcher = std::make_unique<SimplePatternSearcher>();
	
	// TODO: Externalize this function in different implementations (traditional and with SIMD support) and add multithreading
	int Patches::ApplyPatches() {
		int successfulPatches = 0;
		std::cout << "Applying patches, please wait..." << std::endl;
		HANDLE proc = GetCurrentProcess();
		MODULEINFO chromeDllInfo;

		GetModuleInformation(proc, chromeDll, &chromeDllInfo, sizeof(chromeDllInfo));
		MEMORY_BASIC_INFORMATION mbi{};

		int patchedPatches = 0;
		for (uintptr_t i = (uintptr_t)chromeDll; i < (uintptr_t)chromeDll + (uintptr_t)chromeDllInfo.SizeOfImage; i++) {
			if (VirtualQuery((LPCVOID)i, &mbi, sizeof(mbi))) {
				if (mbi.Protect & (PAGE_GUARD | PAGE_NOCACHE | PAGE_NOACCESS) || !(mbi.State & MEM_COMMIT)) {
					i += mbi.RegionSize; // Skip these regions
				} else {
					for (Patch& patch : patches) {
						if (patch.finishedPatch) {
							continue;
						}

						byte* searchResult = simpleSearcher->SearchBytePattern(patch, static_cast<byte*>(mbi.BaseAddress), mbi.RegionSize);

						int offsetAttempt = 0;
						while(!patch.successfulPatch) {
							byte* patchAddr = searchResult + patch.offsets[offsetAttempt];
							std::cout << "Reading address " << std::hex << patchAddr << std::endl;


							if(patch.isSig) { // Add the offset found at the patchAddr (with a 4 byte rel. addr. offset) to the patchAddr
								patchAddr += *reinterpret_cast<int*>(patchAddr) + 4 + patch.sigOffset;
								std::cout << "New aftersig address: " << std::hex << patchAddr << std::endl;
							}

							if(patch.origByte == 0xFF || *patchAddr == patch.origByte) {
								std::cout << "Patching byte " << std::hex << (int)*patchAddr << " to " << (int)patch.patchByte << " at " << patchAddr << std::endl;
								DWORD oldProtect;
								VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_EXECUTE_READWRITE, &oldProtect);

								if (patch.newBytes.empty()) { // Patch a single byte
									*patchAddr = patch.patchByte;
								} else { // Write the newBytes array if it is filled instead
									const size_t newBytesSize = patch.newBytes.size();
									memcpy_s(patchAddr, newBytesSize, patch.newBytes.data(), newBytesSize);

									std::cout << newBytesSize << " NewBytes have been written" << std::endl;
								}
								
								VirtualProtect(mbi.BaseAddress, mbi.RegionSize, oldProtect, &oldProtect);
								patch.successfulPatch = true;
							} else {
								offsetAttempt++;
								std::cerr << "Byte (" << std::hex << (int)*patchAddr << ") not original (" << (int)patch.origByte << ") at " << patchAddr << std::endl;
								
								if(offsetAttempt == patch.offsets.size()) {
									break; // Abort trying out offsets if all didn't work
								}
							}
						}
						
						patch.finishedPatch = true;
						patchedPatches++;
					}

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
}
