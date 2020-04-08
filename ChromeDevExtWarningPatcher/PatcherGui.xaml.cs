using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            openFile.AddExtension = true;
            openFile.CheckFileExists = openFile.CheckPathExists = true;

            if(openFile.ShowDialog(this) == true) { // No, I am not a noob, I have to do it like this
                AddChromiumInstallation(openFile.FileName);
            }
        }

        private void PatchBtn_Click(object sender, RoutedEventArgs e) {

        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);

            new TextRange(ConsoleBox.Document.ContentStart, ConsoleBox.Document.ContentEnd).Text = "";
            Log("Patcher gui initialized");
            Log("Searching for Chromium installations...");
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
            Log("Added Chromium installation at " + chromeDll);
        }
    }
}
