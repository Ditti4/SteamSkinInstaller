using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller.DownloadHandler {
    class DeviantArtDownload : IDownload {
        private readonly string _url;
        private readonly bool _overwrite;
        private readonly string _filename;
        private string _deviantPageString;
        private CookieContainer _cookieContainer;
        private readonly int _versionMatchGroup;
        private readonly string _versionRegexPattern;
        private readonly string _versionMatchURL;
        private readonly string _foldername;
        private string _versionPageString;

        public DeviantArtDownload(string url, string filename, string versionRegexPattern, int versionMatchGroup, string versionMatchURL = null,
            string foldername = null, bool overwrite = false) {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(versionRegexPattern)) {
                throw new Exception("None of the parameters can be empty");
            }
            Regex urlRegex = new Regex(@"^(http|https)://[\d\w]*\.deviantart.com/.*");
            if (!urlRegex.IsMatch(url)) {
                throw new Exception("Invalid DeviantArt URL");
            }
            _url = url;
            _filename = filename;
            _overwrite = overwrite;
            _versionRegexPattern = versionRegexPattern;
            _versionMatchGroup = versionMatchGroup;
            _versionMatchURL = versionMatchURL;
            _foldername = foldername;
        }

        public void GetFile() {
            if (!Directory.Exists("Downloads")) {
                Directory.CreateDirectory("Downloads");
            }
            if (string.IsNullOrEmpty(_deviantPageString)) {
                if (!FetchDeviantArtPage()) {
                    throw new Exception("Couldn't fetch the DeviantArt page of this item");
                }
            }
            if (File.Exists(Path.Combine("Downloads", _filename)) && !_overwrite) {
                return;
            }
            Regex downloadLinkRegex = new Regex(@"""(http://www\.deviantart\.com/download/\d*/.*)""");
            string downloadUrl = downloadLinkRegex.Match(_deviantPageString).Groups[1].Value.Replace("&amp;", "&");
            BetterWebClient downloadClient = new BetterWebClient(_cookieContainer, _url);
            downloadClient.DownloadFile(downloadUrl, Path.Combine("Downloads", _filename));
        }

        public bool FetchDeviantArtPage() {
            BetterWebClient pageClient = new BetterWebClient();
            _cookieContainer = new CookieContainer();
            _deviantPageString = pageClient.DownloadString(_url);
            _cookieContainer.SetCookies(new Uri(_url), pageClient.GetCookies());
            return _deviantPageString != null;
        }

        public string GetLatestVersionString() {
            Regex versionRegex = new Regex(_versionRegexPattern);
            if (string.IsNullOrEmpty(_versionMatchURL) || _versionMatchURL == _url) {
                if (string.IsNullOrEmpty(_deviantPageString)) {
                    if (!FetchDeviantArtPage()) {
                        throw new Exception("Couldn't fetch the DeviantArt page of this item");
                    }
                }
                return versionRegex.Match(_deviantPageString).Groups[_versionMatchGroup].Value;
            }
            if (string.IsNullOrEmpty(_versionPageString)) {
                BetterWebClient versionPageClient = new BetterWebClient();
                _versionPageString = versionPageClient.DownloadString(_versionMatchURL);
            }
            return versionRegex.Match(_versionPageString).Groups[_versionMatchGroup].Value;
        }

        public string GetFolderName() {
            return _foldername;
        }
    }
}
