#pragma once

inline HHOOK hook;

extern "C" _declspec(dllexport) LRESULT CALLBACK CBTProc(int nCode, WPARAM wParam, LPARAM lParam);
extern "C" _declspec(dllexport) bool InstallWinHook();
extern "C" _declspec(dllexport) bool UnInstallWinHook();
