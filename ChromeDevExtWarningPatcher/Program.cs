using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using ChromeDevExtWarningPatcher.Patches;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

// Ugly and uncommented code ahead
namespace ChromeDevExtWarningPatcher {
	class Program {
		private static Application guiApp;
		private static Window guiWindow;
		public static BytePatchManager bytePatchManager;

		public const bool DEBUG = false;

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		private static extern bool FreeConsole();

		[STAThread]
		public static void Main(string[] args) {
			if (args.Length == 0) {
				FreeConsole();
				bytePatchManager = new BytePatchManager(MessageBox.Show);
				guiApp = new Application();
				guiApp.Run(guiWindow = new PatcherGui());
			} else {
				MainCmd(args);
			}
		}

		public static void MainCmd(string[] args) {
			CommandLineOptions clOptions = null;
			ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options => {
				clOptions = options;
			});

			if (result.Tag == ParserResultType.NotParsed) {
				HelpText.AutoBuild(result);
				return;
			}

			if (clOptions == null)
				return;
			bytePatchManager = new BytePatchManager(CustomConsoleWrite);

			List<string> applicationPaths = new List<string>();
			List<int> groups = new List<int>(clOptions.Groups);

			if (groups.Count == 0) {
				Console.WriteLine("Groups need to be defined. Use --help for help.");
				return;
			}

			if (clOptions.CustomPath != null && clOptions.CustomPath.Length > 0) {
				if (!Directory.Exists(clOptions.CustomPath)) {
					Console.WriteLine("CustomPath not found");
					return;
				}

				applicationPaths.AddRange(new CustomPath(clOptions.CustomPath).FindDllFiles());
			} else {
				applicationPaths.AddRange(new InstallationFinder.InstallationManager().FindAllChromiumInstallations());
			}

			foreach (GuiPatchGroupData patchData in bytePatchManager.PatchGroups) {
				if (!groups.Contains(patchData.Group))
					bytePatchManager.DisabledGroups.Add(patchData.Group);
			}

			if (applicationPaths.Count == 0)
				Console.WriteLine("Error: No patchable dll file found!");

			foreach (string path in applicationPaths) {
				try {
					DllPatcher patcher = new DllPatcher(path);
					patcher.Patch(Console.WriteLine);
				} catch (Exception ex) {
					Console.WriteLine("Error while patching " + path + ":" + ex.Message);
				}
			}

			if (!clOptions.NoWait)
				Thread.Sleep(5000); // Wait a bit to let the user see the result
		}

		private static MessageBoxResult CustomConsoleWrite(string str, string arg2 = "") {
			Console.WriteLine(str);
			return MessageBoxResult.OK;
		}
	}
}