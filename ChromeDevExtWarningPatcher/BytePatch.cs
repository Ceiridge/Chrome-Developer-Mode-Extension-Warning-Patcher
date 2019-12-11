namespace ChromeDevExtWarningPatcher
{
    public class BytePatch
    {
        public byte origByte, patchByte;
        public int offset;
        public byte[] pattern;

        public BytePatch(byte[] pattern, byte origByte, byte patchByte, int offset)
        {
            this.pattern = pattern;
            this.origByte = origByte;
            this.patchByte = patchByte;
            this.offset = offset;
        }
    }
}
