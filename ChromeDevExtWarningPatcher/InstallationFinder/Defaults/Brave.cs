using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	internal class Brave : Installation {
		public Brave() : base("Brave") { }

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(@"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application"), "chrome.dll", "brave.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application"), "chrome.dll", "brave.exe"));

			return dllFiles;
		}
	}
}
