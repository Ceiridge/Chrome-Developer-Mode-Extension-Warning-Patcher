using ChromeDevExtWarningPatcher.InstallationFinder;
using ChromeDevExtWarningPatcher.Patches;
using System.IO;

namespace ChromeDevExtWarningPatcher {
	class DllPatcher {
		private string DllPath;

		public DllPatcher(string dllPath) {
			DllPath = dllPath;
		}

		public bool Patch(BytePatchPattern.WriteToLog log) {
			FileInfo dllFile = new FileInfo(DllPath);
			if (!dllFile.Exists)
				throw new IOException("File not found");

			byte[] raw = File.ReadAllBytes(dllFile.FullName);
			log("Patching " + dllFile.FullName + "...");

			FileInfo dllFileBackup = new FileInfo(dllFile.FullName + ".bck");
			if (!dllFileBackup.Exists) {
				File.WriteAllBytes(dllFileBackup.FullName, raw);
				log("Backupped to " + dllFileBackup.FullName);
			}

			if (Program.bytePatchManager.PatchBytes(ref raw, InstallationManager.IsImageX64(dllFile.FullName), log)) {
				File.WriteAllBytes(dllFile.FullName, raw);
				log("Patched and saved successfully " + dllFile.FullName);
				return true;
			} else {
				log("Error trying to patch " + dllFile.FullName);
			}

			return false;
		}
	}
}
