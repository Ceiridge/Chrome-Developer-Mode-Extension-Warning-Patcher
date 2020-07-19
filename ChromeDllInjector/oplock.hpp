#pragma once

namespace ChromePatch::Oplock {
	class Oplock {
	public:
		// Callback can be NULL
		Oplock(HANDLE file, std::function<void(Oplock*)> callback);
		~Oplock();

		bool LockFile();
		bool IsBroken();
		bool UnlockFile();
	private:
		HANDLE file{};
		std::function<void(Oplock*)> callback{};

		bool broken{};
		Apc::ApcEntry* apc;

		bool ControlFile(ULONG controlCode);
		void InternalApcCallback();
		void CreateApc();
	};
}
