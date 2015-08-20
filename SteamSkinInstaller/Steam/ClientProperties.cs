using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SteamSkinInstaller.Steam {
    class ClientProperties {
        private readonly string _installPath;
        private readonly string _exePath;
        private string _skin;
        private bool _beta;

        public ClientProperties() {
            _installPath =
                Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\" + ((Environment.Is64BitOperatingSystem) ? "Wow6432Node" : "") + @"\Valve\Steam",
                    "InstallPath", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) as string;
            _exePath = Path.Combine(_installPath, "Steam.exe");
            if (!File.Exists(_exePath)) {
                _installPath = null;
                _exePath = null;
                throw new Exception("Steam is not installed");
            }
            _skin = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SkinV4", null) as string;
            if (File.Exists(_installPath + @"\package\beta")) _beta = true;
        }

        public ClientProperties(string installPath) {
            if(!File.Exists(installPath + @"\Steam.exe")) {
                _installPath = null;
                throw new Exception("Invalid Steam install path");
            }
            _installPath = installPath;
            _exePath = _installPath + @"\Steam.exe";
            if(File.Exists(_installPath + @"\package\beta")) _beta = true;
        }

        public string GetInstallPath() {
            return _installPath;
        }

        public bool IsBetaClient() {
            return _beta;
        }

        public void UnsubscripeFromBeta() {
            File.Delete(_installPath + @"\package\beta");
            _beta = false;
        }

        public void SubscripeToBeta() {
            File.WriteAllText(_installPath + @"\package\beta", "publicbeta");
            _beta = true;
        }

        public async void RestartClient() {
            ProcessStartInfo quitSteam = new ProcessStartInfo(_exePath, "-shutdown");
            Process.Start(quitSteam);
            await Task.Delay(5000);
            ProcessStartInfo startSteam = new ProcessStartInfo(_exePath);
            Process.Start(startSteam);
        }

        public void SetSkin(string skinName) {
            _skin = skinName;
            Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SkinV4", skinName);
        }

        public string GetSkin() {
            return _skin;
        }
    }
}
