using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SteamSkinInstaller {
    /// <summary>
    /// Interaction logic for NotAdminDialog.xaml
    /// </summary>
    public partial class NotAdminDialog : Window {

        public NotAdminDialog() {
            InitializeComponent();
            ShieldIcon.Source = MiscTools.GetUACShieldIcon();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = Equals(sender as Button, ButtonRestart);
        }
    }
}
