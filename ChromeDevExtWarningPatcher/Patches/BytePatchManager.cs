using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Xml.Linq;

namespace ChromeDevExtWarningPatcher.Patches {
	class BytePatchManager {
		public List<BytePatch> BytePatches = new List<BytePatch>();
		private readonly Dictionary<string, BytePatchPattern> bytePatterns = new Dictionary<string, BytePatchPattern>();

		public List<int> DisabledGroups = new List<int>();
		public List<GuiPatchGroupData> PatchGroups = new List<GuiPatchGroupData>();

		public delegate MessageBoxResult WriteLineOrMessageBox(string str, string title);
		public BytePatchManager(WriteLineOrMessageBox log) {
			this.BytePatches.Clear();
			this.bytePatterns.Clear();

			XDocument xmlDoc = null;
			string xmlFile = Program.DEBUG ? @"..\..\..\patterns.xml" : (Path.GetTempPath() + "chrome_patcher_patterns.xml");

			try {
				if (Program.DEBUG) {
					throw new Exception("Forcing to use local patterns.xml");
				}

				using (WebClient web = new WebClient()) {
					ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2, which is required for Github

					string xmlStr;
					xmlDoc = XDocument.Parse(xmlStr = web.DownloadString("https://raw.githubusercontent.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/master/patterns.xml")); // Hardcoded defaults xml file; This makes quick fixes possible

					File.WriteAllText(xmlFile, xmlStr);
				}
			} catch (Exception ex) {
				if (File.Exists(xmlFile)) {
					xmlDoc = XDocument.Parse(File.ReadAllText(xmlFile));
					log("An error occurred trying to fetch the new patterns. The old cached version will be used instead. Expect patch errors.\n\n" + ex.Message, "Warning");
				} else {
					log("An error occurred trying to fetch the new patterns. The program has to exit, as no cached version of this file has been found.\n\n" + ex.Message, "Error");
					Environment.Exit(1);
				}
			}


			// Comma culture setter from https://stackoverflow.com/questions/9160059/set-up-dot-instead-of-comma-in-numeric-values
			CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone(); customCulture.NumberFormat.NumberDecimalSeparator = ".";
			Thread.CurrentThread.CurrentCulture = customCulture;

			float newVersion = float.Parse(xmlDoc.Root.Attribute("version").Value);
			Version myVersion = Assembly.GetCallingAssembly().GetName().Version;

			if (newVersion > float.Parse(myVersion.Major + "." + myVersion.Minor)) {
				log("A new version of this patcher has been found.\nDownload it at:\nhttps://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/releases", "New update available");
			}

			foreach (XElement pattern in xmlDoc.Root.Element("Patterns").Elements("Pattern")) {
				BytePatchPattern patternClass = new BytePatchPattern(pattern.Attribute("name").Value);

				foreach (XElement bytePattern in pattern.Elements("BytePattern")) {
					string[] unparsedBytes = bytePattern.Value.Split(' ');
					byte[] patternBytesArr = new byte[unparsedBytes.Length];

					for (int i = 0; i < unparsedBytes.Length; i++) {
						string unparsedByte = unparsedBytes[i].Equals("?") ? "FF" : unparsedBytes[i];
						patternBytesArr[i] = Convert.ToByte(unparsedByte, 16);
					}
					patternClass.AlternativePatternsX64.Add(patternBytesArr);
				}

				this.bytePatterns.Add(patternClass.Name, patternClass);
			}

			foreach (XElement patch in xmlDoc.Root.Element("Patches").Elements("Patch")) {
				BytePatchPattern pattern = this.bytePatterns[patch.Attribute("pattern").Value];
				int group = int.Parse(patch.Attribute("group").Value);

				byte origX64 = 0, patchX64 = 0;
				List<int> offsetsX64 = new List<int>();
				int sigOffset = 0;
				bool sig = false;

				foreach (XElement patchData in patch.Elements("PatchData")) {
					foreach (XElement offsetElement in patchData.Elements("Offset")) {
						offsetsX64.Add(Convert.ToInt32(offsetElement.Value.Replace("0x", ""), 16));
					}

					origX64 = Convert.ToByte(patchData.Attribute("orig").Value.Replace("0x", ""), 16);
					patchX64 = Convert.ToByte(patchData.Attribute("patch").Value.Replace("0x", ""), 16);
					sig = Convert.ToBoolean(patchData.Attribute("sig").Value);
					if (patchData.Attributes("sigOffset").Any()) {
						sigOffset = Convert.ToInt32(patchData.Attribute("sigOffset").Value.Replace("0x", ""), 16);
					}
					break;
				}

				this.BytePatches.Add(new BytePatch(pattern, origX64, patchX64, offsetsX64, @group, sig, sigOffset));
			}

			foreach (XElement patchGroup in xmlDoc.Root.Element("GroupedPatches").Elements("GroupedPatch")) {
				this.PatchGroups.Add(new GuiPatchGroupData {
					Group = int.Parse(patchGroup.Attribute("group").Value),
					Default = bool.Parse(patchGroup.Attribute("default").Value),
					Name = patchGroup.Element("Name").Value,
					Tooltip = patchGroup.Element("Tooltip").Value
				});
			}
		}
	}
}
