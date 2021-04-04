using ChromeDevExtWarningPatcher.InstallationFinder;

namespace ChromeDevExtWarningPatcher.ComponentModels {
	public class InstallationElement : SelectionListElement {
		public InstallationPaths Paths { get; set; }

		public InstallationElement(string name, InstallationPaths paths) : base(name) {
			this.Paths = paths;
		}
	}
}
