#include "stdafx.h"
#include "apc.hpp"
#include "oplock.hpp"

typedef NTSTATUS(*PNtFsControlFile)(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, ULONG FsControlCode, PVOID InputBuffer, ULONG InputBufferLength, PVOID OutputBuffer, ULONG OutputBufferLength);
PNtFsControlFile NtFsControlFile = NULL;

namespace ChromePatch::Oplock {
	Oplock::Oplock(HANDLE file, std::function<void(Oplock*)> callback) {
		this->file = file;
		this->callback = callback;
		this->CreateApc();
	}

	Oplock::Oplock(std::wstring& filePath, std::function<void(Oplock*)> callback) {
		this->callback = callback;
		this->CreateFileHandle(filePath);
		this->CreateApc();
	}

	Oplock::~Oplock() {
		if(this->apc != nullptr)
			delete this->apc;
	}

	void Oplock::CreateFileHandle(std::wstring& filePath) {
		OBJECT_ATTRIBUTES objAttr;
		UNICODE_STRING _filePath;
		IO_STATUS_BLOCK ioStatus = { 0 };
		LARGE_INTEGER allocSize;

		RtlInitUnicodeString(&_filePath, filePath.c_str());
		InitializeObjectAttributes(&objAttr, &_filePath, OBJ_CASE_INSENSITIVE, 0, NULL);
		ZeroMemory(&ioStatus, sizeof(IO_STATUS_BLOCK));
		allocSize.QuadPart = 0x0;

		NTSTATUS createStatus;
		if (NT_SUCCESS(createStatus = NtCreateFile(&this->file, SYNCHRONIZE | GENERIC_READ, &objAttr, &ioStatus, &allocSize, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_OPEN, 0x0, NULL, 0))) {
			std::wcout << L"Handle for " << filePath << L": " << file << std::endl;
			this->filePath = filePath;
		}
		else {
			std::wcerr << L"Warning! Couldn't open file" << filePath << L" (" << createStatus << ")" << std::endl;
			throw std::runtime_error("Couldn't open file");
		}
	}

	bool Oplock::ControlFile(ULONG controlCode) {
		if (NtFsControlFile == NULL) {
			if (!(NtFsControlFile = reinterpret_cast<PNtFsControlFile>(GetProcAddress(GetModuleHandle(L"ntdll.dll"), "NtFsControlFile")))) {
				std::cerr << "Couldn't find NtFsControlFile" << std::endl;
				return false;
			}
		}

		apc->CreateEntry(APC_TYPE_FSCTL);
		if (NtFsControlFile(file, apc->event, NULL, NULL, &apc->ioStatus, controlCode, NULL, 0, NULL, 0) == STATUS_PENDING) {
			std::cout << "Controlling file " << file << std::endl;
			this->failedAttempts = 0;

			apc->param = controlCode;
			apc->InsertEntry();
			return true;
		}
		else {
			std::cout << "Error trying to control file " << file << " with " << controlCode << std::endl;
			apc->FreeEntry();
			this->failedAttempts++;

			if (this->failedAttempts >= 10) {
				this->failedAttempts = 0;

				try {
					if (this->filePath.length() > 0) {
						DWORD _fileFlags;
						if (GetHandleInformation(this->file, &_fileFlags)) { // If the handle isn't broken
							NtClose(this->file);
						}
						this->CreateFileHandle(this->filePath);
						this->ignoreNextUnlock = true; // A new handle automatically removed old oplocks apparently
					}
				}
				catch (std::exception& ex) {}
			}
		}

		return false;
	}

	bool Oplock::LockFile() {
		if (this->ControlFile(FSCTL_REQUEST_OPLOCK_LEVEL_1)) {
			this->ignoreNextUnlock = false;
			return true;
		}
		else {
			this->ignoreNextUnlock = true;
			return false;
		}
	}

	bool Oplock::UnlockFile() {
		if (!this->ignoreNextUnlock) {
			return this->ControlFile(FSCTL_OPLOCK_BREAK_ACKNOWLEDGE);
		}

		return true;
	}

	bool Oplock::IsBroken() {
		return this->broken;
	}

	void Oplock::InternalApcCallback() {
		this->broken = true;

		this->apc->FreeEntry();
		delete this->apc;
		CreateApc();

		if (this->callback != nullptr) {
			this->callback(this);
		}
	}

	void Oplock::CreateApc() {
		this->apc = new Apc::ApcEntry(std::bind(&Oplock::InternalApcCallback, this));
	}
}
