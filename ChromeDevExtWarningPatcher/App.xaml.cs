using System;
using System.Windows;
using ChromeDevExtWarningPatcher.Patches;

namespace ChromeDevExtWarningPatcher {
	public partial class App : Application {
		public static BytePatchManager? BytePatchManager;

		public App() {
			string[] args = Environment.GetCommandLineArgs(); // First element is the program's name

			if (args.Length <= 1) { // Start gui
				return;
			}

			BytePatchManager = new BytePatchManager(CustomConsoleWrite);
			Environment.Exit(0); // Exit to prevent the UI from starting
		}

		private static MessageBoxResult CustomConsoleWrite(string str, string? title = null) {
			Console.WriteLine(str);
			return MessageBoxResult.OK;
		}
	}
}
