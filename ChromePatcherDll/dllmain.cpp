#include "stdafx.h"
#include "patches.hpp"
#include "threads.hpp"


BOOL APIENTRY ThreadMain(LPVOID lpModule) {
	FILE* fout = nullptr;
	FILE* ferr = nullptr;
#ifdef _DEBUG
	if (AllocConsole()) {
		freopen_s(&fout, "CONOUT$", "w", stdout);
		freopen_s(&ferr, "CONOUT$", "w", stderr);
	}
#else
	char winDir[MAX_PATH];
	GetWindowsDirectoryA(winDir, MAX_PATH);
	strcat_s(winDir, "\\Temp\\ChromePatcherDll.log"); // A relative path can't be used, because it's not writable without admin rights
	char winDirErr[MAX_PATH];
	GetWindowsDirectoryA(winDirErr, MAX_PATH);
	strcat_s(winDirErr, "\\Temp\\ChromePatcherDllErr.log"); // A relative path can't be used, because it's not writable without admin rights

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

	std::wstring mutexStr = std::wstring(L"ChromeDllMutex") + std::to_wstring(GetCurrentProcessId());
	HANDLE mutex = OpenMutex(MUTEX_ALL_ACCESS, FALSE, mutexStr.c_str()); // Never allow the dll to be injected twice
	if (mutex) {
		ChromePatch::ExitMainThread(lpModule);
		return TRUE;
	}
	else {
		mutex = CreateMutex(0, FALSE, mutexStr.c_str()); // Mutex closes automatically after the process exits
	}

	ChromePatch::SuspendOtherThreads();
	HANDLE proc = GetCurrentProcess();

	bool hasFoundChrome = false;
	int attempts = 0;
	std::wregex chromeDllRegex(L"\\\\Application\\\\(?:\\d+?\\.?)+\\\\[a-zA-Z0-9-]+\\.dll");

	while (!hasFoundChrome && attempts < 30000) { // give it some attempts to find the chrome.dll module
		HMODULE modules[1024];
		DWORD cbNeeded;

		if (EnumProcessModules(proc, modules, sizeof(modules), &cbNeeded)) {
			for (int i = 0; i < (cbNeeded / sizeof(HMODULE)); i++) {
				HMODULE mod = modules[i];
				TCHAR modulePath[1024];

				if (GetModuleFileName(mod, modulePath, ARRAYSIZE(modulePath))) { // analyze the module's file path with regex
					if (std::regex_search(modulePath, chromeDllRegex)) {
						std::wcout << L"Found chrome.dll module: " << modulePath << L" with handle: " << mod << std::endl;
						
						ChromePatch::patches.chromeDll = mod;
						ChromePatch::patches.chromeDllPath = modulePath;
						hasFoundChrome = true;
					}
				}
			}
		}
		else {
			std::cerr << "Couldn't enumerate modules" << std::endl;
		}

		Sleep(1);
		attempts++;
	}
	CloseHandle(proc);

	if (!hasFoundChrome) {
		std::cerr << "Couldn't find the chrome.dll, exiting" << std::endl;
	}
	else {
		try {
			ChromePatch::ReadPatchResult readResult = ChromePatch::patches.ReadPatchFile();
			boolean patchable = false;

			if (readResult.UsingWrongVersion) {
				patchable = MessageBox(NULL, L"Your Chromium version is newer than the patch definition's version! Please reuse the patcher to prevent bugs or failing pattern searches! Do you want to patch anyway?", L"Outdated patch definitions!", MB_YESNO | MB_ICONWARNING | MB_TOPMOST | MB_SETFOREGROUND) == IDYES;
			}
			else {
				patchable = true;
			}

			if (patchable) {
				ChromePatch::patches.ApplyPatches();
			}
		}
		catch (const std::exception& ex) {
			std::cerr << "Error: " << ex.what() << std::endl;
		}
	}

	std::cout << "Unloading patcher dll and resuming threads" << std::endl;
	ChromePatch::ResumeOtherThreads();

	if (fout != nullptr)
		fclose(fout);
	if (ferr != nullptr)
		fclose(ferr);
#ifdef _DEBUG
	Sleep(5000); // Give the user some time to read
	FreeConsole();
#endif

	ChromePatch::ExitMainThread(lpModule);
	return TRUE;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
	switch (ul_reason_for_call) {
		case DLL_PROCESS_ATTACH: {
			std::wstring cmdLine = GetCommandLine();
			if (cmdLine.find(L"--type=") != std::wstring::npos) { // if it's not the parent process, exit
				ChromePatch::mainThreadHandle = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)ChromePatch::ExitMainThread, hModule, NULL, NULL);
				break;
			}

			ChromePatch::mainThreadHandle = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)ThreadMain, hModule, NULL, NULL);
			break;
		}
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
		case DLL_PROCESS_DETACH:
			break;
	}

	return TRUE;
}
