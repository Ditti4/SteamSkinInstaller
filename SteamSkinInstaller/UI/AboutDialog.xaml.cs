using System.Diagnostics;
using System.Reflection;
using System.Windows.Navigation;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog {
        private string _codename = "Knorke";

        public AboutDialog() {
            InitializeComponent();

            string version = Assembly.GetExecutingAssembly().GetName().Version.Major + "." +
                             Assembly.GetExecutingAssembly().GetName().Version.Minor + " \"" + _codename + "\"";

            TextVersion.Text = TextVersion.Text.Replace("%versionplaceholder%", version);
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(e.Uri.AbsoluteUri);
        }
    }
}
