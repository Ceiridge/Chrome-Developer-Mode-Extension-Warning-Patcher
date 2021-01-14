using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class CustomPath : Installation {
		private readonly string path;

		public CustomPath(string path) : base("CustomPath") {
			this.path = path;
		}

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "chrome.exe"));
			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(this.path), "msedge.dll", "msedge.exe"));
			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "brave.exe"));
			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(this.path), "browser.dll", "browser.exe"));

			return dllFiles;
		}
	}
}
