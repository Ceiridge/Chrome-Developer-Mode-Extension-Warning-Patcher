#pragma once

// Some ntdll/list features
#define APC_TYPE_NONE               0
#define APC_TYPE_READ_WRITE         1
#define APC_TYPE_LOCK_UNLOCK        2
#define APC_TYPE_FSCTL              3
#define APC_TYPE_IOCTL              4


namespace ChromePatch::Apc {
	inline CRITICAL_SECTION lock;

	inline HANDLE alertEvent, apcThread;
	inline DWORD alertReason;
	
	void InitApc();
	DWORD WINAPI ApcThread(LPVOID pvParameter);
	void AlertApcThread(DWORD reason);

	class ApcEntry {
	public:
		IO_STATUS_BLOCK ioStatus{};
		LARGE_INTEGER byteOffset{};
		OVERLAPPED overlapped{};
		HANDLE event{};
		ULONG type{}, param{}, bufLen{}, bAsyncComplete : 1, bIncrementPos : 1, bHasIoStatus : 1;
		std::function<void()> callback{};

		// Callback can be NULL
		ApcEntry(std::function<void()> callback);
		~ApcEntry();

		bool CreateEntry(UINT type);
		bool InsertEntry();
		bool FreeEntry();
	};

	inline std::vector<ApcEntry*> apcList;
}
