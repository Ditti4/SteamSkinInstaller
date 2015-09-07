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
            Left = (SystemParameters.PrimaryScreenWidth/2) - (Width/2);
            Top = (SystemParameters.PrimaryScreenHeight/2) - (Height/2);
            ShieldIcon.Source = MiscTools.GetUACShieldIcon();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = ButtonRestart.Equals((Button) sender);
        }
    }
}