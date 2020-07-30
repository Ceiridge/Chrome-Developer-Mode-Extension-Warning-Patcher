#include "stdafx.h"
#include "dllmain.hpp"

#define ERROR_EXIT(err) std::cerr << err << std::endl; if(IsDebuggerPresent()) {DebugBreak();} return 1;

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
		ERROR_EXIT("Process " << GetCurrentProcessId() << " wrongfully started (Mutex found!)");
	}
	else {
		mutex = CreateMutex(0, FALSE, L"ChromeDllInjectorMutex");
	}

	// Get the path of the exe and search for the latest ChromePatcherDll.dll
	HMODULE main = GetModuleHandle(NULL);
	WCHAR mainPath[1024];
	GetModuleFileName(main, mainPath, 1023);

	std::filesystem::path latestPath;
	int64_t bestTime = 0;
	for (const auto& file : std::filesystem::directory_iterator(std::filesystem::path(mainPath).parent_path())) {
		auto& path = file.path();
		if (file.exists() && file.is_regular_file() && path.filename().wstring().find(L"ChromePatcherDll_") != std::wstring::npos && path.has_extension() && path.extension().compare(L".dll") == 0) { // ChromePatcherDll_*.dll
			int64_t modDate = std::chrono::duration_cast<std::chrono::milliseconds>(file.last_write_time().time_since_epoch()).count(); // Sort for the latest dll file
			if (modDate > bestTime) {
				bestTime = modDate;
				latestPath = path;
			}
		}
	}

	if (bestTime == 0) {
		ERROR_EXIT("No latest path found!");
	}
	std::cout << "Latest path: " << latestPath.string() << std::endl;

	HMODULE patcherDll = LoadLibrary(latestPath.wstring().c_str());
	if (!patcherDll) {
		ERROR_EXIT("Library not loaded");
	}

	decltype(InstallWinHook)* installWinHook = reinterpret_cast<decltype(InstallWinHook)*>(GetProcAddress(patcherDll, "InstallWinHook"));
	if (!installWinHook) {
		ERROR_EXIT("InstallWinHook not found");
	}

	if (!installWinHook()) {
		ERROR_EXIT("Couldn't set winhook");
	}
	else {
		std::cout << "Set winhook" << std::endl;
	}

	while (true) {
		Sleep(10000);
	}

	return 0;
}
