using ChromeDevExtWarningPatcher.Patches;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher {
	public class BytePatch {
		public byte origByte, patchByte;
		public List<int> offsets;
		public int sigOffset;
		public bool isSig;
		public BytePatchPattern pattern;

		public int group;

		public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, List<int> offsets, int group, bool isSig = false, int sigOffset = 0) {
			this.pattern = pattern;
			this.origByte = origByte;
			this.patchByte = patchByte;
			this.offsets = offsets;
			this.isSig = isSig;
			this.sigOffset = sigOffset;
			this.group = group;
		}
	}
}
