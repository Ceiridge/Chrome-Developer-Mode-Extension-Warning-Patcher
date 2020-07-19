#pragma once

// Some ntdll/list features
#define InitializeListHead(ListHead) (\
    (ListHead)->Flink = (ListHead)->Blink = (ListHead))
#define RemoveEntryList(Entry) {\
    PLIST_ENTRY _EX_Blink;\
    PLIST_ENTRY _EX_Flink;\
    _EX_Flink = (Entry)->Flink;\
    _EX_Blink = (Entry)->Blink;\
    _EX_Blink->Flink = _EX_Flink;\
    _EX_Flink->Blink = _EX_Blink;\
    }
#define InsertTailList(ListHead,Entry) {\
    PLIST_ENTRY _EX_Blink;\
    PLIST_ENTRY _EX_ListHead;\
    _EX_ListHead = (ListHead);\
    _EX_Blink = _EX_ListHead->Blink;\
    (Entry)->Flink = _EX_ListHead;\
    (Entry)->Blink = _EX_Blink;\
    _EX_Blink->Flink = (Entry);\
    _EX_ListHead->Blink = (Entry);\
    }
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
		//LIST_ENTRY entry{};
		HANDLE event{};
		//HWND page{};
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