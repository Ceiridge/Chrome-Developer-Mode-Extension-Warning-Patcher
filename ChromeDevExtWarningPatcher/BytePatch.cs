namespace ChromeDevExtWarningPatcher
{
    public class BytePatch
    {
        public byte origByte, patchByte;
        public int offset;

        public BytePatch(byte origByte, byte patchByte, int offset)
        {
            this.origByte = origByte;
            this.patchByte = patchByte;
            this.offset = offset;
        }
    }
}
