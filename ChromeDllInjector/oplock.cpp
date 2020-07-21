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
	Oplock::~Oplock() {
		delete this->apc;
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

			apc->param = controlCode;
			apc->InsertEntry();
			return true;
		}
		else {
			std::cout << "Error trying to control file " << file << " with " << controlCode << std::endl;
			apc->FreeEntry();
		}

		return false;
	}

	bool Oplock::LockFile() {
		return this->ControlFile(FSCTL_REQUEST_OPLOCK_LEVEL_1);
	}

	bool Oplock::UnlockFile() {
		return this->ControlFile(FSCTL_OPLOCK_BREAK_ACKNOWLEDGE);
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
