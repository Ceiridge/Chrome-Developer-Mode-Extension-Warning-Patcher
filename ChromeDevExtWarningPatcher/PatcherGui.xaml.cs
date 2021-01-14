using ChromeDevExtWarningPatcher.InstallationFinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChromeDevExtWarningPatcher {
	public partial class PatcherGui : Window {

		public PatcherGui() {
			this.InitializeComponent();
		}

		private void SelectFolderBtn_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog openFile = new OpenFileDialog {
				Title = "Select a chrome.dll",
				Filter = "chrome.dll/msedge.dll file (chrome.dll;msedge.dll)|chrome.dll;msedge.dll|Alternative chrome.dll file (*.dll)|*.dll|All files (*.*)|*.*",
				FilterIndex = 1
			};
			openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;

			if (openFile.ShowDialog(this) != true) { // No, I'm not a noob, I have to do it like this and further below
				return;
			}

			string chromeDllPath = openFile.FileName;

			openFile = new OpenFileDialog {
				Title = "Select a chrome.exe",
				Filter = "Browser executable file (*.exe)|*.exe|All files (*.*)|*.*",
				FilterIndex = 1
			};
			openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;
			openFile.InitialDirectory = Directory.GetParent(Path.GetDirectoryName(chromeDllPath)).FullName;

			if (openFile.ShowDialog(this) != true) {
				return;
			}

			string chromeExePath = openFile.FileName;
			this.AddChromiumInstallation(new InstallationPaths(chromeDllPath, chromeExePath), false);
		}

		private void PatchBtn_Click(object sender, RoutedEventArgs e) {
			this.PatchBtn.IsEnabled = this.UnPatchBtn.IsEnabled = false;

			Program.BytePatchManager.DisabledGroups.Clear();
			foreach (CustomCheckBox patchBox in this.PatchGroupList.Items) {
				if (patchBox.IsChecked == false) {
					Program.BytePatchManager.DisabledGroups.Add(patchBox.Group);
				}
			}

			try {
				List<InstallationPaths> installPaths = new List<InstallationPaths>();
				foreach (CustomCheckBox installBox in this.InstallationList.Items) {
					if (installBox.IsChecked == true) {
						installPaths.Add(new InstallationPaths(installBox.Content.ToString(), installBox.ToolTip.ToString().Split(new string[] { " & " }, StringSplitOptions.None)[1]));
					}
				}

				PatcherInstaller installer = new PatcherInstaller(installPaths);
				if (installer.Install(this.Log)) {
					foreach (CustomCheckBox installBox in this.InstallationList.Items) {
						if (installBox.IsChecked == true) {
							installBox.Foreground = installBox.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 207, 133));
						}
					}
				}
			} catch (Exception ex) {
				this.Log("Error while installing:" + ex.Message);
			}

			this.PatchBtn.IsEnabled = this.UnPatchBtn.IsEnabled = true;
		}

		private void UnPatchBtn_Click(object sender, RoutedEventArgs e) {
			this.PatchBtn.IsEnabled = this.UnPatchBtn.IsEnabled = false;

			try {
				List<InstallationPaths> installPaths = new List<InstallationPaths>();
				foreach (CustomCheckBox installBox in this.InstallationList.Items) {
					installPaths.Add(new InstallationPaths(installBox.Content.ToString(), installBox.ToolTip.ToString().Split(new string[] { " & " }, StringSplitOptions.None)[1])); // chromeExePath is always in the ToolTip after " & "
				}

				PatcherInstaller installer = new PatcherInstaller(installPaths);
				if (installer.UninstallAll(this.Log)) {
					foreach (CustomCheckBox installBox in this.InstallationList.Items) {
						if (installBox.IsChecked == true) {
							installBox.Foreground = installBox.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 207, 133));
						}
					}
				}
			} catch (Exception ex) {
				this.Log("Error while uninstalling:" + ex.Message);
			}

			this.PatchBtn.IsEnabled = this.UnPatchBtn.IsEnabled = true;
		}

		private void CopyBtn_Click(object sender, RoutedEventArgs e) {
			Clipboard.SetText(this.ConsoleBox.GetTotalTextRange().Text);
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);

			this.ConsoleBox.GetTotalTextRange().Text = "";

			this.Log("Patcher gui initialized");
			this.Log("Searching for Chromium installations...");

			foreach (InstallationPaths paths in new InstallationManager().FindAllChromiumInstallations()) {
				this.AddChromiumInstallation(paths);
			}

			foreach (GuiPatchGroupData patchGroup in Program.BytePatchManager.PatchGroups) {
				this.PatchGroupList.Items.Add(new CustomCheckBox(patchGroup));
			}
		}

		public void Log(string str) {
			this.ConsoleBox.Dispatcher.Invoke(new Action(() => {
				Paragraph logParagraph = new Paragraph();
				logParagraph.Inlines.Add(str);

				this.ConsoleBox.Document.Blocks.Add(logParagraph);
				this.ConsoleBox.ScrollToEnd();
			}));
		}

		private void AddChromiumInstallation(InstallationPaths chromePaths, bool suppressErrors = true) {
			if (!chromePaths.Is64Bit()) {
				if (!suppressErrors) {
					MessageBox.Show("A 64-bit Chromium installation is required", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				return;
			}

			CustomCheckBox installationBox = new CustomCheckBox(chromePaths.ChromeDllPath);
			installationBox.IsChecked = true;
			installationBox.ToolTip = chromePaths.ChromeDllPath + " & " + chromePaths.ChromeExePath;

			this.InstallationList.Items.Add(installationBox);
			this.Log("Added Chromium installation at " + chromePaths.ChromeDllPath);
		}
	}

	public static class RichTextBoxExtensions {
		public static TextRange GetTotalTextRange(this RichTextBox richTextBox) {
			return new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
		}
	}
}
