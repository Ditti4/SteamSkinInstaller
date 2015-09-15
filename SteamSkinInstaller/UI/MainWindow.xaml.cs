using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
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
        private bool _lockApplyControlsState;
        private readonly Catalog _availableSkinsCatalog;
        private Catalog _installedSkinsCatalog;
        private List<Skin.Skin> _availableSkins;
        private List<Skin.Skin> _installedSkins;
        private readonly TextBlock _noCatalogWarning;
        private readonly TextBlock _noInstalledCatalogWarning;
        private readonly TextBlock _errorReadingCatalogWarning;
        private readonly TextBlock _errorReadingInstalledCatalogWarning;
        private readonly TextBlock _allSkinsInstalled;

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
                Environment.Exit(1);
            };

            InitializeComponent();

            Left = (SystemParameters.PrimaryScreenWidth/2) - (Width/2);
            Top = (SystemParameters.PrimaryScreenHeight/2) - (Height/2);
            InfoIcon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            ButtonUnelevated.Visibility = Visibility.Hidden;

            _lockInstallControlsState = false;
            _lockUpdateControlsState = false;
            _lockApplyControlsState = false;

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
                    Path.Combine(_steamClient.GetInstallPath(), "skins", "skins.xml") + ") and hope for the best.",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            _allSkinsInstalled = new TextBlock {
                Text =
                    "You already have every available skin installed. Not bad. You may try refreshing the available " +
                    "skin list using the button in the top right button though.",
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
                SetInstallControlsEnabledState(false);
                _lockInstallControlsState = true;
            } else {
                RebuildInstalledTab();
            }

            SetOnlineStatus();

            CheckBoxRestartSteam.IsChecked = Properties.Settings.Default.RestartSteam;
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
                SetInstallControlsEnabledState(true);

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
            SetNetworkControlsEnabledState(false);
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
            SetNetworkControlsEnabledState(true);

            RebuildAvailableTab();
            RemoveInstalledSkinsFromAvailableTab();
        }

        private void ButtonAbout_Click(object sender, RoutedEventArgs e) {
            AboutDialog aboutDialog = new AboutDialog {
                Owner = this,
                ShowInTaskbar = false
            };
            aboutDialog.ShowDialog();
        }

        private void ButtonUnelevated_Click(object sender, RoutedEventArgs e) {
            NotAdminDialog notAdminDialog = new NotAdminDialog {
                Owner = this,
                ShowInTaskbar = false
            };
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

        private DockPanel GetNewAvailableSkinFragment(Skin.Skin skin) {
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
        }

        private DockPanel GetNewInstalledSkinFragment(Skin.Skin skin) {
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
        }

        private void RebuildAvailableTab() {
            for (int i = StackAvailable.Children.Count - 1; i >= 0; i--) {
                StackAvailable.Children.RemoveAt(i);
            }

            int returncode;

            _availableSkins = _availableSkinsCatalog.GetSkins(out returncode);

            switch (returncode) {
                case 0:
                    foreach (Skin.Skin skin in _availableSkins) {
                        StackAvailable.Children.Add(GetNewAvailableSkinFragment(skin));
                    }
                    break;
                case 1:
                    StackAvailable.Children.Add(_noCatalogWarning);
                    _availableSkins = new List<Skin.Skin>();
                    break;
                case 2:
                    StackAvailable.Children.Add(_errorReadingCatalogWarning);
                    _availableSkins = new List<Skin.Skin>();
                    break;
            }
        }

        private void RebuildInstalledTab() {
            for (int i = StackInstalled.Children.Count - 1; i >= 0; i--) {
                StackInstalled.Children.RemoveAt(i);
            }

            int returncode;

            _installedSkinsCatalog = new Catalog(Path.Combine(_steamClient.GetInstallPath(), "skins", "skins.xml"));
            _installedSkins = _installedSkinsCatalog.GetSkins(out returncode);

            switch (returncode) {
                case 0:
                    foreach (Skin.Skin skin in _installedSkins) {
                        StackInstalled.Children.Add(GetNewInstalledSkinFragment(skin));
                    }
                    break;
                case 1:
                    StackInstalled.Children.Add(_noInstalledCatalogWarning);
                    _installedSkins = new List<Skin.Skin>();
                    break;
                case 2:
                    StackInstalled.Children.Add(_errorReadingInstalledCatalogWarning);
                    _installedSkins = new List<Skin.Skin>();
                    break;
            }

            RemoveInstalledSkinsFromAvailableTab();
        }

        private void RemoveInstalledSkinsFromAvailableTab() {
            if (!(StackAvailable.Children[0] is DockPanel)) {
                return;
            }
            for (int i = StackAvailable.Children.Count - 1; i >= 0; i--) {
                Label skinNameLabel = (Label) ((StackPanel) ((DockPanel) StackAvailable.Children[i]).Children[1]).Children[0];
                if (_installedSkins.Any(skin => skin.Entry.Name == (string) skinNameLabel.Content)) {
                    StackAvailable.Children.RemoveAt(i);
                }
            }
            if (StackAvailable.Children.Count == 0) {
                StackAvailable.Children.Add(_allSkinsInstalled);
            }
        }

        private async void SetOnlineStatus() {
            SetNetworkControlsEnabledState(false);
            LabelStatus.Content = "Checking internet connection, please wait …";
            if (!await Task.Run(() => MiscTools.IsComputerOnline())) {
                LabelStatus.Content = "Computer is not online. All online functionality will be disabled.";
                await Task.Delay(5000);
                _online = false;
            } else {
                _online = true;
            }
            LabelStatus.Content = "Ready.";
            SetNetworkControlsEnabledState(true);
        }

        private void SetInstallControlsEnabledState(bool state) {
            if (_lockInstallControlsState) {
                return;
            }
            if (StackAvailable.Children.Count == 0 || !(StackAvailable.Children[0] is DockPanel)) {
                return;
            }
            foreach (DockPanel skin in StackAvailable.Children) {
                foreach (UIElement mightBeRightPanel in skin.Children) {
                    if (mightBeRightPanel is StackPanel) {
                        foreach (UIElement mightBeInstallButton in ((StackPanel) mightBeRightPanel).Children) {
                            if (mightBeInstallButton is Button && (string) ((Button) mightBeInstallButton).Content == "Install") {
                                mightBeInstallButton.IsEnabled = state;
                            }
                        }
                    }
                }
            }
        }


        private void SetInstalledTabControlsEnabledState(string buttonText, bool state) {
            if (StackInstalled.Children.Count == 0 || !(StackInstalled.Children[0] is DockPanel)) {
                return;
            }
            foreach (DockPanel skin in StackInstalled.Children) {
                foreach (UIElement mightBeRightPanel in skin.Children) {
                    if (mightBeRightPanel is StackPanel) {
                        foreach (UIElement mightBeDesiredButton in ((StackPanel) mightBeRightPanel).Children) {
                            if (mightBeDesiredButton is Button && (string) ((Button) mightBeDesiredButton).Content == buttonText) {
                                mightBeDesiredButton.IsEnabled = state;
                            }
                        }
                    }
                }
            }
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
            ButtonRefresh.IsEnabled = state;
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.Reset();
            CheckBoxRestartSteam.IsChecked = Properties.Settings.Default.RestartSteam;
        }

        private void CheckBoxRestartSteam_CheckedChanged(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.RestartSteam = CheckBoxRestartSteam.IsChecked.HasValue && CheckBoxRestartSteam.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
    }
}