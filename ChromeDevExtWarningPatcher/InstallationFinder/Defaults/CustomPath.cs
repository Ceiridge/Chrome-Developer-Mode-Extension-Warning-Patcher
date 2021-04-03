using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults { 
	internal class CustomPath : Installation {
		private readonly string path;

		public CustomPath(string path) : base("CustomPath") {
			this.path = path;
		}

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "chrome.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "msedge.dll", "msedge.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "brave.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "browser.dll", "browser.exe"));

			return dllFiles;
		}
	}
}
