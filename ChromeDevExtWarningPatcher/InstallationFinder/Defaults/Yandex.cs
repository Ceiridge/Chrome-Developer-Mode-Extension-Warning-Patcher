using System;
using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class Yandex : Installation {
		public Yandex() : base("Yandex") { }

		public override List<string> FindDllFiles() {
			List<string> dllFiles = new List<string>();

			string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(Path.Combine(appDataLocal, @"Yandex\YandexBrowser\Application")), "browser.dll"));

			return dllFiles;
		}
	}
}
