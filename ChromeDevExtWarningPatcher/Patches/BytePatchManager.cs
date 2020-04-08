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

                if(addr != -1) {
                    long index = addr + patch.offset;
                    byte sourceByte = raw[index];

                    log("Source byte of patch at " + patch.offset + ": " + sourceByte);
                    if (sourceByte == patch.origByte) {
                        raw[index] = patch.patchByte;
                        log(index + " => " + patch.patchByte);
                        patches++;
                    } else
                        log("Source byte unexpected, should be " + patch.origByte + "!");
                } else {
                    log("Couldn't find offset for a patch " + patch.pattern.Name);
                }
            }

            return patches == BytePatches.Count;
        }
    }
}
