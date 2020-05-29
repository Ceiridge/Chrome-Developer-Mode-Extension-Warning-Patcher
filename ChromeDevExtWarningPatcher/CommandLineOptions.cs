using CommandLine;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher {
	public class CommandLineOptions {
		[Option("groups", Required = false, HelpText = "Set what patch groups you want to use. See patterns.xml to get the group ids (comma-seperated: 0,1,2)", Separator = ',')]
		public IEnumerable<int> Groups { get; set; }

		[Option('w', "noWait", Required = false, HelpText = "Disable the almost-pointless wait after finishing")]
		public bool NoWait { get; set; }

		[Option("customPath", Required = false, HelpText = "Instead of automatically detecting and patching all chrome.dll files, define a custom Application-folder path (see README) (string in quotes is recommended)")]
		public string CustomPath { get; set; }
	}
}
