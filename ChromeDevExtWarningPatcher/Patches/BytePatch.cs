using ChromeDevExtWarningPatcher.Patches;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher {
	public class BytePatch {
		public byte OrigByte, PatchByte;
		public List<int> Offsets;
		public int SigOffset;
		public bool IsSig;
		public BytePatchPattern Pattern;

		public int Group;

		public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, List<int> offsets, int group, bool isSig = false, int sigOffset = 0) {
			this.Pattern = pattern;
			this.OrigByte = origByte;
			this.PatchByte = patchByte;
			this.Offsets = offsets;
			this.IsSig = isSig;
			this.SigOffset = sigOffset;
			this.Group = group;
		}
	}
}
