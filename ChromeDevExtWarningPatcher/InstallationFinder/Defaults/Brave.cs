using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class Brave : Installation {
		public Brave() : base("Brave") { }

		public override List<string> FindDllFiles() {
			List<string> dllFiles = new List<string>();

			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application"), "chrome.dll"));
			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application"), "chrome.dll"));

			return dllFiles;
		}
	}
}
