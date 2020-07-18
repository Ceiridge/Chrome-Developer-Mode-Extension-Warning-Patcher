#include <Windows.h>
#include <fstream>
#include <string>
#include <vector>
#include <iostream>

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
			byte origByte, patchByte;
			file.read(reinterpret_cast<char*>(&offset), sizeof(offset));
			file.read(reinterpret_cast<char*>(&origByte), sizeof(origByte));
			file.read(reinterpret_cast<char*>(&patchByte), sizeof(patchByte));

			Patch patch{ pattern, origByte, patchByte, offset };
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
}