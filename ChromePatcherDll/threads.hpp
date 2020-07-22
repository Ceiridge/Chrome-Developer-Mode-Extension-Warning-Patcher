#pragma once

namespace ChromePatch {
	inline std::vector<DWORD> suspendedThreads;
	inline HANDLE mainThreadHandle;

	void SuspendOtherThreads();
	void ResumeOtherThreads();
}