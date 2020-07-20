using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher.Patches {
	public class BytePatchPattern {
		public string Name;
		public List<byte[]> AlternativePatternsX64 = new List<byte[]>();

		public BytePatchPattern(string name) {
			Name = name;
		}
	}
}
