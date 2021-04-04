using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults { 
	internal class Edge : Installation {
		public Edge() : base("Edge") { }

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application"), "msedge.dll", "msedge.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(@"C:\Program Files\Microsoft\Edge\Application"), "msedge.dll", "msedge.exe"));

			return dllFiles;
		}
	}
}
