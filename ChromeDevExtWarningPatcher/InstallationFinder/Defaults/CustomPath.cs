using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class CustomPath : Installation {
		private string Path;

		public CustomPath(string path) : base("CustomPath") {
			Path = path;
		}

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(Path), "chrome.dll", "chrome.exe"));
			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(Path), "msedge.dll", "msedge.exe"));

			return dllFiles;
		}
	}
}
