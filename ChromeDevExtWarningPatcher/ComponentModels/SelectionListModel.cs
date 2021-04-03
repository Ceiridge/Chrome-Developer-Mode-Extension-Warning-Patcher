using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ChromeDevExtWarningPatcher.ComponentModels {
	public class SelectionListModel {
		[Required]
		public ObservableCollection<SelectionListElement> ElementList { get; set; } = new ObservableCollection<SelectionListElement>();

		public SelectionListModel() {
			Task.Run(async () => {
				await Task.Delay(100);
				this.ElementList.Add(new SelectionListElement("Edge") {
					Description = "Very edgy"
				});
			});
		}
	}
}
