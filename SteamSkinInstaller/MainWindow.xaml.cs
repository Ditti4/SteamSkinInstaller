using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml.Serialization;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private SteamClientProperties _steamClient;
        private WindowsPrincipal _principal;
        private bool _online;
        private List<Skin> _skinsList;

        public bool IsAdmin() {
            _principal = _principal ?? new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return _principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public MainWindow() {
            InitializeComponent();
            Left = (SystemParameters.PrimaryScreenWidth/2) - (Width/2);
            Top = (SystemParameters.PrimaryScreenHeight/2) - (Height/2);
            if (Environment.OSVersion.Version.Major >= 6 && ! IsAdmin()) {
                NotAdminDialog notAdminDialog = new NotAdminDialog();
                notAdminDialog.ShowDialog();
                if(notAdminDialog.DialogResult.HasValue && notAdminDialog.DialogResult.Value) {
                    ProcessStartInfo restartProgramInfo = new ProcessStartInfo {
                        FileName = Assembly.GetEntryAssembly().CodeBase,
                        Verb = "runas"
                    };
                    try {
                        Process.Start(restartProgramInfo);
                        Close();
                    } catch (Exception) {
                        // user denied the UAC request so we're falling back to non-elevated mode
                        // note: this'll disable stuff like installing skins when Steam is
                        // installed in C:\Program Files or something similar
                    }
                }
            }

            SetOnlineStatus();

            ReadSkinsFile();
            foreach (Skin skin in _skinsList) {
                StackAvailable.Children.Add(GetNewAvailableSkinFragment(skin));
            }

            _steamClient = new SteamClientProperties();
            TextSteamLocation.Text = _steamClient.GetInstallPath();

        }

        private async void SetOnlineStatus() {
            LabelStatus.Content = "Checking internet connection, please wait …";
            if (!await Task.Run(() => MiscTools.IsComputerOnline())) {
                LabelStatus.Content = "Computer is not online. All online functionality will be disabled.";
                await Task.Delay(5000);
                _online = false;
            } else {
                _online = true;
            }
            LabelStatus.Content = "Ready.";
        }

        private void ButtonSteamLocation_Click(object sender, RoutedEventArgs e) {
            //TODO: Create a custom version of the folder browser dialog so I don't need to mix Windows Forms and WPF
            System.Windows.Forms.FolderBrowserDialog steamFolder = new System.Windows.Forms.FolderBrowserDialog {
                SelectedPath = TextSteamLocation.Text,
                ShowNewFolderButton = false
            };
            if (steamFolder.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try {
                SteamClientProperties newClient = new SteamClientProperties(steamFolder.SelectedPath);
                _steamClient = newClient;
                TextSteamLocation.Text = _steamClient.GetInstallPath();
            } catch (Exception exc) {
                MessageBox.Show(exc.Message, "Error");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ButtonRefresh.Visibility = Equals((sender as TabControl).SelectedItem, TabSettings) ? Visibility.Hidden : Visibility.Visible;
        }

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e) {
            if (!_online) return;
            LabelStatus.Content = "Downloading newest skin catalog file …";
            DisableControls();
            BetterWebClient skinDownloadClient = new BetterWebClient();
            try {
                // TODO: await skinDownloadClient.DownloadFileTaskAsync("https://raw.githubusercontent.com/Ditti4/SteamSkinInstaller/master/SteamSkinInstaller/skins.xml", "skins.xml");
                LabelStatus.Content = "Ready.";
            } catch (Exception) {
                MessageBox.Show("Something went wrong when trying to get the skin catalog file. Is GitHub offline? Did you delete the internet?", "Error getting skin catalog");
            }
            EnableControls();
            for (int i = StackAvailable.Children.Count - 1; i >= 0; i--) {
                StackAvailable.Children.RemoveAt(i);
            }
            ReadSkinsFile();
            foreach (Skin skin in _skinsList) {
                StackAvailable.Children.Add(GetNewAvailableSkinFragment(skin));
            }
        }

        private void EnableControls() {
            // TODO
        }

        private void DisableControls() {
            // TODO
        }

        private StackPanel GetNewAvailableSkinFragment(Skin skin) {
            StackPanel outerSkinPanel = new StackPanel();
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            StackPanel buttonPanel = new StackPanel();
            Button installButton = new Button();
            DockPanel innerSkinPanel = new DockPanel();
            TextBlock skinDescTextBlock = new TextBlock();
            Button websiteButton = new Button();

            skinNameLabel.Content = skin.Name;
            skinNameLabel.Padding = new Thickness(0, 10, 0, 0);
            skinNameLabel.FontSize = 20;

            skinAuthorLabel.Content = "by " + skin.Author;
            skinAuthorLabel.Padding = new Thickness(0);
            outerSkinPanel.Orientation = Orientation.Vertical;

            skinDescTextBlock.Text = skin.Description;
            skinDescTextBlock.TextWrapping = TextWrapping.Wrap;
            skinDescTextBlock.Margin = new Thickness(10);

            installButton.Content = "Install";
            installButton.Style = FindResource("KewlButton") as Style;
            installButton.Margin = new Thickness(5);
            installButton.Click += async (sender, args) => {
                LabelStatus.Content = "Installing " + skin.Name + ". Please wait …";
                DisableControls();
                switch (await (Task.Run(() => skin.Install()))) {
                    case 0:
                        break;
                    // TODO: add more possible failure reasons including appropiate message boxes
                    default:
                        MessageBox.Show("Something went wrong when trying to install " + skin.Name + ".", "Error installing skin");
                        break;
                }
                LabelStatus.Content = "Ready.";
                EnableControls();
            };
            websiteButton.Content = "Visit website";
            websiteButton.Style = FindResource("KewlButton") as Style;
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => {
                Process.Start(skin.Website);
            };
            websiteButton.ToolTip = "Click here to see screenshots and more!";
            buttonPanel.Orientation = Orientation.Vertical;

            buttonPanel.Children.Add(installButton);
            buttonPanel.Children.Add(websiteButton);

            DockPanel.SetDock(skinDescTextBlock, Dock.Left);
            DockPanel.SetDock(buttonPanel, Dock.Right);

            innerSkinPanel.Children.Add(buttonPanel);
            innerSkinPanel.Children.Add(skinDescTextBlock);

            outerSkinPanel.Children.Add(skinNameLabel);
            outerSkinPanel.Children.Add(skinAuthorLabel);
            outerSkinPanel.Children.Add(innerSkinPanel);

            return outerSkinPanel;
        }

        private void ReadSkinsFile() {
            XDocument document = XDocument.Load("skins.xml");
            try {
                using (FileStream skinCatalogFile = new FileStream("skins.xml", FileMode.Open)) {
                    XmlSerializer serializer = new XmlSerializer(typeof (Skin[]));
                    _skinsList = (serializer.Deserialize(skinCatalogFile) as Skin[]).ToList();
                }
            } catch (Exception e) {
                MessageBox.Show("Invalid skins catalog. Please check it if you modified it or redownload it using the button in the top right corner." +
                                "Please send in a bug report using the following information (you can use Ctrl + C to copy the content):\n" +
                                "Exception message:\n" + e.Message + "Current skin catalog:\n" + document +
                                "Thanks in advance!",
                    "Error reading the skins catalog file");
            }
        }
    }
}
