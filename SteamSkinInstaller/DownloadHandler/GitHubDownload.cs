using System;
using System.IO;
using System.Text.RegularExpressions;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller.DownloadHandler {
    class GitHubDownload : IDownload {
        private readonly string _user;
        private readonly string _repo;
        private readonly string _filename;
        private readonly int _versionMatchGroup;
        private readonly string _versionRegexPattern;
        private readonly string _versionMatchURL;
        private readonly bool _overwrite;
        private readonly bool _usetags;
        private string _latestTag;
        private const string GithubAPIRepoBaseURL = "https://api.github.com/repos/";
        private const string GithubBaseURL = "https://github.com/";
        private string _versionPageString;

        public GitHubDownload(string user, string repo, string filename, string versionRegexPattern, int versionMatchGroup, string versionMatchURL = null,
            bool overwrite = false, bool usetags = false) {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(filename)) {
                throw new Exception("None of the parameters can be empty.");
            }
            _user = user;
            _repo = repo;
            _filename = filename;
            _versionRegexPattern = versionRegexPattern;
            _versionMatchGroup = versionMatchGroup;
            _versionMatchURL = versionMatchURL;
            _overwrite = overwrite;
            _usetags = usetags;
        }

        public void GetMasterZip() {
            BetterWebClient downloadClient = new BetterWebClient();
            if (!Directory.Exists(Skin.Skin.DownloadFolderName)) {
                Directory.CreateDirectory(Skin.Skin.DownloadFolderName);
            }
            if (!File.Exists(Path.Combine(Skin.Skin.DownloadFolderName, _filename)) || _overwrite) {
                downloadClient.DownloadFile(GithubBaseURL + _user + "/" + _repo + "/archive/master.zip", Path.Combine(Skin.Skin.DownloadFolderName, _filename));
            }
        }

        public void GetLatestReleaseZip() {
            BetterWebClient downloadClient = new BetterWebClient();
            if (!Directory.Exists(Skin.Skin.DownloadFolderName)) {
                Directory.CreateDirectory(Skin.Skin.DownloadFolderName);
            }
            if (string.IsNullOrEmpty(_latestTag)) {
                GetLatestReleaseTag();
            }
            if (!File.Exists(Path.Combine(Skin.Skin.DownloadFolderName, _filename)) || _overwrite) {
                downloadClient.DownloadFile(GithubAPIRepoBaseURL + _user + "/" + _repo + "/zipball/" + _latestTag, Path.Combine(Skin.Skin.DownloadFolderName, _filename));
            }
        }

        public void GetFile() {
            if (_usetags) {
                GetLatestReleaseZip();
            } else {
                GetMasterZip();
            }
        }

        public string GetLatestCommitHash() {
            Regex shaRegex = new Regex(@"""sha"": ""([0-9a-f\.]*)"",");
            BetterWebClient apiClient = new BetterWebClient();
            string apiResponse = apiClient.DownloadString(GithubAPIRepoBaseURL + _user + "/" + _repo + "/releases");
            return shaRegex.Match(apiResponse).Groups[1].Value;
        }

        public string GetLatestReleaseTag() {
            Regex shaRegex = new Regex(@"""tag_name"": ""([0-9a-f]{40})"",");
            BetterWebClient apiClient = new BetterWebClient();
            string apiResponse = apiClient.DownloadString(GithubAPIRepoBaseURL + _user + "/" + _repo + "/releases");
            _latestTag = shaRegex.Match(apiResponse).Groups[1].Value;
            return _latestTag;
        }

        public string GetLatestVersionString() {
            if (string.IsNullOrEmpty(_versionMatchURL) || _versionMatchURL == (GithubBaseURL + _user + _repo)) {
                return _usetags ? GetLatestReleaseTag() : GetLatestCommitHash();
            }
            Regex versionRegex = new Regex(_versionRegexPattern);
            if(string.IsNullOrEmpty(_versionPageString)) {
                BetterWebClient versionPageClient = new BetterWebClient();
                _versionPageString = versionPageClient.DownloadString(_versionMatchURL);
            }
            return versionRegex.Match(_versionPageString).Groups[_versionMatchGroup].Value;
        }

        public string GetFolderName() {
            return _usetags ? _repo + "-" + GetLatestReleaseTag() : _repo + "-master";
        }
    }
}
