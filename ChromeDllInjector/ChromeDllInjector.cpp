#include <Windows.h>
#include <winternl.h>
#include <iostream>
#include <vector>
#include <string>
#include <strsafe.h>

#include "apc.hpp"

std::vector<HANDLE> chromeDllHandles;

typedef NTSTATUS(*PNtFsControlFile)(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine,PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, ULONG FsControlCode, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
PNtFsControlFile NtFsControlFile;

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

	if (NT_SUCCESS(NtCreateFile(&file, SYNCHRONIZE | GENERIC_READ, &objAttr, &ioStatus, &allocSize, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_OPEN, 0, NULL, 0))) {
		chromeDllHandles.push_back(file);
		std::wcout << L"Handle for " << filePath << L": " << file << std::endl;
		
		ChromePatch::Apc::ApcEntry apc;
		if (NtFsControlFile(file, apc.event, NULL, NULL, &apc.ioStatus, FSCTL_REQUEST_OPLOCK_LEVEL_1, NULL, 0, NULL, 0) == STATUS_PENDING) {
			std::cout << "Controlling file" << std::endl;

			apc.param = FSCTL_REQUEST_OPLOCK_LEVEL_1;
			apc.InsertEntry();
		}
		else {
			std::cout << "Error trying to control file" << std::endl;
			apc.FreeEntry();
		}
	}
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[]) {
	if (!(NtFsControlFile = reinterpret_cast<PNtFsControlFile>(GetProcAddress(GetModuleHandle(L"ntdll.dll"), "NtFsControlFile")))) {
		std::cerr << "Couldn't find NtFsControlFile" << std::endl;
		return 1;
	}

	ChromePatch::Apc::InitApc();
	Sleep(100000);
	return 0;
}