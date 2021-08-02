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
		static std::wstring MultibyteToWide(const std::string& str);
		static std::string ReadString(std::ifstream& file);
		static unsigned int ReadUInteger(std::ifstream& file);
	};
	inline Patches patches;

	class PatternSearcher {
	public:
		virtual byte* SearchBytePattern(Patch& patch, byte* startAddr, size_t length) = 0;
	};
}
