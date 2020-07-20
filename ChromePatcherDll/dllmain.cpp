#include <Windows.h>
#include <Psapi.h>
#include <iostream>
#include <regex>
#include <string>

#include "patches.hpp"
#include "threads.hpp"

BOOL APIENTRY ThreadMain(LPVOID lpModule) {
	// Taken from https://stackoverflow.com/questions/15543571/allocconsole-not-displaying-cout
//#ifdef _DEBUG
	FILE* fout = nullptr;
	FILE* ferr = nullptr;
	HANDLE hConOut = NULL;

	if (AllocConsole()) {
		freopen_s(&fout, "CONOUT$", "w", stdout);
		freopen_s(&ferr, "CONOUT$", "w", stderr);
		std::cout.clear();
		std::clog.clear();
		std::cerr.clear();

		// std::wcout, std::wclog, std::wcerr, std::wcin
		hConOut = CreateFile(L"CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		SetStdHandle(STD_OUTPUT_HANDLE, hConOut);
		SetStdHandle(STD_ERROR_HANDLE, hConOut);
		std::wcout.clear();
		std::wclog.clear();
		std::wcerr.clear();
	}
//#endif
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
	Sleep(5000); // Give the user some time to read

//#ifdef _DEBUG
	if (fout != nullptr)
		fclose(fout);
	if (ferr != nullptr)
		fclose(ferr);
	if (hConOut != NULL)
		CloseHandle(hConOut);
	FreeConsole();
//#endif

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
