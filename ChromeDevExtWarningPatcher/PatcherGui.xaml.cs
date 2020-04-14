using Microsoft.Win32;
using System;
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

            if(openFile.ShowDialog(this) == true) { // No, I am not a noob, I have to do it like this and further below
                AddChromiumInstallation(openFile.FileName);
            }
        }

        private void PatchBtn_Click(object sender, RoutedEventArgs e) {
            Program.bytePatchManager.DisabledGroups.Clear();
            if (RemoveExtWarning.IsChecked == false)
                Program.bytePatchManager.DisabledGroups.Add(0);
            if (RemoveDebugWarning.IsChecked == false)
                Program.bytePatchManager.DisabledGroups.Add(1);
            if (RemoveElision.IsChecked == false)
                Program.bytePatchManager.DisabledGroups.Add(2);

            foreach (CheckBox installationBox in InstallationList.Items) {
                if (installationBox.IsChecked == true) {
                    string path = installationBox.Content.ToString();

                    new Thread(() => {
                        try {
                            DllPatcher patcher = new DllPatcher(path);
                            patcher.Patch(Log);
                        } catch (Exception ex) {
                            Log("Error while patching " + path + ":" + ex.Message);
                        }
                    }).Start();
                }
            }
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);

            new TextRange(ConsoleBox.Document.ContentStart, ConsoleBox.Document.ContentEnd).Text = "";
            Log("Patcher gui initialized");
            Log("Searching for Chromium installations...");

            foreach(string path in new InstallationFinder.InstallationManager().FindAllChromiumInstallations()) {
                AddChromiumInstallation(path);
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

        private void AddChromiumInstallation(string chromeDll) {
            CheckBox installationBox = new CheckBox();
            installationBox.Content = chromeDll;
            installationBox.IsChecked = true;
            installationBox.Foreground = installationBox.BorderBrush = new SolidColorBrush(Color.FromRgb(202, 62, 71));

            InstallationList.Items.Add(installationBox);
            Log("Added Chromium installation at " + chromeDll);
        }
    }
}
