using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using System.Collections.Generic;

namespace ChromeDevExtWarningPatcher.InstallationFinder {
	class InstallationManager {
		private List<Installation> installationFinders = new List<Installation>();

		public InstallationManager() {
			installationFinders.Clear();
			installationFinders.Add(new Chrome());
			installationFinders.Add(new Brave());
			installationFinders.Add(new Edge());
		}

		public List<string> FindAllChromiumInstallations() {
			List<string> installations = new List<string>();

			foreach (Installation installation in installationFinders) {
				installations.AddRange(installation.FindDllFiles());
			}

			return installations;
		}
	}
}
