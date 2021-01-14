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
		public static BytePatchManager BytePatchManager;

		public const bool DEBUG = false;

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		private static extern bool FreeConsole();

		private const string ARCH_ERROR = "A 64-bit operating system is required. 32-bit is not supported and won't be in the future.";
		[STAThread]
		public static void Main(string[] args) {
			bool incompatibleArchitecture = !Environment.Is64BitOperatingSystem;

			if (args.Length == 0) {
				FreeConsole();

				if (incompatibleArchitecture) {
					MessageBox.Show(ARCH_ERROR, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				BytePatchManager = new BytePatchManager(MessageBox.Show);
				guiApp = new Application();
				guiApp.Run(guiWindow = new PatcherGui());
			} else {
				if (incompatibleArchitecture) {
					Console.WriteLine(ARCH_ERROR);
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

			if (clOptions == null) {
				return;
			}

			BytePatchManager = new BytePatchManager(CustomConsoleWrite);

			List<InstallationPaths> applicationPaths = new List<InstallationPaths>();
			List<int> groups = new List<int>(clOptions.Groups);

			if (groups.Count == 0) {
				Console.WriteLine("Groups need to be defined. Use --help for help.");
				return;
			}

			if (!string.IsNullOrEmpty(clOptions.CustomPath)) {
				if (!Directory.Exists(clOptions.CustomPath)) {
					Console.WriteLine("CustomPath not found");
					return;
				}

				applicationPaths.AddRange(new CustomPath(clOptions.CustomPath).FindInstallationPaths());
			} else {
				applicationPaths.AddRange(new InstallationManager().FindAllChromiumInstallations());
			}

			foreach (GuiPatchGroupData patchData in BytePatchManager.PatchGroups) {
				if (!groups.Contains(patchData.Group)) {
					BytePatchManager.DisabledGroups.Add(patchData.Group);
				}
			}

			if (applicationPaths.Count == 0) {
				Console.WriteLine("Error: No patchable browser files found!");
			}

			try {
				PatcherInstaller installer = new PatcherInstaller(applicationPaths);
				installer.Install(Console.WriteLine);
			} catch (Exception ex) {
				Console.WriteLine("Error while installing patches: " + ex.Message);
			}

			if (!clOptions.NoWait) {
				Thread.Sleep(5000); // Wait a bit to let the user see the result
			}
		}

		private static MessageBoxResult CustomConsoleWrite(string str, string arg2 = "") {
			Console.WriteLine(str);
			return MessageBoxResult.OK;
		}
	}
}

