#pragma once

namespace ChromePatch {
	class
#ifdef _DEBUG
	__declspec(dllexport)
#endif
	SimplePatternSearcher : PatternSearcher {
	public:
		byte* SearchBytePattern(std::vector<Patch>& patchList, byte* startAddr, size_t length) override;
	};
}
