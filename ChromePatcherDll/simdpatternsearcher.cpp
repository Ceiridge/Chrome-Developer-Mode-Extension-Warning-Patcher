#include "stdafx.h"
#include "patches.hpp"
#include "simdpatternsearcher.hpp"

namespace ChromePatch {
	constexpr int SIMD_BYTE_COUNT = 32; // 256 bit

	// Inspired by http://0x80.pl/articles/simd-strfind.html
	byte* SimdPatternSearcher::SearchBytePattern(Patch& patch, byte* startAddr, const size_t length) {
		for (PatchPattern& pattern : patch.patterns) {
			const size_t patternSize = pattern.pattern.size();
			const __m256i firstByte = _mm256_set1_epi8(pattern.pattern[0]); // Set first __m256i to the first byte
			const __m256i lastByte = _mm256_set1_epi8(pattern.pattern[patternSize - 1]); // Set first __m256i to the last byte of the pattern
			
			for (size_t i = 0; i < length; i += SIMD_BYTE_COUNT) {
				const size_t lastBytesAdd = i + patternSize - 1;
				if (lastBytesAdd + SIMD_BYTE_COUNT > length) {  // Prevent access violations
					return nullptr;
				}
				
				const __m256i firstBytes = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(startAddr + i));
				const __m256i lastBytes = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(startAddr + lastBytesAdd));

				const __m256i equalFirst = _mm256_cmpeq_epi8(firstByte, firstBytes);
				const __m256i equalLast = _mm256_cmpeq_epi8(lastByte, lastBytes);

				int equalityMask = _mm256_movemask_epi8(_mm256_and_si256(equalFirst, equalLast)); // AND operation -> save FFs of the __mm256i into mask

				while(equalityMask) { // Manually compare if the first and last byte were equal
					DWORD bitPos;
					_BitScanForward(&bitPos, equalityMask);

					pattern.searchOffset = 1; // Skip first byte, because it has already been compared
					for(size_t x = i + bitPos + 1; x < i + bitPos + patternSize - 1; x++) { // Also subtract one byte; same reason
						const byte patternByte = pattern.pattern[pattern.searchOffset];
						if(patternByte == 0xFF || startAddr[x] == patternByte) {
							pattern.searchOffset++;
						} else {
							pattern.searchOffset = 0;
						}

						if (pattern.searchOffset == patternSize - 1) {
							return startAddr + x - pattern.searchOffset + 1;
						}
					}

					equalityMask &= equalityMask - 1; // Sets left-most bit to 0
				}
			}
		}
		
		return nullptr;
	}

	bool SimdPatternSearcher::IsCpuSupported() {
		return IsProcessorFeaturePresent(PF_AVX_INSTRUCTIONS_AVAILABLE) && IsProcessorFeaturePresent(PF_AVX2_INSTRUCTIONS_AVAILABLE);
	}
}
