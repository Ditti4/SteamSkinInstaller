using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SteamSkinInstaller.Skin;
using SteamSkinInstaller.Steam;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        internal static ClientProperties SteamClient;
        private WindowsPrincipal _principal;
        private bool _online;
        private readonly Catalog _availableSkinsCatalog;
        private readonly Catalog _installedSkinsCatalog;
        private List<Skin.Skin> _availableSkins;
        private List<Skin.Skin> _installedSkins;
        private readonly TextBlock _noCatalogWarning;
        private readonly TextBlock _errorReadingCatalogWarning;

        public bool IsAdmin() {
            _principal = _principal ?? new WindowsPrincipal(WindowsIdentity.GetCurrent() ?? new WindowsIdentity(""));
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

            int returncode;

            SteamClient = new ClientProperties();

            _availableSkinsCatalog = new Catalog("skins.xml");

            _noCatalogWarning = new TextBlock {
                Text = "Skin catalog file not found. Try clicking the button in the top right corner.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            _errorReadingCatalogWarning = new TextBlock {
                Text = "Error while trying to read the skin catalog file. You should try redownloading it in the top right corner.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            _availableSkins = _availableSkinsCatalog.GetSkins(out returncode);

            switch (returncode) {
                case 0:
                    foreach (Skin.Skin skin in _availableSkins) {
                        StackAvailable.Children.Add(GetNewAvailableSkinFragment(skin));
                    }
                    break;
                case 1:
                    StackAvailable.Children.Add(_noCatalogWarning);
                    break;
                case 2:
                    StackAvailable.Children.Add(_errorReadingCatalogWarning);
                    break;
            }

            SetOnlineStatus();

            TextSteamLocation.Text = SteamClient.GetInstallPath();

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
                ClientProperties newClient = new ClientProperties(steamFolder.SelectedPath);
                SteamClient = newClient;
                TextSteamLocation.Text = SteamClient.GetInstallPath();
            } catch (Exception exc) {
                MessageBox.Show(exc.Message, "Error");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ButtonRefresh.Visibility = TabSettings.Equals(((TabControl) sender).SelectedItem) ? Visibility.Hidden : Visibility.Visible;
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
            int returncode;

            _availableSkins = _availableSkinsCatalog.GetSkins(out returncode);

            switch(returncode) {
                case 0:
                    foreach(Skin.Skin skin in _availableSkins) {
                        StackAvailable.Children.Add(GetNewAvailableSkinFragment(skin));
                    }
                    break;
                case 1:
                    StackAvailable.Children.Add(_noCatalogWarning);
                    break;
                case 2:
                    StackAvailable.Children.Add(_errorReadingCatalogWarning);
                    break;
            }
        }

        private void EnableControls() {
            // TODO
        }

        private void DisableControls() {
            // TODO
        }

        private StackPanel GetNewAvailableSkinFragment(Skin.Skin skin) {
            StackPanel outerSkinPanel = new StackPanel();
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            StackPanel buttonPanel = new StackPanel();
            Button installButton = new Button();
            DockPanel innerSkinPanel = new DockPanel();
            TextBlock skinDescTextBlock = new TextBlock();
            Button websiteButton = new Button();

            skinNameLabel.Content = skin.Entry.Name;
            skinNameLabel.Padding = new Thickness(0, 10, 0, 0);
            skinNameLabel.FontSize = 20;

            skinAuthorLabel.Content = "by " + skin.Entry.Author;
            skinAuthorLabel.Padding = new Thickness(0);
            outerSkinPanel.Orientation = Orientation.Vertical;

            skinDescTextBlock.Text = skin.Entry.Description;
            skinDescTextBlock.TextWrapping = TextWrapping.Wrap;
            skinDescTextBlock.Margin = new Thickness(10);

            installButton.Content = "Install";
            installButton.Style = (Style) FindResource("KewlButton");
            installButton.Margin = new Thickness(5);
            installButton.Click += async (sender, args) => {
                LabelStatus.Content = "Installing " + skin.Entry.Name + ". Please wait …";
                DisableControls();
                switch (await (Task.Run(() => skin.Install()))) {
                    case 0:
                        break;
                    // TODO: add more possible failure reasons including appropiate message boxes
                    default:
                        MessageBox.Show("Something went wrong when trying to install " + skin.Entry.Name + ".", "Error installing skin");
                        break;
                }
                LabelStatus.Content = "Ready.";
                EnableControls();
            };
            websiteButton.Content = "Visit website";
            websiteButton.Style = (Style) FindResource("KewlButton");
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => {
                Process.Start(skin.Entry.Website);
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
    }
}
