using System;
using System.IO;
using System.Text.RegularExpressions;
using SteamSkinInstaller.Util;

namespace SteamSkinInstaller.DownloadHandler {
    class GitHubDownload : IDownload {
        private readonly string _user;
        private readonly string _repo;
        private readonly string _filename;
        private readonly bool _overwrite;
        private readonly bool _usetags;
        private string _latestTag;
        private const string GithubAPIRepoBaseURL = "https://api.github.com/repos/";
        private const string GithubBaseURL = "https://github.com/";

        public GitHubDownload(string user, string repo, string filename, bool overwrite = false, bool usetags = false) {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(filename)) {
                throw new Exception("None of the parameters can be empty");
            }
            _user = user;
            _repo = repo;
            _filename = filename;
            _overwrite = overwrite;
            _usetags = usetags;
        }

        public void GetMasterZip() {
            BetterWebClient downloadClient = new BetterWebClient();
            if (!Directory.Exists("Downloads")) {
                Directory.CreateDirectory("Downloads");
            }
            if (!File.Exists(Path.Combine("Downloads", _filename)) || _overwrite) {
                downloadClient.DownloadFile(GithubBaseURL + _user + "/" + _repo + "/archive/master.zip", Path.Combine("Downloads", _filename));
            }
        }

        public void GetLatestReleaseZip() {
            BetterWebClient downloadClient = new BetterWebClient();
            if (!Directory.Exists("Downloads")) {
                Directory.CreateDirectory("Downloads");
            }
            if (string.IsNullOrEmpty(_latestTag)) {
                GetLatestReleaseTag();
            }
            if (!File.Exists(Path.Combine("Downloads", _filename)) || _overwrite) {
                downloadClient.DownloadFile(GithubAPIRepoBaseURL + _user + "/" + _repo + "/zipball/" + _latestTag, Path.Combine("Downloads", _filename));
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
            // TODO: implement support for custom remote version information
            return _usetags ? GetLatestReleaseTag() : GetLatestCommitHash();
        }
    }
}
