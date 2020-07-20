#include "stdafx.h"
#include "apc.hpp"
#include "oplock.hpp"
#include "injector.hpp"

std::map<HANDLE, ChromePatch::Oplock::Oplock*> chromeDllHandles;

void OplockCallback(ChromePatch::Oplock::Oplock* oplock) {
	ChromePatch::Inject::InjectIntoChromeProcesses();

	oplock->UnlockFile();
	Sleep(1000);
	while (!oplock->LockFile()) {
		Sleep(1000);
	}
}

// Has to be a somewhat-UNC path
void OplockFile(std::wstring filePath) {
	HANDLE file;
	OBJECT_ATTRIBUTES objAttr;
	UNICODE_STRING _filePath;
	IO_STATUS_BLOCK ioStatus = { 0 };
	LARGE_INTEGER allocSize;

	RtlInitUnicodeString(&_filePath, filePath.c_str());
	InitializeObjectAttributes(&objAttr, &_filePath, OBJ_CASE_INSENSITIVE, 0, NULL);
	ZeroMemory(&ioStatus, sizeof(IO_STATUS_BLOCK));
	allocSize.QuadPart = 0x0;

	NTSTATUS createStatus;
	if (NT_SUCCESS(createStatus = NtCreateFile(&file, SYNCHRONIZE | GENERIC_READ, &objAttr, &ioStatus, &allocSize, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_OPEN, 0x0, NULL, 0))) {
		std::wcout << L"Handle for " << filePath << L": " << file << std::endl;

		ChromePatch::Oplock::Oplock* oplock;
		chromeDllHandles.emplace(file, oplock = new ChromePatch::Oplock::Oplock(file, &OplockCallback));
		oplock->LockFile();
	}
	else {
		std::wcerr << L"Warning! Couldn't open file" << filePath << L" (" << createStatus << ")" << std::endl;
	}
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[]) {
#ifndef _DEBUG // Hide the injector console and write the output to a file instead
	FreeConsole();

	FILE* stdoutFile;
	FILE* stderrFile;
	freopen_s(&stdoutFile, "injector.log", "a", stdout);
	freopen_s(&stderrFile, "injector.log", "a", stderr);
#endif

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
