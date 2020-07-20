using ChromeDevExtWarningPatcher.InstallationFinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChromeDevExtWarningPatcher {
	public partial class PatcherGui : Window {

		public PatcherGui() {
			InitializeComponent();
		}

		private void SelectFolderBtn_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog openFile = new OpenFileDialog();
			openFile.Title = "Select a chrome.dll";
			openFile.Filter = "chrome.dll/msedge.dll file (chrome.dll;msedge.dll)|chrome.dll;msedge.dll|Alternative chrome.dll file (*.dll)|*.dll|All files (*.*)|*.*";
			openFile.FilterIndex = 1;
			openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;

			if (openFile.ShowDialog(this) == true) { // No, I'm not a noob, I have to do it like this and further below
				string chromeDllPath = openFile.FileName;

				openFile = new OpenFileDialog();
				openFile.Title = "Select a chrome.exe";
				openFile.Filter = "Browser executable file (*.exe)|*.exe|All files (*.*)|*.*";
				openFile.FilterIndex = 1;
				openFile.CheckFileExists = openFile.CheckPathExists = openFile.AddExtension = true;
				openFile.InitialDirectory = Directory.GetParent(Path.GetDirectoryName(chromeDllPath)).FullName;

				if (openFile.ShowDialog(this) == true) {
					string chromeExePath = openFile.FileName;
					AddChromiumInstallation(new InstallationPaths(chromeDllPath, chromeExePath), false);
				}
			}
		}

		private void PatchBtn_Click(object sender, RoutedEventArgs e) {
			PatchBtn.IsEnabled = UnPatchBtn.IsEnabled = false;

			Program.bytePatchManager.DisabledGroups.Clear();
			foreach (CustomCheckBox patchBox in PatchGroupList.Items) {
				if (patchBox.IsChecked == false)
					Program.bytePatchManager.DisabledGroups.Add(patchBox.Group);
			}

			try {
				List<InstallationPaths> installPaths = new List<InstallationPaths>();
				foreach (CustomCheckBox installBox in InstallationList.Items) {
					if (installBox.IsChecked == true) {
						installPaths.Add(new InstallationPaths(installBox.Content.ToString(), installBox.ToolTip.ToString().Split(new string[] { " & " }, StringSplitOptions.None)[1]));
					}
				}

				PatcherInstaller installer = new PatcherInstaller(installPaths);
				if (installer.Install(Log)) {
					foreach (CustomCheckBox installBox in InstallationList.Items) {
						if (installBox.IsChecked == true) {
							installBox.Foreground = installBox.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 207, 133));
						}
					}
				}
			} catch (Exception ex) {
				Log("Error while installing:" + ex.Message);
			}

			PatchBtn.IsEnabled = UnPatchBtn.IsEnabled = true;
		}

		private void UnPatchBtn_Click(object sender, RoutedEventArgs e) {
			PatchBtn.IsEnabled = UnPatchBtn.IsEnabled = false;

			try {
				List<InstallationPaths> installPaths = new List<InstallationPaths>();
				foreach (CustomCheckBox installBox in InstallationList.Items) {
					installPaths.Add(new InstallationPaths(installBox.Content.ToString(), installBox.ToolTip.ToString().Split(new string[] { " & " }, StringSplitOptions.None)[1])); // chromeExePath is always in the ToolTip after " & "
				}

				PatcherInstaller installer = new PatcherInstaller(installPaths);
				if (installer.UninstallAll(Log)) {
					foreach (CustomCheckBox installBox in InstallationList.Items) {
						if (installBox.IsChecked == true) {
							installBox.Foreground = installBox.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 207, 133));
						}
					}
				}
			} catch (Exception ex) {
				Log("Error while uninstalling:" + ex.Message);
			}

			PatchBtn.IsEnabled = UnPatchBtn.IsEnabled = true;
		}

		private void CopyBtn_Click(object sender, RoutedEventArgs e) {
			Clipboard.SetText(ConsoleBox.GetTotalTextRange().Text);
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);

			ConsoleBox.GetTotalTextRange().Text = "";

			Log("Patcher gui initialized");
			Log("Searching for Chromium installations...");

			foreach (InstallationPaths paths in new InstallationFinder.InstallationManager().FindAllChromiumInstallations()) {
				AddChromiumInstallation(paths);
			}

			foreach (GuiPatchGroupData patchGroup in Program.bytePatchManager.PatchGroups) {
				PatchGroupList.Items.Add(new CustomCheckBox(patchGroup));
			}
		}

		public void Log(string str) {
			ConsoleBox.Dispatcher.Invoke(new Action(() => {
				Paragraph logParagraph = new Paragraph();
				logParagraph.Inlines.Add(str);

				ConsoleBox.Document.Blocks.Add(logParagraph);
				ConsoleBox.ScrollToEnd();
			}));
		}

		private void AddChromiumInstallation(InstallationPaths chromePaths, bool suppressErrors = true) {
			if(!chromePaths.Is64Bit()) {
				if(!suppressErrors) {
					MessageBox.Show("A 64-bit Chromium installation is required", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				return;
			}

			CustomCheckBox installationBox = new CustomCheckBox(chromePaths.ChromeDllPath);
			installationBox.IsChecked = true;
			installationBox.ToolTip = chromePaths.ChromeDllPath + " & " + chromePaths.ChromeExePath;

			InstallationList.Items.Add(installationBox);
			Log("Added Chromium installation at " + chromePaths.ChromeDllPath);
		}
	}

	public static class RichTextBoxExtensions {
		public static TextRange GetTotalTextRange(this RichTextBox richTextBox) {
			return new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
		}
	}
}
