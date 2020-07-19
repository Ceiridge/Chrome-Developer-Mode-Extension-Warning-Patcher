#pragma once

namespace ChromePatch::Inject {
	inline std::vector<std::wstring> chromePaths;
	inline std::vector<DWORD> injectedPIDs;

	void InjectIntoChromeProcesses();
	void InjectDll(DWORD pid, std::wstring dllPath);
}