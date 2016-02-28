using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static WindowsPrincipal _principal;
        private bool _online;
        private bool _lockInstallControlsState;
        private bool _lockUpdateControlsState;
        private bool _lockApplyControlsState;
        private readonly Catalog _availableSkinsCatalog;
        private Catalog _installedSkinsCatalog;
        private List<Skin.Skin> _availableSkins;
        private List<Skin.CatalogEntry> _installedSkinEntries;
        
        public static bool IsAdmin() {
            _principal = _principal ?? new WindowsPrincipal(WindowsIdentity.GetCurrent() ?? new WindowsIdentity(""));
            return _principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public MainWindow() {
            ClientProperties steamClient;
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
                    // So, uhm, yeah… we're screwed. Time to panic \o/
                    MessageBox.Show(
                        "Caught an exception while trying to handle another unhandled exception. " +
                        "I'm really sorry (this is the point where you may want to panic).", "What the…");
                }
                Environment.Exit(1);
            };

            InitializeComponent();

            /*InfoIcon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());*/

            /*ButtonUnelevated.Visibility = Visibility.Hidden;*/

            try {
                steamClient = ClientProperties.GetInstance();
                // TODO: settings dialog
                /*TextSteamLocation.Text = _steamClient.GetInstallPath();*/
            } catch (Exception e) {
                MessageBox.Show(e.Message, "Error while trying to find your Steam installation directory");
                // bring the _steamClient variable back into a known state so we can later use this to check for a valid Steam installation
                steamClient = null;
            }

            _availableSkinsCatalog = new Catalog("skins.xml");

            if (steamClient == null) {
                SetInstallControlsEnabledState(false);
                _lockInstallControlsState = true;
            } else {
                _installedSkinsCatalog = new Catalog(Path.Combine(steamClient.GetInstallPath(), "skins", "skins.xml"));
            }

            RebuildSkinList();
            SkinList.SelectedIndex = 0;

            SetOnlineStatus();

            /*CheckBoxRestartSteam.IsChecked = Properties.Settings.Default.RestartSteam;*/
        }

        // TODO: move to settings dialog
        /*private void ButtonSteamLocation_Click(object sender, RoutedEventArgs e) {
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
                SetInstallControlsEnabledState(true);

                RebuildInstalledTab();
            } catch (Exception exc) {
                MessageBox.Show(exc.Message, "Error");
            }
        }*/

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e) {
            if (!_online) {
                return;
            }
            // TODO: status bar
            //LabelStatus.Content = "Downloading newest skin catalog file …";
            SetNetworkControlsEnabledState(false);
            //BetterWebClient skinDownloadClient = new BetterWebClient();
            try {
                // TODO: await skinDownloadClient.DownloadFileTaskAsync("https://raw.githubusercontent.com/Ditti4/SteamSkinInstaller/master/SteamSkinInstaller/skins.xml", "skins.xml");
                await Task.Delay(5000);
                //LabelStatus.Content = "Ready.";
            } catch (Exception) {
                MessageBox.Show(
                    "Something went wrong when trying to get the skin catalog file. Is GitHub offline? Did you delete the internet?",
                    "Error getting skin catalog");
            }
            SetNetworkControlsEnabledState(true);

            RebuildSkinList();
        }

        private void ButtonAbout_Click(object sender, RoutedEventArgs e) {
            AboutDialog aboutDialog = new AboutDialog {
                Owner = this,
                ShowInTaskbar = false
            };
            aboutDialog.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                DragMove();
            }
        }

        private void SkinList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (SkinList.SelectedIndex < 0) {
                return;
            }
            SkinDetailsGrid.Children.Clear();
            SkinDetailsGrid.Children.Add((StackPanel) _availableSkins[SkinList.SelectedIndex]);
        }

        /*private DockPanel GetNewAvailableSkinFragment(Skin.Skin skin) {
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            TextBlock skinDescTextBlock = new TextBlock();
            Button installButton = new Button();
            Button websiteButton = new Button();
            StackPanel leftPanel = new StackPanel();
            StackPanel rightPanel = new StackPanel();
            DockPanel skinFragment = new DockPanel();

            skinNameLabel.Content = skin.Entry.Name;
            skinNameLabel.Padding = new Thickness(0, 10, 0, 0);
            skinNameLabel.FontSize = 20;

            skinAuthorLabel.Content = "by " + skin.Entry.Author;
            skinAuthorLabel.Padding = new Thickness(0);

            skinDescTextBlock.Text = skin.Entry.Description;
            skinDescTextBlock.TextWrapping = TextWrapping.Wrap;
            skinDescTextBlock.Margin = new Thickness(10);

            installButton.Content = "Install";
            installButton.Style = (Style) FindResource("KewlButton");
            installButton.Margin = new Thickness(5);
            installButton.Click += async (sender, args) => {
                LabelStatus.Content = "Installing " + skin.Entry.Name + ". Please wait …";
                SetNetworkControlsEnabledState(false);
                if (await (Task.Run(() => skin.Install(_steamClient.GetInstallPath()))) == 0) {
                    _installedSkins.Add(skin);
                    _installedSkinsCatalog.SaveSkins(_installedSkins);
                    RebuildInstalledTab();
                }
                LabelStatus.Content = "Ready.";
                SetNetworkControlsEnabledState(true);
            };
            websiteButton.Content = "Visit website";
            websiteButton.Style = (Style) FindResource("KewlButton");
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => { Process.Start(skin.Entry.Website); };
            websiteButton.ToolTip = "Click here to see screenshots and more!";

            leftPanel.Children.Add(skinNameLabel);
            leftPanel.Children.Add(skinAuthorLabel);
            leftPanel.Children.Add(skinDescTextBlock);

            rightPanel.Margin = new Thickness(0, 15, 0, 0);
            rightPanel.Children.Add(installButton);
            rightPanel.Children.Add(websiteButton);

            DockPanel.SetDock(leftPanel, Dock.Left);
            DockPanel.SetDock(rightPanel, Dock.Right);

            skinFragment.Margin = new Thickness(0, 5, 0, 5);
            skinFragment.Children.Add(rightPanel);
            skinFragment.Children.Add(leftPanel);

            return skinFragment;
        }*/

        /*private DockPanel GetNewInstalledSkinFragment(Skin.Skin skin) {
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            TextBlock skinDescTextBlock = new TextBlock();
            Button applyButton = new Button();
            Button updateButton = new Button();
            Button websiteButton = new Button();
            StackPanel leftPanel = new StackPanel();
            StackPanel rightPanel = new StackPanel();
            DockPanel skinFragment = new DockPanel();

            skinNameLabel.Content = skin.Entry.Name;
            skinNameLabel.Padding = new Thickness(0, 10, 0, 0);
            skinNameLabel.FontSize = 20;

            skinAuthorLabel.Content = "by " + skin.Entry.Author + "; installed version: " + (skin.GetLocalVersion(_steamClient.GetInstallPath()) ?? "unknown");
            skinAuthorLabel.Padding = new Thickness(0);

            skinDescTextBlock.Text = skin.Entry.Description;
            skinDescTextBlock.TextWrapping = TextWrapping.Wrap;
            skinDescTextBlock.Margin = new Thickness(10);

            applyButton.Content = "Apply";
            applyButton.Style = (Style) FindResource("KewlButton");
            applyButton.Margin = new Thickness(5);
            applyButton.Click += async (sender, args) => {
                SetApplyControlsEnabledState(false);
                LabelStatus.Content = "Setting current Steam skin to " + skin.Entry.Name + " …";
                _steamClient.SetSkin(skin.Entry.Name);
                SetApplyControlsEnabledState(true);
                if (Properties.Settings.Default.RestartSteam) {
                    LabelStatus.Content = "Restarting Steam …";
                    await Task.Run(() => _steamClient.RestartClient());
                }
                SetApplyControlsEnabledState(true);
                LabelStatus.Content = "Done. Enjoy your new skin.";
                if (Properties.Settings.Default.RestartSteam) {
                    LabelStatus.Content += " Hint: Steam might still be busy restarting, so be patient.";
                }
                await Task.Delay(3000);
                LabelStatus.Content = "Ready.";
            };

            updateButton.Content = "Update";
            updateButton.Style = (Style) FindResource("KewlButton");
            updateButton.Margin = new Thickness(5);
            updateButton.Click += async (sender, args) => {
                LabelStatus.Content = "Updating " + skin.Entry.Name + ". Please wait …";
                SetNetworkControlsEnabledState(false);
                bool forceCleanInstall = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                await (Task.Run(() => (forceCleanInstall) ? skin.Install(_steamClient.GetInstallPath()) : skin.Update(_steamClient.GetInstallPath())));
                RebuildInstalledTab();
                LabelStatus.Content = "Ready.";
                SetNetworkControlsEnabledState(true);
            };
            updateButton.ToolTip = "Shift + Click to perform a clean installation";

            websiteButton.Content = "Visit website";
            websiteButton.Style = (Style) FindResource("KewlButton");
            websiteButton.Margin = new Thickness(5);
            websiteButton.Click += (sender, args) => { Process.Start(skin.Entry.Website); };
            websiteButton.ToolTip = "Click here to see screenshots and more!";

            leftPanel.Children.Add(skinNameLabel);
            leftPanel.Children.Add(skinAuthorLabel);
            leftPanel.Children.Add(skinDescTextBlock);

            rightPanel.Margin = new Thickness(0, 15, 0, 0);
            rightPanel.Children.Add(applyButton);
            rightPanel.Children.Add(updateButton);
            rightPanel.Children.Add(websiteButton);

            DockPanel.SetDock(leftPanel, Dock.Left);
            DockPanel.SetDock(rightPanel, Dock.Right);

            skinFragment.Margin = new Thickness(0, 5, 0, 5);
            skinFragment.Children.Add(rightPanel);
            skinFragment.Children.Add(leftPanel);

            return skinFragment;
        }*/

        private void RebuildSkinList() {
            SkinList.Items.Clear();

            int returncode;

            _availableSkins = _availableSkinsCatalog.GetSkins(out returncode);

            switch (returncode) {
                case 0:
                    foreach (Skin.Skin skin in _availableSkins) {
                        SkinList.Items.Add((ListBoxItem) skin);
                    }
                    break;
                case 1:
                    // TODO: add warning to details grid
                    //StackAvailable.Children.Add(_noCatalogWarning);
                    _availableSkins = new List<Skin.Skin>();
                    break;
                case 2:
                    // TODO: add warning to details grid
                    //StackAvailable.Children.Add(_errorReadingCatalogWarning);
                    _availableSkins = new List<Skin.Skin>();
                    break;
            }

            _installedSkinEntries = _installedSkinsCatalog.GetEntries(out returncode);

            Skin.Skin _installedSkin;
            switch (returncode) {
                case 0:
                    foreach (CatalogEntry entry in _installedSkinEntries) {
                        _installedSkin = _availableSkins.First(skin => skin.Entry.Name == entry.Name);
                        if (_installedSkin == null) {
                            continue;
                        }
                        _installedSkin.Installed = true;
                    }
                    break;
                case 1:
                    // no installed skin
                    _installedSkinEntries = new List<CatalogEntry>();
                    break;
            }
        }

        private async void SetOnlineStatus() {
            SetNetworkControlsEnabledState(false);
            // TODO: status bar
            //LabelStatus.Content = "Checking internet connection, please wait …";
            if (!await Task.Run(() => MiscTools.IsComputerOnline())) {
                // TODO: status bar
                //LabelStatus.Content = "Computer is not online. All online functionality will be disabled.";
                await Task.Delay(5000);
                _online = false;
            } else {
                _online = true;
            }
            // TODO: status bar
            //LabelStatus.Content = "Ready.";
            SetNetworkControlsEnabledState(true);
        }

        private void SetInstallControlsEnabledState(bool state) {
            // TODO: fix
            /*if (_lockInstallControlsState) {
                return;
            }
            if (StackAvailable.Children.Count == 0 || !(StackAvailable.Children[0] is DockPanel)) {
                return;
            }
            foreach (DockPanel skin in StackAvailable.Children) {
                ((Button) ((StackPanel) skin.Children[0]).Children[0]).IsEnabled = state;
            }*/
        }


        private void SetInstalledTabControlsEnabledState(string buttonText, bool state) {
            // TODO: fix
            /*if (StackInstalled.Children.Count == 0 || !(StackInstalled.Children[0] is DockPanel)) {
                return;
            }
            foreach (DockPanel skin in StackInstalled.Children) {
                if (buttonText == "Apply") {
                    ((Button) ((StackPanel) skin.Children[0]).Children[0]).IsEnabled = state;
                } else {
                    ((Button) ((StackPanel) skin.Children[0]).Children[1]).IsEnabled = state;
                }
            }*/
        }

        private void SetApplyControlsEnabledState(bool state) {
            if (_lockApplyControlsState) {
                return;
            }
            SetInstalledTabControlsEnabledState("Apply", state);
        }

        private void SetUpdateControlsEnabledState(bool state) {
            if (_lockUpdateControlsState) {
                return;
            }
            SetInstalledTabControlsEnabledState("Update", state);
        }

        private void SetNetworkControlsEnabledState(bool state) {
            SetInstallControlsEnabledState(state);
            SetUpdateControlsEnabledState(state);
            RefreshButton.IsEnabled = state;
        }

        // TODO: move to settings dialog
        /*private void ButtonReset_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.Reset();
            CheckBoxRestartSteam.IsChecked = Properties.Settings.Default.RestartSteam;
        }*/

        // TODO: move to settings dialog
        /*private void CheckBoxRestartSteam_CheckedChanged(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.RestartSteam = CheckBoxRestartSteam.IsChecked.HasValue && CheckBoxRestartSteam.IsChecked.Value;
            Properties.Settings.Default.Save();
        }*/
    }
}