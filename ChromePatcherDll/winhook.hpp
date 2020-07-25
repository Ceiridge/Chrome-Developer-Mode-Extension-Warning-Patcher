#pragma once

inline HHOOK hook;

_declspec(dllexport) LRESULT CALLBACK CBTProc(int nCode, WPARAM wParam, LPARAM lParam);
_declspec(dllexport) bool InstallWinHook();
_declspec(dllexport) bool UnInstallWinHook();