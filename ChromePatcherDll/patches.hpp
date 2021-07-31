#pragma once

namespace ChromePatch {
	struct ReadPatchResult { // Make sure to null-initialize all fields
		bool UsingWrongVersion{};
	};

	struct PatchPattern {
		std::vector<byte> pattern{};
		int searchOffset{};
	};

	struct Patch {
		std::vector<PatchPattern> patterns{};
		byte origByte{}, patchByte{};
		std::vector<int> offsets{};
		std::vector<byte> newBytes{};
		bool isSig{};
		int sigOffset{};
		bool finishedPatch{}, successfulPatch{};

		friend std::ostream& operator<<(std::ostream& os, const Patch& patch);
	};

	class Patches {
	public:
		HMODULE chromeDll{};
		std::wstring chromeDllPath{};
		std::vector<Patch> patches{};

		ReadPatchResult ReadPatchFile();
		int ApplyPatches();
	private:
		std::wstring MultibyteToWide(const std::string& str);
		std::string ReadString(std::ifstream& file);
		unsigned int ReadUInteger(std::ifstream& file);
	};
	inline Patches patches;

	class PatternSearcher {
	public:
		virtual byte* SearchBytePattern(std::vector<Patch>& patchList, byte* startAddr, size_t length) = 0;
	};
}
