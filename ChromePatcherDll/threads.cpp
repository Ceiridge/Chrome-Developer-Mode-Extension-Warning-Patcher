#include "stdafx.h"
#include "threads.hpp"

namespace ChromePatch {
	// Partially taken from https://stackoverflow.com/questions/16684245/can-i-suspend-a-process-except-one-thread
	void SuspendOtherThreads() {
		DWORD pid = GetCurrentProcessId();
		HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
		DWORD mine = GetCurrentThreadId();

		if (snap != INVALID_HANDLE_VALUE) {
			THREADENTRY32 te;
			te.dwSize = sizeof(te);

			if (Thread32First(snap, &te)) {
				do {
					if (te.dwSize >= FIELD_OFFSET(THREADENTRY32, th32OwnerProcessID) + sizeof(te.th32OwnerProcessID)) {
						if (te.th32ThreadID != mine && te.th32OwnerProcessID == pid)
						{
							HANDLE thread = OpenThread(THREAD_ALL_ACCESS, FALSE, te.th32ThreadID);
							if (thread && thread != INVALID_HANDLE_VALUE) {
								SuspendThread(thread);
								CloseHandle(thread);
								suspendedThreads.push_back(te.th32ThreadID);
							}
						}
					}
					te.dwSize = sizeof(te);
				} while (Thread32Next(snap, &te));
			}
			CloseHandle(snap);
		}
	}

	void ResumeOtherThreads() {
		for (DWORD tId : suspendedThreads) {
			HANDLE thread = OpenThread(THREAD_ALL_ACCESS, FALSE, tId);
			if (thread && thread != INVALID_HANDLE_VALUE) {
				ResumeThread(thread);
				CloseHandle(thread);
			}
		}

		suspendedThreads.clear();
	}
}
