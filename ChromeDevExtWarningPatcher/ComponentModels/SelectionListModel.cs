using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ChromeDevExtWarningPatcher.ComponentModels {
	public class SelectionListModel {
		[Required]
		public ObservableCollection<SelectionListElement> ElementList { get; set; } = new ObservableCollection<SelectionListElement>();
	}
}
