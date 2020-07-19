using ChromeDevExtWarningPatcher.Patches;

namespace ChromeDevExtWarningPatcher {
	public class BytePatch {
		public byte origByte, patchByte;
		public int offset;
		public BytePatchPattern pattern;

		public int group;

		public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, int offset, int group) {
			this.pattern = pattern;
			this.origByte = origByte;
			this.patchByte = patchByte;
			this.offset = offset;
			this.group = group;
		}
	}
}
