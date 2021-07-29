using System;

namespace ChromeDllInjector {
	public interface IProcessListener {
		
		// This function should block the thread after being called
		void StartListener(Action<int> callback);
	}
}
