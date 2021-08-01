#include "stdafx.h"
#include "patches.hpp"
#include "simplepatternsearcher.hpp"

namespace ChromePatch {
	byte* SimplePatternSearcher::SearchBytePattern(Patch& patch, byte* startAddr, size_t length) {
		for(size_t i = 0; i < length; i++) {		
			for (PatchPattern& pattern : patch.patterns) {
				const byte searchByte = pattern.pattern[pattern.searchOffset];
				if (searchByte == startAddr[i] || searchByte == 0xFF) {
					pattern.searchOffset++;
				} else {
					pattern.searchOffset = 0; // Reset found offsets if the byte differs from the pattern
				}

				if (pattern.searchOffset == pattern.pattern.size()) {
					return startAddr + i - pattern.searchOffset + 1; // Pattern found
				}
			}

		}
		
		return nullptr;
	}

}
