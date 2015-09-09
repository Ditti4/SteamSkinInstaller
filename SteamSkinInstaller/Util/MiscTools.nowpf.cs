using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace SteamSkinInstaller.Util {
    internal static class MiscTools {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void SendFontChangeBroadcast() {
            SendMessage(new IntPtr(0xFFFF), 0x001D, new IntPtr(0), new IntPtr(0));
        }

        public static int CopyFontFile(string filename) {
            if(string.IsNullOrEmpty(filename)) {
                return 1;
            }
            if(!File.Exists(filename)) {
                return 1;
            }
            if(
                File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts",
                    Path.GetFileName(filename)))) {
                return 2;
            }
            try {
                File.Copy(filename,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts",
                        Path.GetFileName(filename)));
            } catch(Exception) {
                return 1;
            }
            return 0;
        }

        public static bool InstallFont(string fontname, string fontfilename) {
            int result = CopyFontFile(fontfilename);
            if(result == 1) {
                return false;
            }
            // TODO: get full font name (ID 4 in naming table) from TTF file and use that to add the font to the registry
            // ^ Yeah, no, won't do that. Way too much work for that small convenience factor - should probably just "launch" the font file
            // Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", fontname, Path.GetFileName(fontfilename));
            if(result != 2) {
                SendFontChangeBroadcast();
            }
            return true;
        }

        public static bool IsComputerConnected() {
            if(!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            return
                NetworkInterface.GetAllNetworkInterfaces()
                    .Any(
                        intf =>
                            intf.OperationalStatus == OperationalStatus.Up && !intf.Name.ToLower().Contains("virtual") &&
                            !intf.Description.ToLower().Contains("virtual") && !intf.Name.ToLower().Contains("loopback") &&
                            !intf.Description.ToLower().Contains("loopback") &&
                            intf.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            intf.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
        }

        public static bool IsComputerOnline() {
            if(!IsComputerConnected()) {
                return false;
            }
            BetterWebClient ncsiClient = new BetterWebClient();
            return ncsiClient.DownloadString(new Uri("http://www.msftncsi.com/ncsi.txt")).Equals("Microsoft NCSI");
        }
    }
}