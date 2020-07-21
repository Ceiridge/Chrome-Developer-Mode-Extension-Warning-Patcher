using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	struct InstallationPaths {
		public string ChromeDllPath, ChromeExePath;

		public InstallationPaths(string chromeDllPath, string chromeExePath) {
			ChromeDllPath = chromeDllPath;
			ChromeExePath = chromeExePath;
		}

		public InstallationPaths(FileInfo chromeDll, FileInfo chromeExe) {
			ChromeDllPath = chromeDll.FullName;  
			ChromeExePath = chromeExe.FullName;
		}

		public bool Is64Bit() {
			return InstallationManager.IsImageX64(ChromeDllPath) && InstallationManager.IsImageX64(ChromeExePath);
		}
	}
}
