using System.ComponentModel.DataAnnotations;

namespace ChromeDevExtWarningPatcher.ComponentModels {
	public class PatchGroupElement : SelectionListElement {
		[Required]
		public int Group { get; set; }

		public PatchGroupElement(string name, int group) : base(name) {
			this.Group = group;
		}
	}
}
