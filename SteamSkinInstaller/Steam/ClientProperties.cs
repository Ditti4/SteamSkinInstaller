using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SteamSkinInstaller.Steam {
    internal class ClientProperties {
        private static ClientProperties _instance;

        private string _installPath;
        private string _exePath;
        private string _skin;
        private bool _beta;

        public string InstallPath {
            get {
                return _installPath;
            }
            set {
                // null value can be used to reset the installation path to the registry value
                if (string.IsNullOrEmpty(value)) {
                    _installPath =
                        (string)
                            Microsoft.Win32.Registry.GetValue(
                                @"HKEY_LOCAL_MACHINE\SOFTWARE\" +
                                (Environment.Is64BitOperatingSystem ? "Wow6432Node" : "") + @"\Valve\Steam",
                                "InstallPath",
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                    "Steam"));
                } else {
                    _installPath = value;
                }
                _exePath = Path.Combine(_installPath, "Steam.exe");
                if (!File.Exists(_exePath)) {
                    _installPath = null;
                    _exePath = null;
                    throw new Exception("Invalid Steam installation path.");
                }
                if (File.Exists(Path.Combine(_installPath, "package", "beta"))) {
                    _beta = true;
                }
            }
        }

        public string ExePath {
            get { return _exePath; }
        }

        private ClientProperties() {
            InstallPath = null;
            _skin =
                (string) Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SkinV4", null);
        }

        public static ClientProperties GetInstance(string installPath = null) {
            if (_instance == null) {
                _instance = new ClientProperties();
            }
            return _instance;
        }

        public string GetInstallPath() {
            return _installPath;
        }

        public bool IsBetaClient() {
            return _beta;
        }

        public void UnsubscribeFromBeta() {
            if (File.Exists(Path.Combine(_installPath, "package", "beta"))) {
                File.Delete(Path.Combine(_installPath, "package", "beta"));
                _beta = false;
            }
        }

        public void SubscribeToBeta() {
            File.WriteAllText(Path.Combine(_installPath, "package", "beta"), "publicbeta");
            _beta = true;
        }

        public async void RestartClient() {
            if (Process.GetProcessesByName("Steam").Length > 0) {
                ProcessStartInfo quitSteam = new ProcessStartInfo(_exePath, "-shutdown");
                Process.Start(quitSteam);
                do {
                    await Task.Delay(500);
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