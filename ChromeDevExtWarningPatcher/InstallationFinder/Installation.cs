using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	abstract class Installation {
		protected string Name;

		public Installation(string Name) {
			this.Name = Name;
		}

		public abstract List<InstallationPaths> FindInstallationPaths();

		protected static FileInfo[] GetLatestDllAndExe(DirectoryInfo versionsFolder, string dllName, string exeName) {
			if (!versionsFolder.Exists)
				return null;
			FileInfo[] infos = new FileInfo[2];

			List<DirectoryInfo> chromeVersions = new List<DirectoryInfo>(versionsFolder.EnumerateDirectories());
			chromeVersions = chromeVersions.OrderByDescending(dirInfo => GetUnixTime(dirInfo.LastWriteTime)).ToList();

			foreach (DirectoryInfo chromeVersion in chromeVersions) {
				if (chromeVersion.Name.Contains(".")) {
					foreach (FileInfo file in chromeVersion.EnumerateFiles()) {
						if (file.Name.Equals(dllName)) {
							infos[0] = file;
							break;
						}
					}
				}
			}

			FileInfo chromeExe = new FileInfo(Path.Combine(versionsFolder.FullName, exeName));
			if(chromeExe.Exists && infos[0] != null) { // Every installation path also has to have a chrome.exe, otherwise the entire patcher won't work
				infos[1] = chromeExe;
				return infos;
			}

			return null;
		}

		protected static void AddDllAndExeToList(List<InstallationPaths> pathList, FileInfo[] latestDllAndExe) {
			if (latestDllAndExe == null || latestDllAndExe.Length != 2)
				return;
			pathList.Add(new InstallationPaths(latestDllAndExe[0], latestDllAndExe[1]));
		}

		private static double GetUnixTime(DateTime date) {
			return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}
	}
}
