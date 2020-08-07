using System;
using System.IO;
using System.IO.Compression;

// This tool is only executed on build and should create a zip file that is included in the installer resources
namespace ChromeDllInjectorBuildZipper {
	class Program {
		static void Main(string[] _) {
			FileInfo zipFile = new FileInfo("ChromeDllInjector.zip");
			if (zipFile.Exists) {
				zipFile.Delete();
			}

			using (ZipArchive zip = ZipFile.Open(zipFile.FullName, ZipArchiveMode.Create)) {
				DirectoryInfo current = new DirectoryInfo(".");

				foreach (FileInfo file in current.EnumerateFiles()) {
					if (file.Name.Equals("ChromeDllInjector.exe") || file.Name.Equals("ChromeDllInjector.exe.config") || file.Name.EndsWith(".dll")) { // Zip these files with the filter
						zip.CreateEntryFromFile(file.FullName, file.Name);
					}
				}

				// Include the native binaries
				foreach (DirectoryInfo folder in current.EnumerateDirectories()) {
					if (folder.Name.Equals("amd64") || folder.Name.Equals("x86")) {
						zip.CreateEntryFromDirectory(folder.FullName, folder.Name);
					}
				}
			}

			Console.WriteLine("Wrote zip file");
		}
	}
}
