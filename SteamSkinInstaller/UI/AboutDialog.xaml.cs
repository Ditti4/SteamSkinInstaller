using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog {
        private string _codename = "Knorke";
        private readonly string[] _developers = {
            "Rico \"Ditti4\" Dittrich",
        };

        public AboutDialog() {
            InitializeComponent();

            string version = Assembly.GetExecutingAssembly().GetName().Version.Major + "." +
                             Assembly.GetExecutingAssembly().GetName().Version.Minor + " \"" + _codename + "\"";

            TextVersion.Text = TextVersion.Text.Replace("%versionplaceholder%", version);

            foreach (string dev in _developers) {
                BoxDevelopers.AppendText(dev + Environment.NewLine);
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(e.Uri.AbsoluteUri);
        }
    }
}
