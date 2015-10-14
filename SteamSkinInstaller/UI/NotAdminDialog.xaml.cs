using System.Windows;
using System.Windows.Controls;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller.UI {
    /// <summary>
    /// Interaction logic for NotAdminDialog.xaml
    /// </summary>
    public partial class NotAdminDialog {
        public NotAdminDialog() {
            InitializeComponent();
            ShieldIcon.Source = MiscTools.GetUACShieldIcon();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = ButtonRestart.Equals((Button) sender);
        }
    }
}