#include "stdafx.h"
#include "apc.hpp"

// Some ideas/code taken from https://github.com/ladislav-zezula/FileTest
namespace ChromePatch::Apc {
	#define ALERT_REASON_STOP_WORKER	0
	#define ALERT_REASON_UPDATE_WAIT	1

	DWORD WINAPI ApcThread(LPVOID pvParameter) {
		ApcEntry* _apcList[MAXIMUM_WAIT_OBJECTS];
		HANDLE WaitHandles[MAXIMUM_WAIT_OBJECTS];
		ApcEntry* apc;

		// The first event is always the alert event
		WaitHandles[0] = alertEvent;
		// Perform a loop until the process does not end
		while (alertEvent != NULL)
		{
			DWORD dwWaitCount = 1;
			DWORD dwWaitResult;

			// Prepare the list of handles to wait
			EnterCriticalSection(&lock);
			{
				for (ApcEntry* apcEntry : apcList) {
					WaitHandles[dwWaitCount] = apcEntry->event;
					_apcList[dwWaitCount++] = apcEntry;
				}
			}
			LeaveCriticalSection(&lock);

			// Now when the list if prepared, we can perform wait on all
			dwWaitResult = WaitForMultipleObjects(dwWaitCount, WaitHandles, FALSE, INFINITE);

			// If the first wait broke, it means that we need to exit
			if (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED_0)
			{
				// If we need just to update wait list, do it
				if (alertReason == ALERT_REASON_UPDATE_WAIT)
					continue;
				break;
			}

			// Check if any of the APCs has triggered
			if (WAIT_OBJECT_0 < dwWaitResult && dwWaitResult < (WAIT_OBJECT_0 + dwWaitCount))
			{
				// Get the pointer to the triggered APC
				apc = _apcList[dwWaitResult - WAIT_OBJECT_0];

				// Remove the APC from the list (locked)
				EnterCriticalSection(&lock);
				{
					apcList.erase(std::remove(apcList.begin(), apcList.end(), apc), apcList.end());
				}
				LeaveCriticalSection(&lock);
				
				if (apc->callback != nullptr) {
					apc->callback();
				}
			}
		}

		// Now we need to free all the APCs
		EnterCriticalSection(&lock);
		{
			for (ApcEntry* apcEntry : apcList) {
				apcEntry->FreeEntry();
			}

			// Reset the APC list
			apcList.clear();
		}
		LeaveCriticalSection(&lock);

		return 0;
	}

	void InitApc() {
		InitializeCriticalSection(&lock);
		alertEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
		apcThread = CreateThread(NULL, 0, ApcThread, NULL, 0, NULL);
	}

	void AlertApcThread(DWORD reason) {
		if (apcThread && alertEvent)
		{
			alertReason = reason;
			SetEvent(alertEvent);
		}
	}


	ApcEntry::ApcEntry(std::function<void()> callback) { // Manually zero these vars
		this->bAsyncComplete = NULL;
		this->bIncrementPos = NULL;
		this->bHasIoStatus = NULL;
		this->callback = callback;
	}
	ApcEntry::~ApcEntry() {}

	bool ApcEntry::CreateEntry(UINT type) {
		if (apcList.size() < MAXIMUM_WAIT_OBJECTS - 1) {
			this->overlapped.hEvent = this->event = CreateEvent(NULL, TRUE, FALSE, NULL);
			this->type = type;
			//this->page = 
			if (this->event != NULL) {
				return true;
			}
		}

		return false;
	}

	bool ApcEntry::InsertEntry() {
		if (this->event != NULL) {
			if (apcList.size() < MAXIMUM_WAIT_OBJECTS - 1) {
				this->bAsyncComplete = TRUE;

				EnterCriticalSection(&lock);
				apcList.push_back(this);
				LeaveCriticalSection(&lock);

				AlertApcThread(ALERT_REASON_UPDATE_WAIT);
				return true;
			}
		}

		return false;
	}

	bool ApcEntry::FreeEntry() {
		if (this->event != NULL) {
			CloseHandle(this->event);
			return true;
		}

		return false;
	}
}