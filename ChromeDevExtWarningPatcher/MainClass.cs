using ChromeDevExtWarningPatcher.ComponentModels;
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

namespace ChromeDevExtWarningPatcher {
	public class MainClass {
		public static BytePatchManager? BytePatchManager;

		private const string ARCH_ERROR = "A 64-bit operating system is required. 32-bit is not supported and won't be in the future.";
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		private static extern bool FreeConsole();

		[STAThread]
		public static void Main(string[] args) {
			bool incompatibleArchitecture = !Environment.Is64BitOperatingSystem;

			if (args.Length == 0) { // No command line arguments given => launch the GUI
				FreeConsole(); // Hide the console to not interfere with the GUI

				if (incompatibleArchitecture) {
					MessageBox.Show(ARCH_ERROR, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				App app = new App();
				app.InitializeComponent();
				app.Run();
			} else {
				if (incompatibleArchitecture) {
					Console.Error.WriteLine(incompatibleArchitecture);
					return;
				}

				MainCmd(args); // Parse the command line and work with it
			}
		}

		private static void MainCmd(string[] args) {
			CommandLineOptions? clOptions = null;
			ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options => {
				clOptions = options;
			});

			if (result.Tag == ParserResultType.NotParsed) { // Show help if wrong/no arguments were given
				HelpText.AutoBuild(result);
				return;
			}

			if (clOptions == null) {
				return;
			}

			SelectionListModel groupModel = new SelectionListModel(); // Required to store the grouped patches
			BytePatchManager = new BytePatchManager(CustomConsoleWrite, groupModel);

			List<InstallationPaths> applicationPaths = new List<InstallationPaths>();
			List<int> groups = new List<int>(clOptions.Groups);
			List<int> disabledGroups = new List<int>();

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

			foreach (SelectionListElement element in groupModel.ElementList) {
				if (element is PatchGroupElement patchGroup && !groups.Contains(patchGroup.Group)) {
					disabledGroups.Add(patchGroup.Group);
				}
			}

			if (applicationPaths.Count == 0) {
				Console.WriteLine("Error: No patchable browser files found!");
				return;
			}

			try {
				PatcherInstaller installer = new PatcherInstaller(applicationPaths);
				installer.Install(Console.WriteLine, disabledGroups);
			} catch (Exception ex) {
				Console.WriteLine("Error while installing patches: " + ex.Message);
			}

			if (!clOptions.NoWait) {
				Thread.Sleep(5000); // Wait a bit to let the user see the result
			}
		}

		private static MessageBoxResult CustomConsoleWrite(string str, string? title = null) {
			Console.WriteLine(str);
			return MessageBoxResult.OK;
		}
	}
}
