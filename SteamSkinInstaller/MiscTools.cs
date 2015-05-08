using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace SteamSkinInstaller {
    static class MiscTools {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
