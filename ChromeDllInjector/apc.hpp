#pragma once

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

namespace ChromePatch::Apc {
	inline int apcCount = 0;
	inline LIST_ENTRY apcList;
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
		LIST_ENTRY entry{};
		HANDLE event{};
		//HWND page{};
		ULONG type{}, param{}, bufLen{}, bAsyncComplete : 1, bIncrementPos : 1, bHasIoStatus : 1;

		ApcEntry();
		~ApcEntry();

		bool CreateEntry(UINT type);
		bool InsertEntry();
		bool FreeEntry();
	};
}