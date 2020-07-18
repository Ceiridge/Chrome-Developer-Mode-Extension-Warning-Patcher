#include <Windows.h>
#include <Psapi.h>
#include <iostream>
#include <regex>
#include <string>
#include <vector>
#include <tlhelp32.h>

#include "patches.hpp"

HANDLE mainThreadHandle;
HMODULE dllModule;
std::vector<DWORD> suspendedThreads;

BOOL APIENTRY ExitMainThread(LPVOID lpModule) {
	CloseHandle(mainThreadHandle);
	FreeLibraryAndExitThread(dllModule, 0);
}

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

BOOL APIENTRY ThreadMain(LPVOID lpModule) {
	// Taken from https://stackoverflow.com/questions/15543571/allocconsole-not-displaying-cout
//#ifdef _DEBUG
	FILE* fout = nullptr;
	FILE* ferr = nullptr;
	FILE* fin = nullptr;
	HANDLE hConOut = NULL, hConIn = NULL;

	if (AllocConsole()) {
		freopen_s(&fout, "CONOUT$", "w", stdout);
		freopen_s(&ferr, "CONOUT$", "w", stderr);
		freopen_s(&fin, "CONIN$", "r", stdin);
		std::cout.clear();
		std::clog.clear();
		std::cerr.clear();
		std::cin.clear();

		// std::wcout, std::wclog, std::wcerr, std::wcin
		hConOut = CreateFile(L"CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		hConIn = CreateFile(L"CONIN$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		SetStdHandle(STD_OUTPUT_HANDLE, hConOut);
		SetStdHandle(STD_ERROR_HANDLE, hConOut);
		SetStdHandle(STD_INPUT_HANDLE, hConIn);
		std::wcout.clear();
		std::wclog.clear();
		std::wcerr.clear();
		std::wcin.clear();
	}
//#endif
	SuspendOtherThreads();
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
	ResumeOtherThreads();
	Sleep(5000); // Give the user some time to read

//#ifdef _DEBUG
	if (fout != nullptr)
		fclose(fout);
	if (ferr != nullptr)
		fclose(ferr);
	if (fin != nullptr)
		fclose(fin);
	if (hConOut != NULL)
		CloseHandle(hConOut);
	if (hConIn != NULL)
		CloseHandle(hConIn);
	FreeConsole();
//#endif

	ExitMainThread(lpModule);
	return TRUE;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
	switch (ul_reason_for_call) {
		case DLL_PROCESS_ATTACH: {
			dllModule = hModule;

			std::wstring cmdLine = GetCommandLine();
			if (cmdLine.find(L"--type=") != std::wstring::npos) { // if it's not the parent process, exit
				mainThreadHandle = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)ExitMainThread, hModule, NULL, NULL);
				break;
			}

			mainThreadHandle = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)ThreadMain, hModule, NULL, NULL);
			break;
		}
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
		case DLL_PROCESS_DETACH:
			break;
	}

	return TRUE;
}
