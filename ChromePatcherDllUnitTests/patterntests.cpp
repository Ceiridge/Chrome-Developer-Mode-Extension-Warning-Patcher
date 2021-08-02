#include "pch.h"
#include "CppUnitTest.h"
#include "patches.hpp"
#include "simplepatternsearcher.hpp"
#include "simdpatternsearcher.hpp"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace ChromePatch {
	byte SearchPattern1[10] = {0xAB, 0xCD, 0xEF, 0x00, 0xCE, 0x16, 0x1D, 0x6E, 0xFF, 0x02}; // Example bytes to search for
	byte SearchPattern2[5] = { 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };
	byte SearchPatternNowhere[2] = { 0xAA, 0xAA};

	struct TestBytes {
		byte* Bytes;
		size_t BytesLength;
		
		byte* Pattern1Ptr;
		byte* Pattern2Ptr;

		~TestBytes() {
			delete[] this->Bytes;
		}
	};

	constexpr size_t BYTE_ARRAY_SIZE = 150 * 1000 * 1000; // 150 MB
	constexpr size_t PATTERN1_OFFSET = BYTE_ARRAY_SIZE >> 1; // Divide by 2
	constexpr size_t PATTERN2_OFFSET = (BYTE_ARRAY_SIZE >> 1) - (BYTE_ARRAY_SIZE >> 2); // *0.25

	TEST_CLASS(ChromePatcherDllPatternTests) { // Test the pattern searchers
	public:
		TEST_METHOD(SimplePatternSearcherTest) {
			const TestBytes testBytes = CreateTestBytes();
			Assert::AreEqual(BYTE_ARRAY_SIZE, testBytes.BytesLength);
			
			auto simpleSearcher = std::make_unique<SimplePatternSearcher>();
			std::vector<Patch> createdPatches = CreatePatches();
			
			Assert::AreEqual(testBytes.Pattern1Ptr, simpleSearcher->SearchBytePattern(createdPatches[0], testBytes.Bytes, testBytes.BytesLength));
			Assert::AreEqual(testBytes.Pattern2Ptr, simpleSearcher->SearchBytePattern(createdPatches[1], testBytes.Bytes, testBytes.BytesLength));
		}

		TEST_METHOD(SimdPatternSearcherTest) {
			const TestBytes testBytes = CreateTestBytes();
			Assert::AreEqual(BYTE_ARRAY_SIZE, testBytes.BytesLength);

			auto simdSearcher = std::make_unique<SimdPatternSearcher>();
			std::vector<Patch> createdPatches = CreatePatches();

			Assert::AreEqual(testBytes.Pattern1Ptr, simdSearcher->SearchBytePattern(createdPatches[0], testBytes.Bytes, testBytes.BytesLength));
			Assert::AreEqual(testBytes.Pattern2Ptr, simdSearcher->SearchBytePattern(createdPatches[1], testBytes.Bytes, testBytes.BytesLength));
		}

		TEST_METHOD(SimdCpuSupport) {
			Assert::IsTrue(SimdPatternSearcher::IsCpuSupported());
		}

	private:
		// Create two patches: Patch 1 has 2 patterns, of which one has a result. Patch 2 has 1 pattern with a result, but with offsets.
		static std::vector<Patch> CreatePatches() {
			std::vector<Patch> patchList;

			const std::vector<byte> pattern1Vec(SearchPattern1, SearchPattern1 + ARRAYSIZE(SearchPattern1));
			const std::vector<byte> patternNVec(SearchPatternNowhere, SearchPatternNowhere + ARRAYSIZE(SearchPatternNowhere));
			patchList.push_back(Patch{ {{patternNVec}, {pattern1Vec}}, 0, 0 }); // Constructor hell
			
			const std::vector<byte> pattern2Vec(SearchPattern2, SearchPattern2 + ARRAYSIZE(SearchPattern2));
			const std::vector<int> pattern2Offsets = { 7 };
			patchList.push_back(Patch{ {{pattern2Vec}}, 0, 0, pattern2Offsets});

			return patchList;
		}

		static TestBytes CreateTestBytes() {				
			byte* byteArray = new byte[BYTE_ARRAY_SIZE];
			memset(byteArray, '\0', BYTE_ARRAY_SIZE);
			
			byte* pattern1Ptr = byteArray + PATTERN1_OFFSET;
			byte* pattern2Ptr = byteArray + PATTERN2_OFFSET;
			
			memcpy_s(pattern1Ptr, ARRAYSIZE(SearchPattern1), SearchPattern1, ARRAYSIZE(SearchPattern1));
			memcpy_s(pattern2Ptr, ARRAYSIZE(SearchPattern2), SearchPattern2, ARRAYSIZE(SearchPattern2));

			Assert::IsTrue(byteArray[0] == 0);
			Assert::IsTrue(pattern1Ptr[0] == SearchPattern1[0]);

			return TestBytes{ byteArray, BYTE_ARRAY_SIZE, pattern1Ptr, pattern2Ptr };
		}
	};
}
