using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
    class Edge : Installation {
        public Edge() : base("Edge") { }

        public override List<string> FindDllFiles() {
            List<string> dllFiles = new List<string>();

            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Progion"), "msedge.dll"));
            AddDllToList(dllFiles, GetLatestDll(new DirectoryInfo(@"C:\Prcation"), "msedge.dll"));

            return dllFiles;
        }
    }
}
