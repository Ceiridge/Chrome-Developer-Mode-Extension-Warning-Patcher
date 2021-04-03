using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace ChromeDevExtWarningPatcher.ComponentModels {
	public class SelectionListElement : INotifyPropertyChanged {
		[Required]
		public string Name { get; set; }
		public string? Description { get; set; }
		public Image? IconImage { get; set; }

		private bool isSelected;
		public bool IsSelected {
			get => this.isSelected;
			set {
				this.isSelected = value;
				this.OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public SelectionListElement(string name) {
			this.Name = name;
		}

		protected internal void OnPropertyChanged([CallerMemberName] string propertyName = "") {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
