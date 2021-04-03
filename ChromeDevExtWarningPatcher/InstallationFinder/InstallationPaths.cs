using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	public class InstallationPaths {
		[Required]
		public string Name;
		public string? ChromeDllPath, ChromeExePath;

		public InstallationPaths(string name, string chromeDllPath, string chromeExePath) {
			this.Name = name;
			this.ChromeDllPath = chromeDllPath;
			this.ChromeExePath = chromeExePath;
		}

		public InstallationPaths(string name, FileInfo chromeDll, FileInfo chromeExe) : this(name, chromeDll.FullName, chromeExe.FullName) { }

		public InstallationPaths(string name) {
			this.Name = name;
		}

		public bool Is64Bit() {
			if(this.ChromeDllPath == null || this.ChromeExePath == null) {
				return false;
			}

			return InstallationManager.IsImageX64(this.ChromeDllPath) && InstallationManager.IsImageX64(this.ChromeExePath);
		}
	}
}
