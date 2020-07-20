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

		protected static InstallationPaths GetLatestDllAndExe(DirectoryInfo versionsFolder, string dllName, string exeName) {
			if (!versionsFolder.Exists)
				return new InstallationPaths();
			InstallationPaths paths = new InstallationPaths();

			List<DirectoryInfo> chromeVersions = new List<DirectoryInfo>(versionsFolder.EnumerateDirectories());
			chromeVersions = chromeVersions.OrderByDescending(dirInfo => GetUnixTime(dirInfo.LastWriteTime)).ToList();

			foreach (DirectoryInfo chromeVersion in chromeVersions) {
				if (chromeVersion.Name.Contains(".")) {
					foreach (FileInfo file in chromeVersion.EnumerateFiles()) {
						if (file.Name.Equals(dllName)) {
							paths.ChromeDllPath = file.FullName;
							break;
						}
					}
				}
			}

			FileInfo chromeExe = new FileInfo(Path.Combine(versionsFolder.FullName, exeName));
			if (chromeExe.Exists && paths.ChromeDllPath != null) { // Every installation path also has to have a chrome.exe, otherwise the entire patcher won't work
				paths.ChromeExePath = chromeExe.FullName;
				return paths;
			}

			return new InstallationPaths();
		}

		protected static void AddDllAndExeToList(List<InstallationPaths> pathList, InstallationPaths latestDllAndExe) {
			if (latestDllAndExe.ChromeDllPath == null || latestDllAndExe.ChromeExePath == null)
				return;
			pathList.Add(latestDllAndExe);
		}

		private static double GetUnixTime(DateTime date) {
			return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}
	}
}
