using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private SteamClientProperties _steamClient;
        private WindowsPrincipal _principal;

        public bool IsAdmin() {
            _principal = _principal ?? new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return _principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public MainWindow() {
            InitializeComponent();
            Left = (System.Windows.SystemParameters.PrimaryScreenWidth/2) - (Width/2);
            Top = (System.Windows.SystemParameters.PrimaryScreenHeight/2) - (Height/2);
            if (System.Environment.OSVersion.Version.Major >= 6 && ! IsAdmin()) {
                NotAdminDialog notAdminDialog = new NotAdminDialog();
                notAdminDialog.ShowDialog();
                if(notAdminDialog.DialogResult.HasValue && notAdminDialog.DialogResult.Value) {
                    ProcessStartInfo restartProgramInfo = new ProcessStartInfo {
                        FileName = System.Reflection.Assembly.GetEntryAssembly().CodeBase,
                        Verb = "runas"
                    };
                    try {
                        Process.Start(restartProgramInfo);
                        Close();
                    } catch (Exception) {
                        // user just said "no" to the UAC request so we're falling back to non-elevated mode
                    }
                }
            }

            LabelStatus.Content = "Ready.";
            _steamClient = new SteamClientProperties();
            TextSteamLocation.Text = _steamClient.GetInstallPath();
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
            // TODO: get newest version of XML skin list file from the GitHub repo
        }

        private StackPanel GetNewAvailableSkinFragment(string name, string author, string description, string wobsite) {
            StackPanel outerSkinPanel = new StackPanel();
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            StackPanel buttonPanel = new StackPanel();
            Button installButton = new Button();
            DockPanel innerSkinPanel = new DockPanel();
            TextBlock skinDescTextBlock = new TextBlock();
            Button wobsiteButton = new Button();

            skinNameLabel.Content = name;
            skinNameLabel.Padding = new Thickness(0, 10, 0, 0);
            skinNameLabel.FontSize = 20;

            skinAuthorLabel.Content = "by " + author;
            skinAuthorLabel.Padding = new Thickness(0);
            outerSkinPanel.Orientation = Orientation.Vertical;

            skinDescTextBlock.Text = description;
            skinDescTextBlock.TextWrapping = TextWrapping.Wrap;
            skinDescTextBlock.Margin = new Thickness(10);

            installButton.Content = "Install";
            installButton.Style = FindResource("KewlButton") as Style;
            installButton.Margin = new Thickness(5);
            wobsiteButton.Content = "Visit website";
            wobsiteButton.Style = FindResource("KewlButton") as Style;
            wobsiteButton.Margin = new Thickness(5);
            wobsiteButton.Click += delegate {
                Process.Start(wobsite);
            };
            wobsiteButton.ToolTip = "Click here to see screenshots and more!";
            buttonPanel.Orientation = Orientation.Vertical;

            buttonPanel.Children.Add(installButton);
            buttonPanel.Children.Add(wobsiteButton);

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
