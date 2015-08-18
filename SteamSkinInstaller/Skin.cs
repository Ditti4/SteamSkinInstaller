using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller {
    class Skin {
        public string Author;
        public string Name;
        public string Description;
        public string Website;

        public class DownloadInfo {
            public string Method;
            public string DeviantURL;
            public string GithubUser;
            public string GithubRepo;
            public bool GithubUseTags;
        }

        public DownloadInfo Download;

        public class VersionInfo {
            public string MatchURL;
            public string MatchPattern;
            public int MatchGroup;
        }

        public VersionInfo Version;

        public class ExtraInfo {
            public class Font {
                public string Filename;
                public string Fontname;
            }

            public List<Font> Fonts;
        }
    }
}
