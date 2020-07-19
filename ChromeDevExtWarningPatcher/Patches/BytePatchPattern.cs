using System;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher.Patches {
	public class BytePatchPattern {
		public string Name;
		public List<byte[]> AlternativePatternsX64 = new List<byte[]>();

		public BytePatchPattern(string name) {
			Name = name;
		}

		public delegate void WriteToLog(string str);
		/*public Tuple<long, byte[]> FindAddress(byte[] raw, bool x64, WriteToLog log) { // This returns the offset and pattern
			foreach (byte[] pattern in AlternativePatternsX64) {
				int patternIndex = 0, patternOffset = 0;

				for (int i = 0; i < raw.Length; i++) {
					byte chromeByte = raw[i];
					byte expectedByte = pattern[patternIndex];

					if (expectedByte == 0xFF ? true : (chromeByte == expectedByte))
						patternIndex++;
					else
						patternIndex = 0;

					if (patternIndex == pattern.Length) {
						patternOffset = i - (patternIndex - 1);
						log("Found pattern offset at " + patternOffset);
						return new Tuple<long, byte[]>(patternOffset, pattern);
					}
				}
			}

			return new Tuple<long, byte[]>(-1L, null);
		}*/
	}
}
