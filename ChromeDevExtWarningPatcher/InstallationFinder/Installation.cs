using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	internal abstract class Installation {
		protected string Name;

		protected Installation(string name) {
			this.Name = name;
		}

		public abstract List<InstallationPaths> FindInstallationPaths();

		protected InstallationPaths GetLatestDllAndExe(DirectoryInfo versionsFolder, string dllName, string exeName) {
			if (!versionsFolder.Exists) {
				return new InstallationPaths(this.Name);
			}

			InstallationPaths paths = new InstallationPaths(this.Name);

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

			return new InstallationPaths(this.Name);
		}

		protected static void AddDllAndExeToList(List<InstallationPaths> pathList, InstallationPaths latestDllAndExe) {
			if (latestDllAndExe.ChromeDllPath != null && latestDllAndExe.ChromeExePath != null) {
				pathList.Add(latestDllAndExe);
			}
		}

		public static double GetUnixTime(DateTime date) {
			return (date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}
	}
}
