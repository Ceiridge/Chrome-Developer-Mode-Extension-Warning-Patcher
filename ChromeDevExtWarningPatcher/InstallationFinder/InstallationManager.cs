using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	class InstallationManager {
		private List<Installation> installationFinders = new List<Installation>();

		public InstallationManager() {
			installationFinders.Clear();
			installationFinders.Add(new Chrome());
			installationFinders.Add(new Brave());
			installationFinders.Add(new Edge());
			installationFinders.Add(new Yandex());
		}

		public List<InstallationPaths> FindAllChromiumInstallations() {
			List<InstallationPaths> installations = new List<InstallationPaths>();

			foreach (Installation installation in installationFinders) {
				foreach (InstallationPaths paths in installation.FindInstallationPaths()) {
					if (paths.Is64Bit()) { // force x64
						installations.Add(paths);
					}
				}
			}

			return installations;
		}

		// Taken from https://stackoverflow.com/questions/480696/how-to-find-if-a-native-dll-file-is-compiled-as-x64-or-x86
		public static bool IsImageX64(string peFilePath) {
			using (var stream = new FileStream(peFilePath, FileMode.Open, FileAccess.Read))
			using (var reader = new BinaryReader(stream)) {
				//check the MZ signature to ensure it's a valid Portable Executable image
				if (reader.ReadUInt16() != 23117)
					throw new BadImageFormatException("Not a valid Portable Executable image", peFilePath);

				// seek to, and read, e_lfanew then advance the stream to there (start of NT header)
				stream.Seek(0x3A, SeekOrigin.Current);
				stream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);

				// Ensure the NT header is valid by checking the "PE\0\0" signature
				if (reader.ReadUInt32() != 17744)
					throw new BadImageFormatException("Not a valid Portable Executable image", peFilePath);

				// seek past the file header, then read the magic number from the optional header
				stream.Seek(20, SeekOrigin.Current);
				ushort magicByte = reader.ReadUInt16();
				return magicByte == 0x20B;
			}
		}
	}
}
