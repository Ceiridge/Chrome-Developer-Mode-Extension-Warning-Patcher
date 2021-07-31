#include "pch.h"
#include "CppUnitTest.h"
#include "patches.hpp"
#include "simplepatternsearcher.hpp"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace ChromePatch{
	TEST_CLASS(ChromePatcherDllPatternTests) { // Test the pattern searchers
	public:
		TEST_METHOD(SimplePatternSearcherTest) {
			auto simpleSearcher = std::make_unique<SimplePatternSearcher>();

			
		}
	};
}
