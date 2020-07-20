using ChromeDevExtWarningPatcher.Patches;

namespace ChromeDevExtWarningPatcher {
	public class BytePatch {
		public byte origByte, patchByte;
		public int offset, sigOffset;
		public bool isSig;
		public BytePatchPattern pattern;

		public int group;

		public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, int offset, int group, bool isSig = false, int sigOffset = 0) {
			this.pattern = pattern;
			this.origByte = origByte;
			this.patchByte = patchByte;
			this.offset = offset;
			this.isSig = isSig;
			this.sigOffset = sigOffset;
			this.group = group;
		}
	}
}
