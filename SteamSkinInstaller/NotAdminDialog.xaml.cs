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
        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHSTOCKICONINFO {
            public uint Size;
            public IntPtr Handle;
            public int IconIndex;
            public int PathIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ResourcePath;
        }

        [DllImport("Shell32.dll")]
        public static extern int SHGetStockIconInfo(int siid, int uFlags, ref SHSTOCKICONINFO psii);

        public NotAdminDialog() {
            InitializeComponent();

            SHSTOCKICONINFO sii = new SHSTOCKICONINFO {Size = (uint) Marshal.SizeOf(typeof (SHSTOCKICONINFO))};
            Marshal.ThrowExceptionForHR(SHGetStockIconInfo(77, 0x000000100, ref sii));
            ShieldIcon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sii.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = Equals(sender as Button, ButtonRestart);
        }
    }
}
