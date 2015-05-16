using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Media.Imaging;


namespace SteamSkinInstaller {
    static class MiscTools {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

        public static BitmapSource GetUACShieldIcon() {
            SHSTOCKICONINFO shieldIconInfo = new SHSTOCKICONINFO {Size = (uint) Marshal.SizeOf(typeof (SHSTOCKICONINFO))};
            Marshal.ThrowExceptionForHR(SHGetStockIconInfo(77, 0x000000100, ref shieldIconInfo));
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shieldIconInfo.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public static void SendFontChangeBroadcast() {
            SendMessage(new IntPtr(0xFFFF), 0x001D, new IntPtr(0), new IntPtr(0));
        }

        public static bool CopyFontFile(string filename) {
            if (String.IsNullOrEmpty(filename))
                return false;
            if (!File.Exists(filename))
                return false;
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", Path.GetFileName(filename))))
                return false;
            try {
                File.Copy(filename, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", Path.GetFileName(filename)));
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public static bool RegisterFont(string fontname, string fontfilename) {
            return true;
        }

        public static bool IsComputerConnected() {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;
            return
                NetworkInterface.GetAllNetworkInterfaces()
                    .Any(
                        intf =>
                            intf.OperationalStatus == OperationalStatus.Up && !intf.Name.ToLower().Contains("virtual") &&
                            !intf.Description.ToLower().Contains("virtual") && !intf.Name.ToLower().Contains("loopback") &&
                            !intf.Description.ToLower().Contains("loopback") && intf.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            intf.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
        }

        public static bool IsComputerOnline() {
            if (!IsComputerConnected())
                return false;
            BetterWebClient ncsiClient = new BetterWebClient();
            return ncsiClient.DownloadString(new Uri("http://www.msftncsi.com/ncsi.txt")).Equals("Microsoft NCSI");
        }
    }
}
