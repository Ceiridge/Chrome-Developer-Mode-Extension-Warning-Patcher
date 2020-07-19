#include <Windows.h>
#include <winternl.h>
#include <iostream>
#include <vector>
#include <string>
#include <strsafe.h>
#include <functional>
#include <vector>

#include "apc.hpp"
#include "oplock.hpp"

std::vector<HANDLE> chromeDllHandles;
ChromePatch::Oplock::Oplock* oplockTemp;

void OplockCallback() {
	//MessageBox(NULL, L"Broken!", std::to_wstring(oplockTemp->IsBroken()).c_str(), MB_OK);
	oplockTemp->UnlockFile();

	//delete oplockTemp;
	//oplockTemp = new ChromePatch::Oplock::Oplock(chromeDllHandles[0], std::bind(&OplockCallback));
	Sleep(100);
	oplockTemp->LockFile();
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
		chromeDllHandles.push_back(file);
		std::wcout << L"Handle for " << filePath << L": " << file << std::endl;
		
		oplockTemp = new ChromePatch::Oplock::Oplock(file, std::bind(&OplockCallback));
		oplockTemp->LockFile();
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