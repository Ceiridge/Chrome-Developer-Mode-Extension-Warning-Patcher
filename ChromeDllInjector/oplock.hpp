#pragma once

namespace ChromePatch::Oplock {
	class Oplock {
	public:
		// Callback can be NULL
		Oplock(HANDLE file, std::function<void(Oplock*)> callback);
		Oplock(std::wstring& filePath, std::function<void(Oplock*)> callback);
		~Oplock();

		bool LockFile();
		bool IsBroken();
		bool UnlockFile();
	private:
		HANDLE file{};
		std::wstring filePath; // Can be null
		std::function<void(Oplock*)> callback{};

		bool broken{}, ignoreNextUnlock{};
		int failedAttempts{};
		Apc::ApcEntry* apc;

		void CreateFileHandle(std::wstring& filePath);
		bool ControlFile(ULONG controlCode);
		void InternalApcCallback();
		void CreateApc();
	};
}
