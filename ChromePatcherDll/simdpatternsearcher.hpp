#pragma once

namespace ChromePatch {
	class
#ifdef _DEBUG
		__declspec(dllexport)
#endif
	SimdPatternSearcher : public PatternSearcher {
	public:
		byte* SearchBytePattern(Patch& patch, byte* startAddr, size_t length) override;
		static bool IsCpuSupported();
	private:
		static int CountTrailingZeros(int i);
	};
}
