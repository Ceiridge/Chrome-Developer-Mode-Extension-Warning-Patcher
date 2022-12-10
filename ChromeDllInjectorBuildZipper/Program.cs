using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

// This tool is helps in the build process
namespace ChromeDllInjectorBuildZipper {
	internal class Program {
		public static void Main(string[] _) {
			Console.WriteLine("\n\n=== CHR DLL INJECTOR BUILD ZIPPER HELPER START ===\n");

			string buildFolder = ProjectFileFinders.FindBuildFolder();
			SignFiles(buildFolder);

			// Place zip file in the root project folder
			string zipFilePath = Path.Combine(buildFolder, "..", "..", "..", "..", "ChromeDllInjector.zip");
			ZipFiles(buildFolder, zipFilePath);

			Console.WriteLine("\n=== CHR DLL INJECTOR BUILD ZIPPER HELPER DONE ===\n\n");
		}

		private static void SignFiles(string buildFolder) {
			FileInfo? signExe = ProjectFileFinders.FindSignExecutor(new DirectoryInfo(Path.Combine(buildFolder, "..")));

			if (signExe is null) {
				Console.Error.WriteLine("No sign executor found! Not signing any files now.");
				return;
			}

			ProcessStartInfo psi = new ProcessStartInfo("cmd.exe") {
				ArgumentList = { "/c", signExe.FullName },
				WorkingDirectory = buildFolder
			};
			Process? p = Process.Start(psi);
			p?.WaitForExit();

			Console.WriteLine("Possibly signed all files.");
		}

		private static void ZipFiles(string buildFolder, string zipFilePath) {
			Console.WriteLine("Writing zip file to: " + zipFilePath);

			FileInfo zipFile = new FileInfo(zipFilePath);
			if (zipFile.Exists) {
				zipFile.Delete();
			}

			using ZipArchive zip = ZipFile.Open(zipFile.FullName, ZipArchiveMode.Create);
			DirectoryInfo current = new DirectoryInfo(buildFolder);

			foreach (FileInfo file in current.EnumerateFiles()) {
				zip.CreateEntryFromFile(file.FullName, file.Name);
			}

			// Include the native binaries
			foreach (DirectoryInfo folder in current.EnumerateDirectories()) {
				if (folder.Name is not ("amd64" or "arm64" or "fr" or "ref" or "x86")) {
					continue;
				}

				zip.CreateEntryFromDirectory(folder.FullName, folder.Name);
			}

			Console.WriteLine("Wrote zip file.");
		}
	}
}
