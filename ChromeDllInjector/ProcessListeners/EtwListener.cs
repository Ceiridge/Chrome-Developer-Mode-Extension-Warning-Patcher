using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading;
using Vanara.PInvoke;

namespace ChromeDllInjector.ProcessListeners {
	public class EtwListener : IProcessListener {
		private Action<int> processCallback;

		public void StartListener(Action<int> callback) {
			this.processCallback = callback;

			TraceEventSession kernelSession = new TraceEventSession("ChromePatcherETW");
			kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);
			kernelSession.Source.Kernel.ProcessStart += this.Kernel_ProcessStart;

			new Thread(() => { // Required because of blocking Process() below
				Thread.Sleep(10); // Wait a bit to make sure the ETW started processing

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

		private void Kernel_ProcessStart(ProcessTraceData obj) {
			this.processCallback(obj.ProcessID);
		}
	}
}
