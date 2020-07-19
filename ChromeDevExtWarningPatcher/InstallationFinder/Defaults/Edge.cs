using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults {
	class Edge : Installation {
		public Edge() : base("Edge") { }

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, GetLatestDllAndExe(new DirectoryInfo(@"C:\Program Files\Microsoft\Edge\Application"), "msedge.dll", "msedge.exe"));

			return dllFiles;
		}
	}
}
