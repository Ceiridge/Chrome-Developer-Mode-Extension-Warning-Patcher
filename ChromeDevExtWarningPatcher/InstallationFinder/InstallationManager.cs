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
			installationFinders.Add(new Yandex());
		}

		public List<InstallationPaths> FindAllChromiumInstallations() {
			List<InstallationPaths> installations = new List<InstallationPaths>();

			foreach (Installation installation in installationFinders) {
				installations.AddRange(installation.FindInstallationPaths());
			}

			return installations;
		}
	}
}
