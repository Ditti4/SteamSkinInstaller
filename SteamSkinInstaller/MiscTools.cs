using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
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
            MiscTools.SHSTOCKICONINFO shieldIconInfo = new MiscTools.SHSTOCKICONINFO {Size = (uint) Marshal.SizeOf(typeof (MiscTools.SHSTOCKICONINFO))};
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
    }
}
