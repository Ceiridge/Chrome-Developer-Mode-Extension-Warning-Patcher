using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ChromeDllInjector.ProcessListeners;
using Vanara.PInvoke;

namespace ChromeDllInjector {
	class Program {
		private static readonly List<string> ChromeExeFilePaths = new List<string>();
		private static Injector Injector;
		private static IProcessListener Listener;

		static void Main(string[] _) {
			try {
#if !DEBUG
				RedirectConsole();
#endif
				CreateInjector();
				LoadChromeExePaths();
			} catch (Exception e) {
				Console.WriteLine("Couldn't initialize: " + e.Message);
				Environment.Exit(1);
			}

			HINSTANCE ntDllInstance = Kernel32.GetModuleHandle("ntdll.dll");
			if (ntDllInstance.IsNull) {
				Console.WriteLine("Ntdll.dll not found!");
				Environment.Exit(1);
			}

			// Check if the OS supports the ETW listener library (otherwise use the compatibility mode for Windows 7)
			if (Kernel32.GetProcAddress(ntDllInstance, "RtlGetDeviceFamilyInfoEnum") == IntPtr.Zero) {
				Console.WriteLine("ETWs not supported. Having to use compatibility mode!");
				Listener = new CompatibleListener();
			} else {
				Console.WriteLine("ETWs are supported.");
				Listener = new EtwListener();
			}

			Listener.StartListener(OnProcessStart); // Blocking forever
		}

		private static void OnProcessStart(int processId) {
			try {
				Process proc = Process.GetProcessById(processId);

				foreach (string chromePath in ChromeExeFilePaths) {
					if (chromePath.Equals(proc.MainModule.FileName)) {
						Console.WriteLine("Injecting into " + proc.Id);
						Injector.Inject(proc);
						break;
					}
				}
			} catch (Exception e) { // Ignore some rare errors (often occur for very short lived processes)
				Console.WriteLine(e.Message);
			}
		}

		// Init functions to make the code cleaner
		private static void RedirectConsole() {
			Kernel32.FreeConsole(); // Hide the console window

			StreamWriter writer;
			Console.SetOut(writer = new StreamWriter(new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Temp\ChromePatcherInjector.log", FileMode.Append, FileAccess.Write, FileShare.Read))); // Redirect to C:\Windows\Temp\ChromePatcherInjector.log
			writer.AutoFlush = true;

			Console.WriteLine(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		}

		private static void CreateInjector() {
			DirectoryInfo curDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			FileInfo dll = curDir.EnumerateFiles().Where(file => file.Name.EndsWith(".dll") && file.Name.StartsWith("ChromePatcherDll_")).OrderByDescending(file => file.LastWriteTimeUtc).First(); // Select the latest Chrome patcher dll

			Console.WriteLine("Using injector with " + dll.FullName);
			Injector = new Injector(dll.FullName);
		}

		private static void LoadChromeExePaths() {
			using (RegistryKey exeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ceiridge\ChromePatcher\ChromeExes")) {
				foreach (string name in exeKey.GetValueNames()) {
					if (name.Length >= 1) {
						string value = exeKey.GetValue(name, "").ToString();

						if (value.Length > 5) {
							ChromeExeFilePaths.Add(value);
						}
					}
				}
			}
		}
	}
}
