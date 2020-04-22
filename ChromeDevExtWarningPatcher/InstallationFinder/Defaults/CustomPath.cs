using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
    class CustomPath : Installation {
        private string Path;

        public CustomPath(string path) : base("CustomPath") {
            Path = path;
        }

        public override List<string> FindDllFiles() {
            List<string> dllFiles = new List<string>();

            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(Path), "chrome.dll"));
            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(Path), "msedge.dll"));

            return dllFiles;
        }
    }
}
