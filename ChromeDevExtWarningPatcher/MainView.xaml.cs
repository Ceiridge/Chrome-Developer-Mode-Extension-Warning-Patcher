using ChromeDevExtWarningPatcher.ComponentModels;
using ChromeDevExtWarningPatcher.InstallationFinder;
using ChromeDevExtWarningPatcher.InstallationFinder.Defaults;
using ChromeDevExtWarningPatcher.Patches;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace ChromeDevExtWarningPatcher {
	public partial class MainView : Window {
		private readonly MainModel mainModel = new MainModel();
		private readonly InstallationManager installationManager = new InstallationManager();
		private static readonly Brush GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(148, 148, 148));

		public MainView() {
			this.InitializeComponent();
			this.DataContext = this.mainModel;
		}

		public void Log(string str, Brush? color) { // Add a line to the ConsoleBox and scroll to end
			this.ConsoleBox.Dispatcher.Invoke(() => {
				Paragraph logParagraph = new Paragraph {
					Foreground = color ?? GRAY_BRUSH
				};
				logParagraph.Inlines.Add(str);

				this.ConsoleBox.Document.Blocks.Add(logParagraph);
				this.ConsoleBox.ScrollToEnd();
			});
		}

		private void Log(string str) { // For delegates
			this.Log(str, null);
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);
			this.ConsoleBox.Document.Blocks.Clear();

			this.Log("Patcher gui initialized");
			this.Log("Searching for Chromium installations...");

			foreach (InstallationPaths paths in this.installationManager.FindAllChromiumInstallations()) {
				this.AddInstallationPath(paths);
			}

			MainClass.BytePatchManager = new BytePatchManager(MessageBox.Show, this.mainModel.PatchListModel);
		}

		private void AddInstallationPath(InstallationPaths paths) {
			Icon? icon = System.Drawing.Icon.ExtractAssociatedIcon(paths.ChromeExePath!);
			ImageSource? source = icon != null ? Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()) : null; // Turn the icon into an ImageSource for the WPF gui

			this.mainModel.BrowserListModel.ElementList.Add(new InstallationElement(paths.Name, paths) {
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

		private void DisableButtons(bool disable) {
			this.InstallButton.Dispatcher.Invoke(() => {
				this.InstallButton.IsEnabled = this.UninstallButton.IsEnabled = !disable;
			});
		}

		private List<InstallationPaths> GetEnabledInstallationPaths() {
			List<InstallationPaths> installPaths = new List<InstallationPaths>();
			foreach (SelectionListElement element in this.mainModel.BrowserListModel.ElementList) {
				if (element is InstallationElement { IsSelected: true } installation) {
					installPaths.Add(installation.Paths);
				}
			}
			return installPaths;
		}

		private void OnInstall(object sender, RoutedEventArgs e) {
			this.DisableButtons(true);

			List<int> disabledGroups = new List<int>(); // Get all disabled patch groups from the UI
			foreach (SelectionListElement element in this.mainModel.PatchListModel.ElementList) {
				if (element is PatchGroupElement { IsSelected: false } patchGroup) {
					disabledGroups.Add(patchGroup.Group);
				}
			}

			new Thread((() => {
				try {
					List<InstallationPaths> installPaths = this.GetEnabledInstallationPaths();
					PatcherInstaller installer = new PatcherInstaller(installPaths);

					if (installer.Install(this.Log, disabledGroups)) {
						foreach (InstallationPaths paths in installPaths) {
							this.Log($"Successfully installed to {paths.ChromeExePath}", Brushes.Green);
						}
					}
				} catch (Exception exception) {
					this.Log("Error while installing: " + exception.Message, Brushes.Red);
				}

				this.DisableButtons(false);
			})).Start();
		}

		private void OnUninstall(object sender, RoutedEventArgs e) {
			this.DisableButtons(true);

			new Thread((() => {
				try {
					List<InstallationPaths> installPaths = this.GetEnabledInstallationPaths();
					PatcherInstaller installer = new PatcherInstaller(installPaths);

					if (installer.UninstallAll(this.Log)) {
						foreach (InstallationPaths paths in installPaths) {
							this.Log($"Successfully uninstalled from {paths.ChromeExePath}", Brushes.Green);
						}
					}
				} catch (Exception exception) {
					this.Log("Error while uninstalling: " + exception.Message, Brushes.Red);
				}

				this.DisableButtons(false);
			})).Start();
		}
	}
}
