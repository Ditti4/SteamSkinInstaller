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
        private string[] _developers = {
            "Rico \"Ditti4\" Dittrich",
        };

        public AboutDialog() {
            InitializeComponent();

            Left = (SystemParameters.PrimaryScreenWidth / 2) - (Width / 2);
            Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);

            string version = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." +
                             Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString() + " \"" + _codename + "\"";

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
