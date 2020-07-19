using System;
using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class Yandex : Installation {
		public Yandex() : base("Yandex") { }

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(Path.Combine(appDataLocal, @"Yandex\YandexBrowser\Application")), "browser.dll", "browser.exe"));

			return dllFiles;
		}
	}
}
