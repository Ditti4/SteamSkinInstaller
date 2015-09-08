using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SteamSkinInstaller.Skin;
using SteamSkinInstaller.Steam;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private ClientProperties _steamClient;
        private static WindowsPrincipal _principal;
        private bool _online;
        private bool _lockInstallControlsState;
        private bool _lockUpdateControlsState;
        private readonly Catalog _availableSkinsCatalog;
        private Catalog _installedSkinsCatalog;
        private List<Skin.Skin> _availableSkins;
        private List<Skin.Skin> _installedSkins;
        private readonly TextBlock _noCatalogWarning;
        private readonly TextBlock _noInstalledCatalogWarning;
        private readonly TextBlock _errorReadingCatalogWarning;
        private readonly TextBlock _errorReadingInstalledCatalogWarning;

        public static bool IsAdmin() {
            _principal = _principal ?? new WindowsPrincipal(WindowsIdentity.GetCurrent() ?? new WindowsIdentity(""));
            return _principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public MainWindow() {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                try {
                    MessageBox.Show("Caught an unhandled exception. This should never ever happen " +
                                    "so please report this using the GitHub issue tracker. " +
                                    "Just attach the ssi.log file in the directory where I'm located and " +
                                    "you should be good to go. Thanks in advance.", "Uh-oh");
                    using (StreamWriter logfile = new StreamWriter("ssi.log")) {
                        logfile.WriteLine("Unhandled exception at {0}: {1}", DateTime.Now,
                            ((Exception) e.ExceptionObject).ToString());
                    }
                } catch {
                    // So, uhm, yeah … we're screwed. Time to panic \o/
                    MessageBox.Show(
                        "Caught an exception while trying to handle another unhandled exception. " +
                        "I'm really sorry (this is the point where you may want to panic).", "What the …");
                }
            };

            InitializeComponent();

            Left = (SystemParameters.PrimaryScreenWidth/2) - (Width/2);
            Top = (SystemParameters.PrimaryScreenHeight/2) - (Height/2);
            if (Environment.OSVersion.Version.Major >= 6 && !IsAdmin()) {
                NotAdminDialog notAdminDialog = new NotAdminDialog();
                notAdminDialog.ShowDialog();
                if (notAdminDialog.DialogResult.HasValue && notAdminDialog.DialogResult.Value) {
                    ProcessStartInfo restartProgramInfo = new ProcessStartInfo {
                        FileName = Assembly.GetEntryAssembly().CodeBase,
                        Verb = "runas"
                    };
                    try {
                        Process.Start(restartProgramInfo);
                        Close();
                    } catch (Exception) {
                        // user denied the UAC request so we're falling back to non-elevated mode
                        // this will disable the experimental font installation and the "let me
                        // change the skin for you" thingy
                    }
                }
            }

            bool invalidSteamLocation = false;

            try {
                _steamClient = new ClientProperties();
                TextSteamLocation.Text = _steamClient.GetInstallPath();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "Error while trying to find your Stem installation");
                invalidSteamLocation = true;
            }


            _noCatalogWarning = new TextBlock {
                Text = "Skin catalog file not found. Try clicking the button in the top right corner.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            _noInstalledCatalogWarning = new TextBlock {
                Text = "Looks like you don't have any skin installed. Head over to the first tab and install one.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            _errorReadingCatalogWarning = new TextBlock {
                Text =
                    "Error while trying to read the skin catalog file. You should try redownloading it in the top right corner.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            _errorReadingInstalledCatalogWarning = new TextBlock {
                Text =
                    "Error while trying to read the skin catalog file. Try deleting it (located in " +
                    Path.Combine(_steamClient.GetInstallPath(), "skins", "skins.xml") + ") and hope the best",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            TextBlock invalidSteamLocationWarning = new TextBlock {
                Text =
                    "Couldn't find Steam and, thus, couldn't find any installed skins. You should try to fix this by going " +
                    "to the settings and selecting your current Steam installation folder.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            _availableSkinsCatalog = new Catalog("skins.xml");
            RebuildAvailableTab();

            if (invalidSteamLocation) {
                StackInstalled.Children.Add(invalidSteamLocationWarning);
                DisableInstallControls();
                _lockInstallControlsState = true;
            } else {
                RebuildInstalledTab();
            }

            SetOnlineStatus();
        }

        private void ButtonSteamLocation_Click(object sender, RoutedEventArgs e) {
            //TODO: Create a custom version of the folder browser dialog so I don't need to mix Windows Forms and WPF
            System.Windows.Forms.FolderBrowserDialog steamFolder = new System.Windows.Forms.FolderBrowserDialog {
                SelectedPath = TextSteamLocation.Text,
                ShowNewFolderButton = false
            };
            if (steamFolder.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
                return;
            }
            try {
                ClientProperties newClient = new ClientProperties(steamFolder.SelectedPath);
                _steamClient = newClient;
                TextSteamLocation.Text = _steamClient.GetInstallPath();
                _lockInstallControlsState = false;
                EnableInstallControls();

                RebuildInstalledTab();
            } catch (Exception exc) {
                MessageBox.Show(exc.Message, "Error");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ButtonRefresh.Visibility = TabSettings.Equals(((TabControl) sender).SelectedItem)
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e) {
            if (!_online) {
                return;
            }
            LabelStatus.Content = "Downloading newest skin catalog file …";
            DisableNetworkControls();
            //BetterWebClient skinDownloadClient = new BetterWebClient();
            try {
                // TODO: await skinDownloadClient.DownloadFileTaskAsync("https://raw.githubusercontent.com/Ditti4/SteamSkinInstaller/master/SteamSkinInstaller/skins.xml", "skins.xml");
                await Task.Delay(5000);
                LabelStatus.Content = "Ready.";
            } catch (Exception) {
                MessageBox.Show(
                    "Something went wrong when trying to get the skin catalog file. Is GitHub offline? Did you delete the internet?",
                    "Error getting skin catalog");
            }
            EnableNetworkControls();

            RebuildAvailableTab();
        }

        private void ButtonAbout_Click(object sender, RoutedEventArgs e) {
            (new AboutDialog()).ShowDialog();
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
                DisableNetworkControls();
                await (Task.Run(() => skin.Install(_steamClient.GetInstallPath())));
                LabelStatus.Content = "Ready.";
                EnableNetworkControls();
            };
            websiteButton.Content = "Visit website";
            websiteButton.Style = (Style) FindResource("KewlButton");
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => { Process.Start(skin.Entry.Website); };
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

        private StackPanel GetNewInstalledSkinFragment(Skin.Skin skin) {
            StackPanel outerSkinPanel = new StackPanel();
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            StackPanel buttonPanel = new StackPanel();
            Button updateButton = new Button();
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

            updateButton.Content = "Update";
            updateButton.Style = (Style) FindResource("KewlButton");
            updateButton.Margin = new Thickness(5);
            updateButton.Click += async (sender, args) => {
                LabelStatus.Content = "Updating " + skin.Entry.Name + ". Please wait …";
                DisableNetworkControls();
                await
                    (Task.Run(
                        () =>
                            (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                                ? skin.Install(_steamClient.GetInstallPath())
                                : skin.Update(_steamClient.GetInstallPath())));
                LabelStatus.Content = "Ready.";
                EnableNetworkControls();
            };
            updateButton.ToolTip = "Shift + Click to perform a clean installation";
            websiteButton.Content = "Visit website";
            websiteButton.Style = (Style) FindResource("KewlButton");
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => { Process.Start(skin.Entry.Website); };
            websiteButton.ToolTip = "Click here to see screenshots and more!";
            buttonPanel.Orientation = Orientation.Vertical;

            buttonPanel.Children.Add(updateButton);
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

        private void RebuildAvailableTab() {
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

        private void RebuildInstalledTab() {
            for(int i = StackInstalled.Children.Count - 1; i >= 0; i--) {
                StackInstalled.Children.RemoveAt(i);
            }

            int returncode;

            _installedSkinsCatalog = new Catalog(Path.Combine(_steamClient.GetInstallPath(), "skins", "skins.xml"));
            _installedSkins = _installedSkinsCatalog.GetSkins(out returncode);

            switch(returncode) {
                case 0:
                    foreach(Skin.Skin skin in _installedSkins) {
                        StackInstalled.Children.Add(GetNewInstalledSkinFragment(skin));
                    }
                    break;
                case 1:
                    StackInstalled.Children.Add(_noInstalledCatalogWarning);
                    break;
                case 2:
                    StackInstalled.Children.Add(_errorReadingInstalledCatalogWarning);
                    break;
            }
        }

        private async void SetOnlineStatus() {
            DisableNetworkControls();
            LabelStatus.Content = "Checking internet connection, please wait …";
            if(!await Task.Run(() => MiscTools.IsComputerOnline())) {
                LabelStatus.Content = "Computer is not online. All online functionality will be disabled.";
                await Task.Delay(5000);
                _online = false;
            } else {
                _online = true;
            }
            LabelStatus.Content = "Ready.";
            EnableNetworkControls();
        }

        private void SetInstallControlsState(bool state) {
            if(_lockInstallControlsState) {
                return;
            }
            if(!(StackAvailable.Children[0] is StackPanel)) {
                return;
            }
            foreach(StackPanel skin in StackAvailable.Children) {
                foreach(UIElement mightBeInnerPanel in skin.Children) {
                    if(mightBeInnerPanel is DockPanel) {
                        foreach(UIElement mightBeButtonPanel in ((DockPanel)mightBeInnerPanel).Children) {
                            if(mightBeButtonPanel is StackPanel) {
                                foreach(UIElement mightBeInstallButton in ((StackPanel)mightBeButtonPanel).Children) {
                                    if(mightBeInstallButton is Button &&
                                        (string)((Button)mightBeInstallButton).Content == "Install") {
                                        mightBeInstallButton.IsEnabled = state;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetUpdateControlsState(bool state) {
            if(_lockUpdateControlsState) {
                return;
            }
            if (!(StackInstalled.Children[0] is StackPanel)) {
                return;
            }
            foreach(StackPanel skin in StackInstalled.Children) {
                foreach(UIElement mightBeInnerPanel in skin.Children) {
                    if(mightBeInnerPanel is DockPanel) {
                        foreach(UIElement mightBeButtonPanel in ((DockPanel)mightBeInnerPanel).Children) {
                            if(mightBeButtonPanel is StackPanel) {
                                foreach(UIElement mightBeUpdateButton in ((StackPanel)mightBeButtonPanel).Children) {
                                    if(mightBeUpdateButton is Button &&
                                        (string)((Button)mightBeUpdateButton).Content == "Update") {
                                        mightBeUpdateButton.IsEnabled = state;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void EnableInstallControls() {
            SetInstallControlsState(true);
        }

        private void DisableInstallControls() {
            SetInstallControlsState(false);
        }

        private void EnableUpdateControls() {
            SetUpdateControlsState(true);
        }

        private void DisableUpdateControls() {
            SetUpdateControlsState(false);
        }

        private void SetNetworkControlsState(bool state) {
            SetInstallControlsState(state);
            SetUpdateControlsState(state);
            ButtonRefresh.IsEnabled = state;
        }

        private void EnableNetworkControls() {
            SetNetworkControlsState(true);
        }

        private void DisableNetworkControls() {
            SetNetworkControlsState(false);
        }
    }
}