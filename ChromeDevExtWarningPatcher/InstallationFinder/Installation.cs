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

        public abstract List<string> FindDllFiles();

        protected static FileInfo GetLatestDll(DirectoryInfo versionsFolder, string dllName) {
            if (!versionsFolder.Exists)
                return null;

            List<DirectoryInfo> chromeVersions = new List<DirectoryInfo>(versionsFolder.EnumerateDirectories());
            chromeVersions = chromeVersions.OrderByDescending(dirInfo => GetUnixTime(dirInfo.LastWriteTime)).ToList();

            foreach (DirectoryInfo chromeVersion in chromeVersions) {
                if (chromeVersion.Name.Contains(".")) {
                    foreach (FileInfo file in chromeVersion.EnumerateFiles()) {
                        if (file.Name.Equals(dllName))
                            return file;
                    }
                }
            }
            return null;
        }

        protected static void AddDllToList(List<String> dllList, FileInfo latestDll) {
            if (latestDll == null)
                return;
            dllList.Add(latestDll.FullName);
        }

        private static double GetUnixTime(DateTime date) {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
