using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SteamSkinInstaller.DownloadHandler {
    class DeviantArtDownload {
        private readonly string _url;
        private readonly bool _overwrite;
        private readonly string _filename;
        private string _deviantPageString;
        private CookieContainer _cookieContainer;

        public DeviantArtDownload(string url, string filename, bool overwrite = false) {
            if(string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filename))
                throw new Exception("None of the parameters can be empty");
            Regex urlRegex = new Regex(@"^(http|https)://[\d\w]*\.deviantart.com/.*");
            if(!urlRegex.IsMatch(url))
                throw new Exception("Invalid DeviantArt URL");
            _url = url;
            _filename = filename;
            _overwrite = overwrite;
        }

        public void DownloadFile() {
            if(!Directory.Exists("Downloads"))
                Directory.CreateDirectory("Downloads");
            if(string.IsNullOrEmpty(_deviantPageString)) {
                if(!FetchDeviantArtPage())
                    throw new Exception("Couldn't fetch the DeviantArt page of this item");
            }
            if(File.Exists(Path.Combine("Downloads", _filename)) && !_overwrite)
                return;
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

        public string GetVersionString(string regexPattern, int matchGroup) {
            Regex versionRegex = new Regex(regexPattern);
            if(string.IsNullOrEmpty(_deviantPageString)) {
                if(!FetchDeviantArtPage())
                    throw new Exception("Couldn't fetch the DeviantArt page of this item");
            }
            return versionRegex.Match(_deviantPageString).Groups[matchGroup].Value;
        }
    }
}
