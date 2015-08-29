using System.Collections.Generic;

namespace SteamSkinInstaller.Skin {
    public class CatalogEntry {
        public class DownloadInfo {
            public string Method;
            public string DeviantURL;
            public string DirectURL;
            public string GithubUser;
            public string GithubRepo;
            public bool GithubUseTags;
            public bool CreateFolder;
            public string FolderName;
        }

        public class VersionInfo {
            public string MatchURL;
            public string MatchPattern;
            public int MatchGroup;
        }

        public class ExtraInfo {
            public class Font {
                public string FileName;
                public string FontName;
            }

            public List<Font> FontList;
            public List<string> FoldersToDeleteOnInstall;
            public List<string> FilesToDeleteOnInstall;
            public List<string> FoldersToDeleteOnUpdate;
            public List<string> FilesToDeleteOnUpdate;
        }

        public string Author;
        public string Name;
        public string Description;
        public string Website;

        public DownloadInfo FileDownload;
        public VersionInfo RemoteVersionInfo;
        public VersionInfo LocalVersionInfo;
        public ExtraInfo ExtraStuff;
    }
}
