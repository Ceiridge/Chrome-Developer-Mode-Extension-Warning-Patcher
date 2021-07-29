using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher.Patches {
	public class BytePatch {
		public byte OrigByte, PatchByte;
		public List<int> Offsets;
		public int SigOffset;
		public bool IsSig;
		public BytePatchPattern Pattern;
		public byte[]? NewBytes;

		public int Group;

		public BytePatch(BytePatchPattern pattern, byte origByte, byte patchByte, List<int> offsets, int group, byte[]? newBytes = null, bool isSig = false, int sigOffset = 0) {
			this.Pattern = pattern;
			this.OrigByte = origByte;
			this.PatchByte = patchByte;
			this.Offsets = offsets;
			this.IsSig = isSig;
			this.SigOffset = sigOffset;
			this.Group = group;
			this.NewBytes = newBytes;
		}
	}
}
