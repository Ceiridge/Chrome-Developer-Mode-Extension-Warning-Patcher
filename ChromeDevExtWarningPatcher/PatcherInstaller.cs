using ChromeDevExtWarningPatcher.InstallationFinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher {
	class PatcherInstaller {
		private const uint FILE_HEADER = 0xCE161D6E;
		private const uint PATCH_HEADER = 0x8A7C5000;

		private List<InstallationPaths> InstallationPaths;

		public PatcherInstaller(List<InstallationPaths> installationPaths) {
			InstallationPaths = installationPaths;
		}

		private static byte[] GetPatchFileBinary(InstallationPaths paths) {
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);

			writer.Write(FILE_HEADER);
			writer.Write(paths.ChromeDllPath);

			foreach (BytePatch patch in Program.bytePatchManager.BytePatches) {
				if (Program.bytePatchManager.DisabledGroups.Contains(patch.group)) {
					continue;
				}

				writer.Write(PATCH_HEADER);
				writer.Write(patch.pattern.AlternativePatternsX64.Count);

				foreach (byte[] pattern in patch.pattern.AlternativePatternsX64) {
					writer.Write(pattern.Length);
					writer.Write(pattern);
				}

				writer.Write(patch.offset);
				writer.Write(patch.origByte);
				writer.Write(patch.patchByte);
				writer.Write(patch.isSig);
				writer.Write(patch.sigOffset);
			}

			writer.Close();
			byte[] data = stream.ToArray();
			stream.Close();
			return data;
		}

		private static RegistryKey OpenDllKey() {
			return Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Ceiridge\ChromePatcher\ChromeDlls");
		}
		
		private static DirectoryInfo GetProgramsFolder() {
			return new DirectoryInfo(Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "Ceiridge", "ChromeDllInjector"));
		}

		public delegate void WriteToLog(string str);
		public bool Install(WriteToLog log) {
			using (RegistryKey dllPathKeys = OpenDllKey()) {
				foreach (string valueName in dllPathKeys.GetValueNames()) {
					if (valueName.Length > 0) {
						dllPathKeys.DeleteValue(valueName);
					}
				}
				log("Cleared ChromeDlls registry");

				int i = 0;
				foreach (InstallationPaths paths in InstallationPaths) {
					string appDir = Path.GetDirectoryName(paths.ChromeExePath);

					// Write patch data info file
					byte[] patchData = GetPatchFileBinary(paths);
					File.WriteAllBytes(Path.Combine(appDir, "ChromePatches.bin"), patchData);
					log("Wrote patch file to " + appDir);

					// Write chrome patcher dll
					File.WriteAllBytes(Path.Combine(appDir, "ChromePatcher.dll"), Properties.Resources.ChromePatcherDll);
					log("Wrote patcher dll to " + appDir);

					// Add chrome dll to the registry key
					dllPathKeys.SetValue(i.ToString(), paths.ChromeDllPath);
					i++;
					log("Appended " + paths.ChromeDllPath + " to the registry key");
				}
			}

			// Write the injector to "Program Files"
			DirectoryInfo programsFolder = GetProgramsFolder();
			if(!programsFolder.Exists) {
				Directory.CreateDirectory(programsFolder.FullName); // Also creates all subdirectories
				File.WriteAllBytes(Path.Combine(programsFolder.FullName, "ChromeDllInjector.exe"), Properties.Resources.ChromeDllInjector);
			}


			return true;
		}

		public bool UninstallAll(WriteToLog log) {

		}
	}
}
