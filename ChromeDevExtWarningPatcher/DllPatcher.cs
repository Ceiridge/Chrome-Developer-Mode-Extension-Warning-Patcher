using ChromeDevExtWarningPatcher.Patches;
using System;
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

            FileInfo dllFileBackup = new FileInfo(dllFile.FullName + ".bck");
            if (!dllFileBackup.Exists) {
                File.WriteAllBytes(dllFileBackup.FullName, raw);
                log("Backupped to " + dllFileBackup.FullName);
            }

            if (Program.bytePatchManager.PatchBytes(ref raw, IsImageX64(dllFile.FullName), log)) {
                File.WriteAllBytes(dllFile.FullName, raw);
                log("Patched and saved successfully " + dllFile.FullName);
                return true;
            } else {
                log("Error trying to patch " + dllFile.FullName);
            }

            return false;
        }

        // Taken from https://stackoverflow.com/questions/480696/how-to-find-if-a-native-dll-file-is-compiled-as-x64-or-x86
        private static bool IsImageX64(string dllFilePath) {
            using (var stream = new FileStream(dllFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream)) {
                //check the MZ signature to ensure it's a valid Portable Executable image
                if (reader.ReadUInt16() != 23117)
                    throw new BadImageFormatException("Not a valid Portable Executable image", dllFilePath);

                // seek to, and read, e_lfanew then advance the stream to there (start of NT header)
                stream.Seek(0x3A, SeekOrigin.Current);
                stream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);

                // Ensure the NT header is valid by checking the "PE\0\0" signature
                if (reader.ReadUInt32() != 17744)
                    throw new BadImageFormatException("Not a valid Portable Executable image", dllFilePath);

                // seek past the file header, then read the magic number from the optional header
                stream.Seek(20, SeekOrigin.Current);
                ushort magicByte = reader.ReadUInt16();
                return magicByte == 0x20B;
            }
        }
    }
}
