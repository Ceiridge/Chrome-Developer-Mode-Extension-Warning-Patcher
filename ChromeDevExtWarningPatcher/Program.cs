using ChromeDevExtWarningPatcher.Patches;
using System;
using System.Windows;
using CommandLine;
using CommandLine.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using System.Threading;

// Ugly and uncommented code ahead
namespace ChromeDevExtWarningPatcher
{
    class Program
    {
        private static Application guiApp;
        private static Window guiWindow;
        public static BytePatchManager bytePatchManager;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [STAThread]
        public static void Main(string[] args)
        {
            CommandLineOptions clOptions = null;

            if (args.Length > 0) {
                ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options => {
                    clOptions = options;
                });

                if(result.Tag == ParserResultType.NotParsed) {
                    HelpText.AutoBuild(result);
                    return;
                }
            } else {
                FreeConsole();
                bytePatchManager = new BytePatchManager(MessageBox.Show);
                guiApp = new Application();
                guiApp.Run(guiWindow = new PatcherGui());
                return;
            }

            if (clOptions == null)
                return;
            bytePatchManager = new BytePatchManager(CustomConsoleWrite);

            List<string> applicationPaths = new List<string>();
            
            if(clOptions.CustomPath.Length > 0) {
                if(!Directory.Exists(clOptions.CustomPath)) {
                    Console.WriteLine("CustomPath not found");
                    return;
                }

                applicationPaths.AddRange(new CustomPath(clOptions.CustomPath).FindDllFiles());
            } else {
                applicationPaths.AddRange(new InstallationFinder.InstallationManager().FindAllChromiumInstallations());
            }

            bytePatchManager.DisabledGroups.AddRange(clOptions.DisabledGroups);

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