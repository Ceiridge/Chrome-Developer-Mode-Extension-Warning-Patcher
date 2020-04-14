using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Xml.Linq;

namespace ChromeDevExtWarningPatcher.Patches {
    class BytePatchManager {
        private List<BytePatch> BytePatches = new List<BytePatch>();
        private Dictionary<string, BytePatchPattern> BytePatterns = new Dictionary<string, BytePatchPattern>();

        public List<int> DisabledGroups = new List<int>();

        public BytePatchManager() {
            BytePatches.Clear();
            BytePatterns.Clear();

            XDocument xmlDoc = null;
            string xmlFile = Path.GetTempPath() + "chrome_patcher_patterns.xml";

            try {
                using (WebClient web = new WebClient()) {
                    string xmlStr;
                    xmlDoc = XDocument.Parse(xmlStr = web.DownloadString("https://raw.githubusercontent.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/master/patterns.xml")); // Hardcoded defaults xml file; This makes quick fixes possible

                    File.WriteAllText(xmlFile, xmlStr);
                }
            } catch (Exception ex) {
                if(File.Exists(xmlFile)) {
                    xmlDoc = XDocument.Parse(File.ReadAllText(xmlFile));
                    MessageBox.Show("An error occurred trying to fetch the new patterns. The old cached version will be used instead. Expect patch errors.\n\n" + ex.Message, "Warning");
                } else {
                    MessageBox.Show("An error occurred trying to fetch the new patterns. The program has to exit, as no cached version of this file has been found.\n\n" + ex.Message, "Error");
                    Environment.Exit(1);
                }
            }


            if(xmlDoc != null) {
                // Comma culture setter from https://stackoverflow.com/questions/9160059/set-up-dot-instead-of-comma-in-numeric-values
                CultureInfo customCulture = (CultureInfo) Thread.CurrentThread.CurrentCulture.Clone();                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = customCulture;

                float newVersion = float.Parse(xmlDoc.Root.Attribute("version").Value);
                if(newVersion > Program.VERSION) {
                    MessageBox.Show("A new version of this patcher has been found.\nDownload it at:\nhttps://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/releases", "New update available");
                }

                foreach(XElement pattern in xmlDoc.Root.Element("Patterns").Elements("Pattern")) {
                    BytePatchPattern patternClass = new BytePatchPattern(pattern.Attribute("name").Value);

                    foreach (XElement patternList in pattern.Elements("BytePatternList")) {
                        bool isX64 = patternList.Attribute("type").Value.Equals("x64");

                        foreach(XElement bytePattern in patternList.Elements("BytePattern")) {
                            string[] unparsedBytes = bytePattern.Value.Split(' ');
                            byte[] patternBytesArr = new byte[unparsedBytes.Length];

                            for(int i = 0; i < unparsedBytes.Length; i++) {
                                string unparsedByte = unparsedBytes[i].Equals("?") ? "FF" : unparsedBytes[i];
                                patternBytesArr[i] = Convert.ToByte(unparsedByte, 16);
                            }
                            (isX64 ? patternClass.AlternativePatternsX64 : patternClass.AlternativePatternsX86).Add(patternBytesArr);
                        }
                    }
                    BytePatterns.Add(patternClass.Name, patternClass);
                }

                foreach(XElement patch in xmlDoc.Root.Element("Patches").Elements("Patch")) {
                    BytePatchPattern pattern = BytePatterns[patch.Attribute("pattern").Value];
                    int group = int.Parse(patch.Attribute("group").Value);

                    byte origX64 = 0, origX86 = 0, patchX64 = 0, patchX86 = 0;
                    int offsetX64 = 0, offsetX86 = 0, aoffsetX64 = -1, aoffsetX86 = -1;

                    foreach(XElement patchData in patch.Elements("PatchData")) {
                        byte orig = Convert.ToByte(patchData.Attribute("orig").Value.Replace("0x", ""), 16);
                        byte patchB = Convert.ToByte(patchData.Attribute("patch").Value.Replace("0x", ""), 16);
                        int offset = Convert.ToInt32(patchData.Attribute("offset").Value.Replace("0x", ""), 16);

                        XAttribute alternativeOffsetAttr = patchData.Attribute("alternativeOffset");
                        int aoffset = alternativeOffsetAttr == null ? -1 : Convert.ToInt32(alternativeOffsetAttr.Value.Replace("0x", ""), 16);

                        if (patchData.Attribute("type").Value.Equals("x64")) {
                            origX64 = orig;
                            patchX64 = patchB;
                            offsetX64 = offset;
                            aoffsetX64 = aoffset;
                        } else {
                            origX86 = orig;
                            patchX86 = patchB;
                            offsetX86 = offset;
                            aoffsetX86 = aoffset;
                        }
                    }
                    BytePatches.Add(new BytePatch(pattern, origX64, patchX64, offsetX64, aoffsetX64, origX86, patchX86, offsetX86, aoffsetX86, group));
                }
            }
        }

        public bool PatchBytes(ref byte[] raw, bool x64, BytePatchPattern.WriteToLog log) {
            int patches = 0;

            foreach(BytePatch patch in BytePatches) {
                if (DisabledGroups.Contains(patch.group)) {
                    patches++;
                    continue;
                }
                long addr = patch.pattern.FindAddress(raw, x64, log);
                int patchOffset = x64 ? patch.offsetX64 : patch.offsetX86;
                byte patchOrigByte = x64 ? patch.origByteX64 : patch.origByteX86;
                byte patchPatchByte = x64 ? patch.patchByteX64 : patch.patchByteX86;

                if(addr != -1) {
REDO_CHECKS:
                    long index = addr + patchOffset;
                    byte sourceByte = raw[index];

                    log("Source byte of patch at " + patchOffset + ": " + sourceByte);
                    if (sourceByte == patchOrigByte) {
                        raw[index] = patchPatchByte;
                        log(index + " => " + patchPatchByte);
                        patches++;
                    } else {
                        int patchAlternativeOffset = x64 ? patch.aoffsetX64 : patch.aoffsetX86;
                        if(patchOffset != patchAlternativeOffset && patchAlternativeOffset != -1) { // if the first offset didn't work, try the next one
                            patchOffset = patchAlternativeOffset;
                            goto REDO_CHECKS;
                        }

                        log("Source byte unexpected, should be " + patchOrigByte + "!");
                    }
                } else {
                    log("Couldn't find offset for a patch " + patch.pattern.Name);
                }
            }

            return patches == BytePatches.Count;
        }
    }
}
