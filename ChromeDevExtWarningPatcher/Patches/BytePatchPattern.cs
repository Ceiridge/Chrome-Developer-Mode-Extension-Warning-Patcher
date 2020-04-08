using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChromeDevExtWarningPatcher.Patches {
    public abstract class BytePatchPattern {
        public string Name;
        protected List<byte[]> AlternativePatternsX86 = new List<byte[]>();
        protected List<byte[]> AlternativePatternsX64 = new List<byte[]>();

        public BytePatchPattern(string name) {
            Name = name;
        }

        public delegate void WriteToLog(string str);
        public long FindAddress(byte[] raw, bool x64, WriteToLog log) {
            foreach (byte[] pattern in (x64 ? AlternativePatternsX64 : AlternativePatternsX86)) {
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
                        return patternOffset;
                    }
                }
            }

            return -1L;
        }
    }
}
