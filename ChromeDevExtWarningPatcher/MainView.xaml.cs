using System.Windows;
using ChromeDevExtWarningPatcher.ComponentModels;

namespace ChromeDevExtWarningPatcher {
	public partial class MainView : Window {
		private readonly MainModel mainModel = new MainModel();

		public MainView() {
			this.InitializeComponent();
			this.DataContext = this.mainModel;
		}
	}
}
