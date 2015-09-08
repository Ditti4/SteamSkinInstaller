using System.Reflection;
using System.Windows;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window {
        public AboutDialog() {
            InitializeComponent();

            TextVersion.Text = TextVersion.Text.Replace("%versionplaceholder%", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}
