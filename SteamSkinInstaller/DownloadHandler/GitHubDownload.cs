using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SteamSkinInstaller.DownloadHandler {
    class GitHubDownload {
        private readonly string _user;
        private readonly string _repo;
        private readonly string _filename;
        private readonly bool _overwrite;
        private const string GithubAPIBaseURL = "https://api.github.com/repos/";
        private const string GithubBaseURL = "https://github.com/";

        public GitHubDownload(string user, string repo, string filename, bool overwrite = false) {
            if(string.IsNullOrEmpty(user) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(filename))
                throw new Exception("None of the parameters can be empty");
            _user = user;
            _repo = repo;
            _filename = filename;
            _overwrite = overwrite;
        }

        public void GetMasterZip() {
            BetterWebClient downloadClient = new BetterWebClient();
            if (!Directory.Exists("Downloads"))
                Directory.CreateDirectory("Downloads");
            if (!File.Exists(_filename) || _overwrite)
                downloadClient.DownloadFile(GithubBaseURL + _user + "/" + _repo + "/archive/master.zip", Path.Combine("Downloads", _filename));
        }

        public string GetLatestCommitHash() {
            const string regexPattern = "\"sha\": \"([0-9a-f]{40})\",";
            Regex shaRegex = new Regex(regexPattern);
            BetterWebClient apiClient = new BetterWebClient();
            string apiResponse =
                apiClient.DownloadString(GithubAPIBaseURL + _user + "/" + _repo + "/git/refs/heads/master");
            return shaRegex.Match(apiResponse).Groups[1].Value;
        }
    }
}
