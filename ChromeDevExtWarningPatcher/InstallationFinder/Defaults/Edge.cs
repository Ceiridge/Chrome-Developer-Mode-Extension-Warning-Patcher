using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
    class Edge : Installation {
        public Edge() : base("Edge") { }

        public override List<string> FindDllFiles() {
            List<string> dllFiles = new List<string>();

            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application"), "msedge.dll"));
            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Program Files\Microsoft\Edge\Application"), "msedge.dll"));

            return dllFiles;
        }
    }
}
