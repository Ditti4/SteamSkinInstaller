using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller.Skins {
    class Skin {
        private IDownload _downloadHandler;
        public readonly CatalogEntry Entry;

        public Skin(CatalogEntry entry) {
            Entry = entry;
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
                            Entry.RemoteVersionInfo.MatchGroup, Entry.RemoteVersionInfo.MatchURL);
                        break;
                    case "direct":
                        break;
                    default:
                        throw new Exception("Unknown download method for skin " + Entry.Name);
                }
            }
        }

        public int Install() {
            int result = Download();
            if(result != 0) {
                return result;
            }
            result = Unpack();
            if(result != 0) {
                return result;
            }
            result = CopyToSkinFolder();
            if(result != 0) {
                return result;
            }
            return 0;
        }

        public int Download() {
            try {
                CreateDownloadHandler();
            } catch(Exception) {
                return 1;
            }
            _downloadHandler.GetFile();
            return !File.Exists(Path.Combine("Downloads", Entry.Name + ".zip")) ? 2 : 0;
        }

        public int Unpack() {
            return 0;
        }

        public int CopyToSkinFolder() {
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
