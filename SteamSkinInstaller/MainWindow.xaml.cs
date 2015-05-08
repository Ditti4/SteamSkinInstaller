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

namespace SteamSkinInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly bool _isAdmin;
        private readonly SteamClientProperties _steamClient;
        [DllImport("shell32.dll")]
        public static extern bool IsUserAnAdmin();

        public MainWindow() {
            InitializeComponent();
            _isAdmin = IsUserAnAdmin();
            if (!_isAdmin && System.Environment.OSVersion.Version.Major >= 6) {
                NotAdminDialog notAdminDialog = new NotAdminDialog();
                notAdminDialog.ShowDialog();
                if(notAdminDialog.DialogResult.HasValue && notAdminDialog.DialogResult.Value) {
                    ProcessStartInfo restartProgram = new ProcessStartInfo {
                        FileName = System.Reflection.Assembly.GetEntryAssembly().CodeBase,
                        Verb = "runas"
                    };
                    try {
                        Process.Start(restartProgram);
                        Close();
                    } catch (Exception) {
                        // user just said "no" to the UAC request so we're falling back to non-elevated mode
                    }
                } else
                    Testlabel.Content = "continue";
            }
            Testlabel.Content = "I am" + (_isAdmin ? " " : " not ") + "an admin";

            _steamClient = new SteamClientProperties();

            TextSteamLocation.Text = _steamClient.GetInstallPath();
        }

        private void buttonSteamLocation_Click(object sender, RoutedEventArgs e) {
            if(_isAdmin) { }
        }
    }
}
