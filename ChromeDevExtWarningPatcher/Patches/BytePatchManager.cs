using ChromeDevExtWarningPatcher.Patches.Defaults;
using System;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher.Patches {
    class BytePatchManager {
        private List<BytePatch> BytePatches = new List<BytePatch>();

        public List<Type> disabledTypes = new List<Type>();

        public BytePatchManager() {
            BytePatches.Clear();
            BytePatches.Add(new RemoveExtensionWarningPatch1());
            BytePatches.Add(new RemoveExtensionWarningPatch2());
            BytePatches.Add(new RemoveDebugWarningPatch());
            BytePatches.Add(new RemoveElisionPatch());
        }

        public bool PatchBytes(ref byte[] raw, bool x64, BytePatchPattern.WriteToLog log) {
            int patches = 0;

            foreach(BytePatch patch in BytePatches) {
                if (disabledTypes.Contains(patch.GetType())) {
                    patches++;
                    continue;
                }
                long addr = patch.pattern.FindAddress(raw, x64, log);
                int patchOffset = x64 ? patch.offsetX64 : patch.offsetX86;
                byte patchOrigByte = x64 ? patch.origByteX64 : patch.origByteX86;
                byte patchPatchByte = x64 ? patch.patchByteX64 : patch.patchByteX86;

                if(addr != -1) {
                    long index = addr + patchOffset;
                    byte sourceByte = raw[index];

                    log("Source byte of patch at " + patchOffset + ": " + sourceByte);
                    if (sourceByte == patchOrigByte) {
                        raw[index] = patchPatchByte;
                        log(index + " => " + patchPatchByte);
                        patches++;
                    } else
                        log("Source byte unexpected, should be " + patchOrigByte + "!");
                } else {
                    log("Couldn't find offset for a patch " + patch.pattern.Name);
                }
            }

            return patches == BytePatches.Count;
        }
    }
}
