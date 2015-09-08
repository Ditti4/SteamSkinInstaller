using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window {
        private string[] _developers = {
            "Rico \"Ditti4\" Dittrich",
        };

        public AboutDialog() {
            InitializeComponent();

            Left = (SystemParameters.PrimaryScreenWidth / 2) - (Width / 2);
            Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);

            TextVersion.Text = TextVersion.Text.Replace("%versionplaceholder%", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            foreach (string dev in _developers) {
                BoxDevelopers.AppendText(dev + Environment.NewLine);
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(e.Uri.AbsoluteUri);
        }
    }
}
