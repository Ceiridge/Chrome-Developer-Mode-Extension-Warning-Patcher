#include <Windows.h>
#include <fstream>
#include <string>
#include <vector>
#include <iostream>
#include <Psapi.h>

#include "patches.hpp"

namespace ChromePatch {
	ReadPatchResult Patches::ReadPatchFile() {
		static const unsigned int FILE_HEADER = 0xCE161D6E;
		static const unsigned int PATCH_HEADER = 0x8A7C5000;
		ReadPatchResult result{};

		bool firstFile = true;
	RETRY_FILE_LABEL:
		std::ifstream file(firstFile ? "ChromePatches.bin" : "..\\ChromePatches.bin", std::ios::binary);
		file.unsetf(std::ios::skipws);

		if (!file.good()) {
			if (firstFile) {
				firstFile = false;
				goto RETRY_FILE_LABEL;
			}

			throw std::exception("ChromePatches.bin file not found or not accessible");
		}

		unsigned int header = ReadUInteger(file);
		if (header != FILE_HEADER) {
			throw std::runtime_error("Invalid file: Wrong header " + std::to_string(header));
		}

		std::wstring dllPath = MultibyteToWide(ReadString(file));
		if (dllPath.compare(chromeDllPath) != 0) {
			result.UsingWrongVersion = true;
		}

		while (file.peek() != EOF) {
			unsigned int patchHeader = ReadUInteger(file);
			if (patchHeader != PATCH_HEADER) {
				throw std::runtime_error("Invalid file: Wrong patch header " + std::to_string(patchHeader));
			}

			int patternLength;
			file.read(reinterpret_cast<char*>(&patternLength), sizeof(patternLength));
			std::vector<byte> pattern;
			for (int i = 0; i < patternLength; i++) {
				byte patternByte;
				file.read(reinterpret_cast<char*>(&patternByte), sizeof(patternByte));
				pattern.push_back(patternByte);
			}

			int offset;
			byte origByte, patchByte, isSig;
			file.read(reinterpret_cast<char*>(&offset), sizeof(offset));
			file.read(reinterpret_cast<char*>(&origByte), sizeof(origByte));
			file.read(reinterpret_cast<char*>(&patchByte), sizeof(patchByte));
			file.read(reinterpret_cast<char*>(&isSig), sizeof(isSig));

			Patch patch{ pattern, origByte, patchByte, offset, isSig > 0 };
			patches.push_back(patch);
			
			std::cout << "Loaded pattern: " << pattern.size() << " " << (int)origByte << " " << (int)patchByte << " " << offset << std::endl;
		}

		file.close();
		return result;
	}

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

	std::string Patches::ReadString(std::ifstream& stream) {
		int length;
		stream.read(reinterpret_cast<char*>(&length), sizeof(length));

		std::string str(length, '\0');
		stream.read(str.data(), length);
		return str;
	}

	unsigned int Patches::ReadUInteger(std::ifstream& stream) {
		unsigned int integer;
		stream.read(reinterpret_cast<char*>(&integer), sizeof(integer));

		return _byteswap_ulong(integer);
	}

	void Patches::ApplyPatches() {
		std::cout << "Applying patches, please wait..." << std::endl;
		HANDLE proc = GetCurrentProcess();
		MODULEINFO chromeDllInfo;

		GetModuleInformation(proc, chromeDll, &chromeDllInfo, sizeof(chromeDllInfo));
		MEMORY_BASIC_INFORMATION mbi{0};

		int patchedPatches = 0;
		for (uintptr_t i = (uintptr_t)chromeDll; i < (uintptr_t)chromeDll + (uintptr_t)chromeDllInfo.SizeOfImage; i++) {
			if (VirtualQuery((LPCVOID)i, &mbi, sizeof(mbi))) {
				if (mbi.Protect & (PAGE_GUARD | PAGE_NOCACHE | PAGE_NOACCESS) || !(mbi.State & MEM_COMMIT)) {
					i += mbi.RegionSize;	
				}
				else {
					for (uintptr_t addr = (uintptr_t)mbi.BaseAddress; addr < (uintptr_t)mbi.BaseAddress + (uintptr_t)mbi.RegionSize; addr++) {
						byte byt = *reinterpret_cast<byte*>(addr);

						for (Patch& patch : patches) {
							if (patch.searchOffset == -1) {
								continue;
							}

							byte searchByte = patch.pattern[patch.searchOffset];
							if (searchByte == byt || searchByte == 0xFF) {
								patch.searchOffset++;
							}
							else {
								patch.searchOffset = 0;
							}

							if (patch.searchOffset == patch.pattern.size()) {
								uintptr_t patchAddr = addr - patch.searchOffset + patch.offset + 1;
								byte* patchByte = reinterpret_cast<byte*>(patchAddr);

								if (*patchByte == patch.origByte) {
									std::cout << "Patching byte " << (int)patch.origByte << " to " << (int)patch.patchByte << " at " << patchAddr << std::endl;

									DWORD oldProtect;
									VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_EXECUTE_READWRITE, &oldProtect);
									*patchByte = patch.patchByte;
									VirtualProtect(mbi.BaseAddress, mbi.RegionSize, oldProtect, &oldProtect);
									patch.successfulPatch = true;
								}
								else {
									std::cerr << "Byte (" << (int)*patchByte << ") not original (" << (int)patch.origByte << ") at " << patchAddr << std::endl;
								}

								patch.searchOffset = -1;

								if (patchedPatches >= patches.size()) {
									goto END_PATCH_SEARCH_LABEL;
								}
							}
						}
					}

					i = (uintptr_t)mbi.BaseAddress + mbi.RegionSize;
				}
			}
		}

	END_PATCH_SEARCH_LABEL:
		for (Patch& patch : patches) {
			if (!patch.successfulPatch) {
				std::cerr << "Couldn't patch " << patch.pattern.size() << " " << patch.offset << std::endl;
			}
		}

		CloseHandle(proc);
	}
}