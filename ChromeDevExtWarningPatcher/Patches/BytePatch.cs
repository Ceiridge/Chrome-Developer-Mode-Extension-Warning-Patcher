using ChromeDevExtWarningPatcher.Patches;

namespace ChromeDevExtWarningPatcher {
    public class BytePatch {
        public byte origByteX64, patchByteX64;
        public int offsetX64, aoffsetX64;
        public BytePatchPattern pattern;

        public byte origByteX86, patchByteX86;
        public int offsetX86, aoffsetX86;

        public int group;

        public BytePatch(BytePatchPattern pattern, byte origByteX64, byte patchByteX64, int offsetX64, int aoffsetX64, byte origByteX86, byte patchByteX86, int offsetX86, int aoffsetX86, int group) {
            this.pattern = pattern;
            this.origByteX64 = origByteX64;
            this.patchByteX64 = patchByteX64;
            this.offsetX64 = offsetX64;
            this.aoffsetX64 = aoffsetX64;

            this.origByteX86 = origByteX86;
            this.patchByteX86 = patchByteX86;
            this.offsetX86 = offsetX86;
            this.aoffsetX86 = aoffsetX86;

            this.group = group;
        }
    }
}
