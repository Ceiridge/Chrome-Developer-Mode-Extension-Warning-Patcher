using ChromeDevExtWarningPatcher.InstallationFinder;
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

// Uncommented code ahead; Code quality may vary because of three partial recodes
namespace ChromeDevExtWarningPatcher {
	class Program {
		private static Application guiApp;
		private static Window guiWindow;
		public static BytePatchManager bytePatchManager;

		public const bool DEBUG = false;

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		private static extern bool FreeConsole();

		private const string archError = "A 64-bit operating system is required. 32-bit is not supported and won't be in the future.";
		[STAThread]
		public static void Main(string[] args) {
			bool incompatibleArchitecture = !Environment.Is64BitOperatingSystem;

			if (args.Length == 0) {
				FreeConsole();
				
				if(incompatibleArchitecture) {
					MessageBox.Show(archError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				bytePatchManager = new BytePatchManager(MessageBox.Show);
				guiApp = new Application();
				guiApp.Run(guiWindow = new PatcherGui());
			} else {
				if(incompatibleArchitecture) {
					Console.WriteLine(archError);
					return;
				}

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

			List<InstallationPaths> applicationPaths = new List<InstallationPaths>();
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

				applicationPaths.AddRange(new CustomPath(clOptions.CustomPath).FindInstallationPaths());
			} else {
				applicationPaths.AddRange(new InstallationFinder.InstallationManager().FindAllChromiumInstallations());
			}

			foreach (GuiPatchGroupData patchData in bytePatchManager.PatchGroups) {
				if (!groups.Contains(patchData.Group))
					bytePatchManager.DisabledGroups.Add(patchData.Group);
			}

			if (applicationPaths.Count == 0)
				Console.WriteLine("Error: No patchable browser files found!");

			try {
				PatcherInstaller installer = new PatcherInstaller(applicationPaths);
				installer.Install(Console.WriteLine);
			} catch (Exception ex) {
				Console.WriteLine("Error while installing patches: " + ex.Message);
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

