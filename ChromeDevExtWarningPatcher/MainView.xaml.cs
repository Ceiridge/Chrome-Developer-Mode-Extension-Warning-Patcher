using ChromeDevExtWarningPatcher.ComponentModels;
using ChromeDevExtWarningPatcher.InstallationFinder;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using ChromeDevExtWarningPatcher.Patches;
using Brush = System.Windows.Media.Brush;

namespace ChromeDevExtWarningPatcher {
	public partial class MainView : Window {
		private readonly MainModel mainModel = new MainModel();
		private readonly InstallationManager installationManager = new InstallationManager();
		private BytePatchManager? bytePatchManager;

		public MainView() {
			this.InitializeComponent();
			this.DataContext = this.mainModel;
		}

		public void Log(string str, Brush? color = null) { // Add a line to the ConsoleBox and scroll to end
			this.ConsoleBox.Dispatcher.Invoke(() => {
				Paragraph logParagraph = new Paragraph();
				if (color != null) {
					logParagraph.Foreground = color;
				}
				logParagraph.Inlines.Add(str);

				this.ConsoleBox.Document.Blocks.Add(logParagraph);
				this.ConsoleBox.ScrollToEnd();
			});
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);
			this.ConsoleBox.Document.Blocks.Clear();

			this.Log("Patcher gui initialized");
			this.Log("Searching for Chromium installations...");

			foreach (InstallationPaths paths in this.installationManager.FindAllChromiumInstallations()) {
				this.AddInstallationPath(paths);
			}

			this.bytePatchManager = new BytePatchManager(MessageBox.Show, this.mainModel.PatchListModel);
		}

		private void AddInstallationPath(InstallationPaths paths) {
			Icon? icon = System.Drawing.Icon.ExtractAssociatedIcon(paths.ChromeExePath!);
			ImageSource? source = icon != null ? Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()) : null; // Turn the icon into an ImageSource for the WPF gui

			this.mainModel.BrowserListModel.ElementList.Add(new SelectionListElement(paths.Name) {
				Description = paths.ChromeDllPath,
				IconImage = source,
				Tooltip = paths.ChromeExePath + " & " + paths.ChromeDllPath,
				IsSelected = true
			});
		}

		private void OnAddCustomPath(object sender, RoutedEventArgs e) {
			InstallationPaths? paths = CustomPath.GuiAddCustomPath();
			if (paths != null) {
				this.AddInstallationPath(paths);
			}
		}

		private void OnExpanderExpand(object sender, RoutedEventArgs e) {
			Expander?[] expanders = { this.BrowserExpander, this.PatchExpander, this.InstallExpander };

			foreach (Expander? expander in expanders) {
				if (expander != null && expander != sender) {
					expander.IsExpanded = false;
				}
			}
		}

		private void OnInstall(object sender, RoutedEventArgs e) {
			this.InstallButton.IsEnabled = this.UninstallButton.IsEnabled = false;


		}
	}
}
