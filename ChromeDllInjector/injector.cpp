#include "stdafx.h"
#include "injector.hpp"

namespace ChromePatch::Inject {
	void InjectIntoChromeProcesses() {
		DWORD processes[1024], cbNeeded, cProcesses;
		if (!EnumProcesses(processes, sizeof(processes), &cbNeeded)) {
			return;
		}
		cProcesses = cbNeeded / sizeof(DWORD);

		for (int i = 0; i < cProcesses; i++) {
			try {
				DWORD pid = processes[i];

				if (pid && (!(std::find(injectedPIDs.begin(), injectedPIDs.end(), pid) != injectedPIDs.end()) || injectedPIDs.empty())) { // doesn't contain
					WCHAR procName[1024];
					HANDLE proc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);

					if (proc) {
						HMODULE hMod;
						DWORD cbNeeded;

						if (EnumProcessModules(proc, &hMod, sizeof(hMod), &cbNeeded)) {
							GetModuleFileNameEx(proc, hMod, procName, sizeof(procName) / sizeof(WCHAR));
							std::wstring procPathFolder = procName;
							std::wstring procPath = procName;

							if (procPathFolder.find(L"\\Application") != std::wstring::npos) {
								procPathFolder = procPathFolder.substr(0, procPathFolder.find(L"\\Application"));
							}

							for (const std::wstring& str : chromePaths) {
								if (str.substr(0, str.find(L"\\Application")).compare(procPathFolder) == 0) {
									InjectDll(pid, procPath.substr(0, procPath.find_last_of(L'\\')) + L"\\ChromePatcher.dll");
								}
							}
						}
						CloseHandle(proc);
					}
				}
			}
			catch (std::exception& ex) {
				std::cerr << "Exception: " << ex.what() << std::endl;
			}
		}
	}

	void InjectDll(DWORD pid, const std::wstring& dllPath) {
		injectedPIDs.push_back(pid);
		std::wcout << L"Injecting " << dllPath << L" into " << pid << std::endl;

		std::ifstream dllStream(dllPath);
		if (!dllStream.good()) {
			throw std::exception("Dll not found or not accessible");
		}
		dllStream.close();

		HANDLE proc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
		
		if (proc && proc != INVALID_HANDLE_VALUE) {
			FARPROC loadLib = GetProcAddress(LoadLibrary(L"kernel32.dll"), "LoadLibraryW");
			if (loadLib == NULL) {
				std::cerr << "LoadLibaryW not found" << std::endl;
				return;
			}

			void* alloc = VirtualAllocEx(proc, NULL, dllPath.size() * sizeof(WCHAR) + 1, MEM_COMMIT, PAGE_EXECUTE_READWRITE); // + 1 for null terminator
			if (alloc == NULL) {
				std::cerr << "Couldn't allocate" << std::endl;
				return;
			}
			if (WriteProcessMemory(proc, alloc, dllPath.c_str(), dllPath.size() * sizeof(WCHAR) + 1, NULL) == NULL) {
				std::cerr << "Couldn't write " << GetLastError() << std::endl;
				return;
			}

			DWORD threadId;
			HANDLE thread = CreateRemoteThread(proc, NULL, 0, (LPTHREAD_START_ROUTINE)loadLib, alloc, 0, &threadId);

			if (thread && thread != INVALID_HANDLE_VALUE) {
				std::wcout << L"Injected" << std::endl;
				WaitForSingleObject(thread, INFINITE);
				CloseHandle(thread);
			}
		}

		CloseHandle(proc);
	}
}