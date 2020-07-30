#include "stdafx.h"
#include "winhook.hpp"

extern "C" __declspec(dllexport) LRESULT CALLBACK CBTProc(int nCode, WPARAM wParam, LPARAM lParam) {
	return CallNextHookEx(hook, nCode, wParam, lParam);
}

extern "C" __declspec(dllexport) bool InstallWinHook() {
	if (hook) {
		return false;
	}

	return hook = SetWindowsHookEx(WH_CBT, CBTProc, module, 0); // Global windows hook -> Windows injects the dll everywhere automatically
}

extern "C" __declspec(dllexport) bool UnInstallWinHook() {
	if (!hook) {
		return false;
	}

	if (UnhookWindowsHookEx(hook)) {
		hook = NULL;
		return true;
	}
	return false;
}
