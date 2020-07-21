#include "stdafx.h"
#include "apc.hpp"
#include "oplock.hpp"
#include "injector.hpp"

std::vector<ChromePatch::Oplock::Oplock*> chromeDllLocks;

void OplockCallback(ChromePatch::Oplock::Oplock* oplock) {
	ChromePatch::Inject::InjectIntoChromeProcesses();

	while (!oplock->UnlockFile()) {
		Sleep(1000);
	}
	Sleep(1000);
	while (!oplock->LockFile()) {
		Sleep(1000);
	}
}

// Has to be a somewhat-UNC path
void OplockFile(std::wstring filePath) {
	try {
		ChromePatch::Oplock::Oplock* oplock;
		chromeDllLocks.push_back(oplock = new ChromePatch::Oplock::Oplock(filePath, &OplockCallback));
		oplock->LockFile();
	}
	catch (std::exception& ex) {
		std::cerr << "Error: " << ex.what() << std::endl;
	}
}

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

	ChromePatch::Apc::InitApc();
	
	HKEY hkey;
	DWORD dispo;
	if (RegCreateKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Ceiridge\\ChromePatcher\\ChromeDlls", NULL, NULL, NULL, KEY_READ | KEY_QUERY_VALUE, NULL, &hkey, &dispo) == ERROR_SUCCESS) {
		WCHAR valueName[64];
		DWORD valueNameLength = 64, valueType, dwIndex = 0;
		WCHAR value[1024];
		DWORD valueLength = 1024;

		LSTATUS STATUS;
		while (STATUS = RegEnumValue(hkey, dwIndex, valueName, &valueNameLength, NULL, &valueType, (LPBYTE)&value, &valueLength) == ERROR_SUCCESS) {
			if (valueType == REG_SZ) {
				std::wcout << "New path found: " << value << std::endl;

				std::wstring path = value;

				if (path.find(L"\\Application") != std::wstring::npos) {
					ChromePatch::Inject::chromePaths.push_back(path);
					OplockFile(L"\\??\\" + path);
				}
			}

			valueLength = 1024;
			valueNameLength = 64;
			dwIndex++;
		}

		RegCloseKey(hkey);
	}
	else {
		std::cerr << "Couldn't open regkey\n";
		return 1;
	}

	while (true) {
		Sleep(10000);
	}
	return 0;
}
