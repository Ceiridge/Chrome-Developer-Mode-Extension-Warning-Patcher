using ChromeDevExtWarningPatcher.Patches;

namespace ChromeDevExtWarningPatcher
{
    public class BytePatch
    {
        public byte origByte, patchByte;
        public int offset;
        public BytePatchPattern pattern;

        public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, int offset)
        {
            this.pattern = pattern;
            this.origByte = origByte;
            this.patchByte = patchByte;
            this.offset = offset;
        }
    }
}
