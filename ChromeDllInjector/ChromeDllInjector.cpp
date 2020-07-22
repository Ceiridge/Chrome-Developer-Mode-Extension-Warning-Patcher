#include "stdafx.h"
#include "dllmain.hpp"

int wmain(int argc, wchar_t* argv[], wchar_t* envp[]) {
	FILE* fout = nullptr;
	FILE* ferr = nullptr;
#ifndef _DEBUG // Hide the injector console and write the output to a file instead
	FreeConsole();

	char winDir[MAX_PATH];
	GetWindowsDirectoryA(winDir, MAX_PATH);
	strcat_s(winDir, "\\Temp\\ChromePatcherInjector.log"); // A relative path can't be used, because it's not writable without admin rights
	char winDirErr[MAX_PATH];
	GetWindowsDirectoryA(winDirErr, MAX_PATH);
	strcat_s(winDirErr, "\\Temp\\ChromePatcherInjectorErr.log"); // A relative path can't be used, because it's not writable without admin rights

	if (fopen_s(&fout, winDir, "a") == 0) {
		fclose(fout);
		freopen_s(&fout, winDir, "a", stdout);
	}
	if (fopen_s(&ferr, winDirErr, "a") == 0) {
		fclose(ferr);
		freopen_s(&ferr, winDirErr, "a", stderr);
	}
#endif
	std::cout << std::time(0) << std::endl; // Log time for debug purposes
	std::cerr << std::time(0) << std::endl;

	HANDLE mutex = OpenMutex(MUTEX_ALL_ACCESS, FALSE, L"ChromeDllInjectorMutex"); // Never allow two injectors
	if (mutex) {
		std::cerr << "Process " << GetCurrentProcessId() << " wrongfully started (Mutex found!)" << std::endl;
		return 2;
	}
	else {
		mutex = CreateMutex(0, FALSE, L"ChromeDllInjectorMutex");
	}

	if (!InstallWinHook()) {
		std::cerr << "Couldn't set winhook" << std::endl;
	}
	else {
		std::cout << "Set winhook" << std::endl;
	}

	while (true) {
		Sleep(10000);
	}
	return 0;
}
