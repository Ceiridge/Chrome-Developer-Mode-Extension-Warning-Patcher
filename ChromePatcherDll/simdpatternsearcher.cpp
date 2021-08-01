#include "stdafx.h"
#include "patches.hpp"
#include "simdpatternsearcher.hpp"

namespace ChromePatch {
	byte* SimdPatternSearcher::SearchBytePattern(Patch& patch, byte* startAddr, size_t length) {
		// TODO
		return nullptr;
	}

	bool SimdPatternSearcher::IsCpuSupported() {
		// TODO
		return IsProcessorFeaturePresent(PF_AVX2_INSTRUCTIONS_AVAILABLE);
	}
}
