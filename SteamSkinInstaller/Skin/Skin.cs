using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;
using SteamSkinInstaller.DownloadHandler;
using SteamSkinInstaller.UI;

namespace SteamSkinInstaller.Skin {
    class Skin {
        private IDownload _downloadHandler;
        public readonly CatalogEntry Entry;
        private readonly string _filename;

        public Skin(CatalogEntry entry) {
            Entry = entry;
            _filename = Path.Combine("Downloads", Entry.Name + ".zip");
        }

        private void CreateDownloadHandler() {
            if(_downloadHandler == null) {
                switch(Entry.FileDownload.Method.ToLower()) {
                    case "github":
                        _downloadHandler = new GitHubDownload(Entry.FileDownload.GithubUser, Entry.FileDownload.GithubRepo, Entry.Name + ".zip",
                            Entry.RemoteVersionInfo.MatchPattern, Entry.RemoteVersionInfo.MatchGroup, Entry.RemoteVersionInfo.MatchURL);
                        break;
                    case "deviantart":
                        _downloadHandler = new DeviantArtDownload(Entry.FileDownload.DeviantURL, Entry.Name + ".zip", Entry.RemoteVersionInfo.MatchPattern,
                            Entry.RemoteVersionInfo.MatchGroup, Entry.RemoteVersionInfo.MatchURL, Entry.FileDownload.Foldername ?? Entry.Name);
                        break;
                    case "direct":
                        // TODO
                        break;
                    default:
                        throw new Exception("Unknown download method for skin " + Entry.Name);
                }
            }
        }

        public int Install() {
            int result = Download();
            switch (result) {
                case 1:
                    // handled in the Donwload method because I'm too lazy to try and catch here
                    break;
                case 2:
                    MessageBox.Show(
                        "Somehow I wasn't able to download the file archive although everything looks fine. " +
                        "Don't blame me, though, blame your missing free disk space, your boss or something else.",
                        "Error downloading file");
                    break;
            }
            result = Unpack();
            if(result != 0) {
                return result;
            }
            if (MainWindow.IsAdmin() &&
                (MainWindow.SteamClient.GetInstallPath().StartsWith(Environment.SpecialFolder.ProgramFiles.ToString()) ||
                 MainWindow.SteamClient.GetInstallPath().StartsWith(Environment.SpecialFolder.ProgramFilesX86.ToString()))) {
                result = MoveToSkinFolder();
                if (result != 0) {
                    return result;
                }
            } else {
                MessageBox.Show(
                    "Can't automatically move the skin folder to the appropiate directory in your Steam installation " +
                    "directory because I wasn't launched using administrative privileges. You can still go ahead and " +
                    "manually move it, it's located in the \"Downloads\" directory",
                    "Missing privileges to continue");
            }
            return 0;
        }

        public int Download() {
            try {
                CreateDownloadHandler();
            } catch(Exception e) {
                    MessageBox.Show(
                        "An error occured while trying to create the download handler. This shouldn't have happened and I'm sorry it did. " +
                        "But if you want to help out, just Ctrl + C on this dialog window and create a bug report. The important stuff comes here:\n\n" +
                        e.Message, "Error trying to create the download handler");
            }
            _downloadHandler.GetFile();
            return !File.Exists(_filename) ? 2 : 0;
        }

        public int Unpack() {
            try {
                using (ZipArchive archive = ZipFile.OpenRead(_filename)) {
                    archive.ExtractToDirectory(Entry.FileDownload.CreateFolder ? Path.Combine("Downloads", Entry.Name) : "Downloads");
                }
            } catch (Exception e) {
                MessageBox.Show(
                    "An error occured while trying to extract the downloaded archive. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" + e.Message,
                    "Error trying to extract the archive");
            }
            return 0;
        }

        public int MoveToSkinFolder() {
            try {
                Directory.Move(Path.Combine("Downloads", _downloadHandler.GetFolderName()),
                    Path.Combine(MainWindow.SteamClient.GetInstallPath(), "skins", _downloadHandler.GetFolderName()));
            } catch (Exception e) {
                MessageBox.Show(
                    "An error occured while trying to move the extracted files. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" + e.Message,
                    "Error trying to move the skin folder");
            }
            return 0;
        }

        public int InstallFonts(List<CatalogEntry.ExtraInfo.Font> fontList) {
            foreach(CatalogEntry.ExtraInfo.Font font in fontList) {
                // TODO: check if user wants to use the experimental install method (copy, add to registry, SendMessage())
                Process.Start(font.Filename);
            }
            return 0;
        }

        public string GetRemoteVersion() {
            try {
                CreateDownloadHandler();
            } catch(Exception) {
                return null;
            }
            return _downloadHandler.GetLatestVersionString();
        }

        public string GetLocalVersion() {
            try {
                string verisonFileContent = File.ReadAllText(Path.Combine(MainWindow.SteamClient.GetInstallPath(), Entry.LocalVersionInfo.MatchURL));
                Regex versionRegex = new Regex(Entry.LocalVersionInfo.MatchPattern);
                return versionRegex.Match(verisonFileContent).Groups[Entry.LocalVersionInfo.MatchGroup].Value;
            } catch (Exception) {
                return null;
            }
        }
    }
}
