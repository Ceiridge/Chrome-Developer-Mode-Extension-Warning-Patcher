using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;

namespace ChromeDllInjector {
	class Injector {
		private readonly string DllPath;

		public Injector(string dllPath) {
			DllPath = dllPath;
		}

		// Inject the dll with by creating a remote thread on LoadLibraryW
		public void Inject(Process p) {
			Kernel32.SafeHPROCESS proc = Kernel32.OpenProcess(new ACCESS_MASK(Kernel32.ProcessAccess.PROCESS_ALL_ACCESS), false, (uint)p.Id);

			if (!proc.IsNull && !proc.IsInvalid) {
				IntPtr loadLib = Kernel32.GetProcAddress(Kernel32.LoadLibrary("kernel32.dll"), "LoadLibraryW");
				if (loadLib == IntPtr.Zero) {
					Console.WriteLine("LoadLibaryW not found");
					return;
				}

				byte[] DllPathBytes = DllPath.GetBytes(true, CharSet.Unicode); // Unicode for wide chars
				IntPtr alloc = Kernel32.VirtualAllocEx(proc, IntPtr.Zero, DllPathBytes.Length, Kernel32.MEM_ALLOCATION_TYPE.MEM_COMMIT, Kernel32.MEM_PROTECTION.PAGE_EXECUTE_READWRITE);
				if (alloc == IntPtr.Zero) {
					Console.WriteLine("Couldn't allocate");
					return;
				}
				if (!Kernel32.WriteProcessMemory(proc, alloc, DllPathBytes, DllPathBytes.Length, out _)) {
					Console.WriteLine("Couldn't write " + Kernel32.GetLastError());
					return;
				}

				Kernel32.SafeHTHREAD thread = Kernel32.CreateRemoteThread(proc, null, 0, loadLib, alloc, 0, out uint threadId);

				if (!thread.IsNull && !thread.IsInvalid) {
					Console.WriteLine("Injected!");
					Kernel32.WaitForSingleObject(thread, Kernel32.INFINITE);
					thread.Close();
				}
			}

			proc.Close();
		}
	}
}
