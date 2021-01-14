using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	struct InstallationPaths {
		public string ChromeDllPath, ChromeExePath;

		public InstallationPaths(string chromeDllPath, string chromeExePath) {
			this.ChromeDllPath = chromeDllPath;
			this.ChromeExePath = chromeExePath;
		}

		public InstallationPaths(FileInfo chromeDll, FileInfo chromeExe) {
			this.ChromeDllPath = chromeDll.FullName;
			this.ChromeExePath = chromeExe.FullName;
		}

		public bool Is64Bit() {
			return InstallationManager.IsImageX64(this.ChromeDllPath) && InstallationManager.IsImageX64(this.ChromeExePath);
		}
	}
}
