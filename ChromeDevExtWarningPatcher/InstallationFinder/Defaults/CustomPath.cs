using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ChromeDevExtWarningPatcher.InstallationFinder.Defaults { 
	internal class CustomPath : Installation {
		private readonly string path;

		public CustomPath(string path) : base("CustomPath") {
			this.path = path;
		}

		public override List<InstallationPaths> FindInstallationPaths() {
			List<InstallationPaths> dllFiles = new List<InstallationPaths>();

			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "chrome.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "msedge.dll", "msedge.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "chrome.dll", "brave.exe"));
			AddDllAndExeToList(dllFiles, this.GetLatestDllAndExe(new DirectoryInfo(this.path), "browser.dll", "browser.exe"));

			return dllFiles;
		}

		public static InstallationPaths? GuiAddCustomPath() {
			OpenFileDialog openFile = new OpenFileDialog {
				Title = "Select a chrome.dll",
				Filter = "chrome.dll/msedge.dll file (chrome.dll;msedge.dll)|chrome.dll;msedge.dll|Alternative chrome.dll file (*.dll)|*.dll|All files (*.*)|*.*",
				FilterIndex = 1
			};
			openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;

			if (!openFile.ShowDialog() ?? true) {
				return null;
			}

			string chromeDllPath = openFile.FileName;

			openFile = new OpenFileDialog {
				Title = "Select a chrome.exe",
				Filter = "Browser executable file (*.exe)|*.exe|All files (*.*)|*.*",
				FilterIndex = 1
			};
			openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;
			openFile.InitialDirectory = Directory.GetParent(Path.GetDirectoryName(chromeDllPath)!)!.FullName;

			if (!openFile.ShowDialog() ?? true) {
				return null;
			}

			string chromeExePath = openFile.FileName;
			return new InstallationPaths("Custom Browser", chromeDllPath, chromeExePath);
		}
	}
}
