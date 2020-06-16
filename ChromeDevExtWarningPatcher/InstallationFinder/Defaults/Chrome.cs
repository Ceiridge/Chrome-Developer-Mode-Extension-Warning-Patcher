using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class Chrome : Installation {
		public Chrome() : base("Chrome") { }

		public override List<string> FindDllFiles() {
			List<string> dllFiles = new List<string>();

			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files (x86)\Google\Chrome\Application"), "chrome.dll"));
			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files\Google\Chrome\Application"), "chrome.dll"));


			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files (x86)\Google\Chrome Beta\Application"), "chrome.dll"));
			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files\Google\Chrome Beta\Application"), "chrome.dll"));

			return dllFiles;
		}
	}
}
