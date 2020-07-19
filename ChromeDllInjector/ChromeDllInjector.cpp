#include "stdafx.h"
#include "apc.hpp"
#include "oplock.hpp"

std::map<HANDLE, ChromePatch::Oplock::Oplock*> chromeDllHandles;

void OplockCallback(ChromePatch::Oplock::Oplock* oplock) {
	//MessageBox(NULL, L"Broken!", std::to_wstring(oplockTemp->IsBroken()).c_str(), MB_OK);
	
	oplock->UnlockFile();
	Sleep(1000);
	oplock->LockFile();
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
	ChromePatch::Apc::InitApc();
	OplockFile(L"\\??\\C:\\Program Files (x86)\\Google\\Chrome\\Application\\84.0.4147.89\\chrome.dll");

	Sleep(100000);
	return 0;
}
