using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ChromeDllInjector.ProcessListeners {
	// The purpose of this listener is that it should also run on Windows 7, which has problems with the EtwListener.
	// One side effect is a much higher CPU usage.
	public class CompatibleListener : IProcessListener {
		private readonly HashSet<UniquePid> checkedProcesses = new HashSet<UniquePid>(); // PIDs can be reused and the start time makes sure that it is actually unique.

		public void StartListener(Action<int> callback) {
			while (true) {
				Thread.Sleep(50);

				foreach (Process process in Process.GetProcesses()) {
					try {
						UniquePid proUniquePid = new UniquePid(process.Id, process.StartTime);

						if (!this.checkedProcesses.Contains(proUniquePid)) {
							this.checkedProcesses.Add(proUniquePid);
							callback(proUniquePid.Pid); // Only call once for each (newly) started process
						}
					} catch (Exception) {} // Ignore errors that might occur while trying to get the start time of system processes
				}
			}
		}

		private class UniquePid : IEquatable<UniquePid> {
			public readonly int Pid;
			public readonly DateTime StartTime;

			public UniquePid(int pid, DateTime startTime) {
				this.Pid = pid;
				this.StartTime = startTime;
			}

			public override int GetHashCode() {
				return (this.Pid, this.StartTime).GetHashCode();
			}

			public bool Equals(UniquePid other) {
				return other != null && (other.Pid, other.StartTime).Equals((this.Pid, this.StartTime));
			}
		}
	}
}
