using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SteamSkinInstaller.Steam {
    internal class ClientProperties {
        private readonly string _installPath;
        private readonly string _exePath;
        private string _skin;
        private bool _beta;

        public ClientProperties() {
            _installPath =
                (string)
                    Microsoft.Win32.Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\" + ((Environment.Is64BitOperatingSystem) ? "Wow6432Node" : "") +
                        @"\Valve\Steam", "InstallPath", null) ??
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            _exePath = Path.Combine(_installPath, "Steam.exe");
            if (!File.Exists(_exePath)) {
                _installPath = null;
                _exePath = null;
                throw new Exception(
                    "Steam doesn't seem to be installed or is installed in a very, very non-standard path. " +
                    "You may, however, specify your own Steam installation location in the settings. " +
                    "You will not be able to install skins until this is fixed. This is done for your own safety.");
            }
            _skin =
                (string) Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SkinV4", null);
            if (File.Exists(Path.Combine(_installPath, "package", "beta"))) {
                _beta = true;
            }
        }

        public ClientProperties(string installPath) {
            if (!File.Exists(Path.Combine(installPath, "Steam.exe"))) {
                _installPath = null;
                throw new Exception("Invalid Steam install path.");
            }
            _installPath = installPath;
            _exePath = Path.Combine(installPath, "Steam.exe");
            if (File.Exists(Path.Combine(_installPath, "package", "beta"))) {
                _beta = true;
            }
        }

        public string GetInstallPath() {
            return _installPath;
        }

        public bool IsBetaClient() {
            return _beta;
        }

        public void UnsubscripeFromBeta() {
            if (File.Exists(Path.Combine(_installPath, "package", "beta"))) {
                File.Delete(Path.Combine(_installPath, "package", "beta"));
                _beta = false;
            }
        }

        public void SubscripeToBeta() {
            File.WriteAllText(Path.Combine(_installPath, "package", "beta"), "publicbeta");
            _beta = true;
        }

        public async void RestartClient() {
            if (Process.GetProcessesByName("Steam").Length > 0) {
                ProcessStartInfo quitSteam = new ProcessStartInfo(_exePath, "-shutdown");
                Process.Start(quitSteam);
                do {
                    await Task.Delay(5000);
                } while (Process.GetProcessesByName("Steam").Length > 0);
            }
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