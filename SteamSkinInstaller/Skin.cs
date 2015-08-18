using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller {
    public class Skin {
        private IDownload _downloadHandler;

        public class DownloadInfo {
            public string Method;
            public string DeviantURL;
            public string DirectURL;
            public string GithubUser;
            public string GithubRepo;
            public bool GithubUseTags;
        }

        public class VersionInfo {
            public string MatchURL;
            public string MatchPattern;
            public int MatchGroup;
        }

        public class ExtraInfo {
            public class Font {
                public string Filename;
                public string Fontname;
            }

            public List<Font> Fonts;
        }

        public string Author;
        public string Name;
        public string Description;
        public string Website;

        public DownloadInfo FileDownload;
        public VersionInfo RemoteVersionInfo;
        public VersionInfo LocalVersionInfo;
        public ExtraInfo ExtraStuff;

        private void CreateDownloadHandler() {
            if (_downloadHandler == null) {
                switch (FileDownload.Method.ToLower()) {
                    case "github":
                        _downloadHandler = new GitHubDownload(FileDownload.GithubUser, FileDownload.GithubRepo, Name + ".zip");
                        break;
                    case "deviantart":
                        _downloadHandler = new DeviantArtDownload(FileDownload.DeviantURL, Name + ".zip", RemoteVersionInfo.MatchPattern,
                            RemoteVersionInfo.MatchGroup, RemoteVersionInfo.MatchURL);
                        break;
                    case "direct":
                        break;
                    default:
                        throw new Exception("Unknown download method for skin " + Name);
                }
            }
        }

        public int Install() {
            int result = Download();
            if (result != 0) {
                return result;
            }
            result = Unpack();
            if (result != 0) {
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
            } catch (Exception) {
                return 1;
            }
            _downloadHandler.GetFile();
            return !File.Exists(Path.Combine("Downloads", Name + ".zip")) ? 2 : 0;
        }

        public int Unpack() {
            return 0;
        }

        public int CopyToSkinFolder() {
            return 0;
        }

        public int InstallFonts(List<ExtraInfo.Font> fontList) {
            foreach (ExtraInfo.Font font in fontList) {
                
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
    }
}
