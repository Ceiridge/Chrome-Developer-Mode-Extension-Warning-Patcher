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
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.PInvoke;

namespace ChromeDllInjector {
	class Program {
		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private static readonly List<string> ChromeExeFilePaths = new List<string>();
		private static Injector Injector;

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

			TraceEventSession kernelSession = new TraceEventSession("ChromePatcherETW");
			kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);
			kernelSession.Source.Kernel.ProcessStart += Kernel_ProcessStart;

			new Thread(delegate () { // Required because of blocking Process() below
				Thread.Sleep(100); // Wait a bit to make sure the ETW started processing

				AdvApi32.EVENT_TRACE_PROPERTIES properties = new AdvApi32.EVENT_TRACE_PROPERTIES {
					Wnode = new AdvApi32.WNODE_HEADER {
						BufferSize = 1024 // Max buffer size
					}
				};

				AdvApi32.QueryTrace(0 /* NULL Handle */, kernelSession.SessionName, ref properties); // Fill the struct with info
				Console.WriteLine("Flush thread started: " + properties.Wnode.Guid);

				while (true) {
					Thread.Sleep(50); // Flush the ETW buffer every 50ms to be faster than Chromium starting up; The default flush timer is set to 1s (=> too slow)
					try {
						AdvApi32.FlushTrace(0 /* NULL Handle */, kernelSession.SessionName, ref properties);
					} catch (Exception) { }
				}
			}).Start();

			Console.WriteLine("Starting to process");
			kernelSession.Source.Process(); // Blocking forever
		}

		private static void Kernel_ProcessStart(ProcessTraceData obj) {
			try {
				Process proc = Process.GetProcessById(obj.ProcessID);

				foreach (string chromePath in ChromeExeFilePaths) {
					if (chromePath.Equals(proc.MainModule.FileName)) {
						Console.WriteLine("Injecting into " + proc.Id);
						Injector.Inject(proc);
						break;
					}
				}
			} catch (Exception e) { // Ignore some rare errors (often occurr for very short lived processes)
				Console.WriteLine(e.Message);
			}
		}

		// Init functions to make the code cleaner
		private static void RedirectConsole() {
			ShowWindow(Kernel32.GetConsoleWindow().DangerousGetHandle(), 0); // Hide the console window

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
