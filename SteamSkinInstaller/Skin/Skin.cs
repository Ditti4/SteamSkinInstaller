using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller.Skin {
    internal class Skin {
        public static string DownloadFolderName = "SSIDownloads";
        private IDownload _downloadHandler;
        public readonly CatalogEntry Entry;
        private readonly string _filename;
        private Exception _lastException;

        public Skin(CatalogEntry entry) {
            Entry = entry;
            _filename = Path.Combine(DownloadFolderName, Entry.Name + ".zip");
        }

        private void CreateDownloadHandler() {
            if (_downloadHandler == null) {
                switch (Entry.FileDownload.Method.ToLower()) {
                    case "github":
                        _downloadHandler = new GitHubDownload(Entry.FileDownload.GithubUser,
                            Entry.FileDownload.GithubRepo, Entry.Name + ".zip",
                            Entry.RemoteVersionInfo.MatchPattern, Entry.RemoteVersionInfo.MatchGroup,
                            Entry.RemoteVersionInfo.MatchURL);
                        break;
                    case "deviantart":
                        _downloadHandler = new DeviantArtDownload(Entry.FileDownload.DeviantURL, Entry.Name + ".zip",
                            Entry.RemoteVersionInfo.MatchPattern,
                            Entry.RemoteVersionInfo.MatchGroup, Entry.RemoteVersionInfo.MatchURL,
                            Entry.FileDownload.FolderName ?? Entry.Name);
                        break;
                    case "direct":
                        // TODO
                        break;
                    default:
                        throw new Exception("Unknown download method " + Entry.FileDownload.Method + " for skin " +
                                            Entry.Name + ".");
                }
            }
        }

        public int Install(string installPath) {
            switch (Download()) {
                case 1:
                    MessageBox.Show(
                        "Okay, here's the thing: something went horribly wrong when trying to download the skin archive. " +
                        "This is a bug in this very tool right here. In case you do have a GitHub account, please submit a bug report at https://github.com/Ditti4/SteamSkinInstaller " +
                        "using the following details:\n\n" + _lastException.Message,
                        "Error trying to create the download handler");
                    break;
                case 2:
                    MessageBox.Show(
                        "Somehow I wasn't able to download the file archive although everything looks fine. " +
                        "Don't blame me, though, blame yourself for being low on disk space, your boss or somebody else.",
                        "Error downloading file");
                    break;
            }
            if (Unpack() != 0) {
                MessageBox.Show(
                    "Hrm, looks like I wasn't able to extract the skin archive. This probably ... okay, hopefully isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to extract the archive");
            }
            if (CleanupOnInstall() != 0) {
                MessageBox.Show(
                    "Looks like something went wrong when trying to delete a few files and folders which are supposed to be deleted. " +
                    "You shouldn't have to worry about that but if you're interested in the detailed error message " +
                    "then here you go:\n\n" +
                    _lastException.Message, "Error while trying to clean up");
            }
            if (MoveToSkinFolder(installPath) != 0) {
                MessageBox.Show(
                    "An error occured while trying to move the extracted files. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to move the skin folder");
            } else {
                FullCleanup();
            }
            return 0;
        }

        public int Update(string installPath) {
            switch (Download()) {
                case 1:
                    MessageBox.Show(
                        "Okay, here's the thing: something went horribly wrong when trying to download the skin archive. " +
                        "This is a bug in this very tool right here. In case you do have a GitHub account, please submit a bug report at https://github.com/Ditti4/SteamSkinInstaller " +
                        "using the following details:\n\n" + _lastException.Message,
                        "Error trying to create the download handler");
                    break;
                case 2:
                    MessageBox.Show(
                        "Somehow I wasn't able to download the file archive although everything looks fine. " +
                        "Don't blame me, though, blame yourself for being low on disk space, your boss or somebody else.",
                        "Error downloading file");
                    break;
            }
            if (Unpack() != 0) {
                MessageBox.Show(
                    "Hrm, looks like I wasn't able to extract the skin archive. This probably ... okay, hopefully isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to extract the archive");
            }
            if (CleanupOnUpdate() != 0) {
                MessageBox.Show(
                    "Looks like something went wrong when trying to delete a few files and folders which are supposed to be deleted. " +
                    "You shouldn't have to worry about that but if you're interested in the detailed error message " +
                    "then here you go:\n\n" +
                    _lastException.Message, "Error while trying to clean up");
            }
            if (MoveToSkinFolder(installPath) != 0) {
                MessageBox.Show(
                    "An error occured while trying to move the extracted files. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to move the skin folder");
            } else {
                FullCleanup();
            }
            return 0;
        }

        public int Download() {
            try {
                CreateDownloadHandler();
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            _downloadHandler.GetFile();
            return !File.Exists(_filename) ? 2 : 0;
        }

        public int Unpack() {
            try {
                using (ZipArchive archive = ZipFile.OpenRead(_filename)) {
                    foreach (ZipArchiveEntry entry in archive.Entries) {
                        string fullname = Entry.FileDownload.CreateFolder
                            ? Path.Combine(DownloadFolderName, Entry.Name, entry.FullName)
                            : Path.Combine(DownloadFolderName, entry.FullName);
                        if (File.Exists(fullname)) {
                            File.Delete(fullname);
                        } else if (Directory.Exists(fullname)) {
                            Directory.Delete(fullname, true);
                        }
                    }
                    archive.ExtractToDirectory(Entry.FileDownload.CreateFolder
                        ? Path.Combine(DownloadFolderName, Entry.Name)
                        : DownloadFolderName);
                }
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            return 0;
        }

        public int CleanupOnInstall() {
            try {
                foreach (string fileName in Entry.ExtraStuff.FilesToDeleteOnInstall) {
                    if (File.Exists(fileName)) {
                        File.Delete(fileName);
                    }
                }
                foreach (string folderName in Entry.ExtraStuff.FoldersToDeleteOnInstall) {
                    if (Directory.Exists(folderName)) {
                        Directory.Delete(folderName, true);
                    }
                }
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            return 0;
        }

        public int CleanupOnUpdate() {
            try {
                foreach (string fileName in Entry.ExtraStuff.FilesToDeleteOnUpdate) {
                    if (File.Exists(fileName)) {
                        File.Delete(fileName);
                    }
                }
                foreach (string folderName in Entry.ExtraStuff.FoldersToDeleteOnUpdate) {
                    if (Directory.Exists(folderName)) {
                        Directory.Delete(folderName, true);
                    }
                }
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            return 0;
        }

        public int MoveToSkinFolder(string installPath) {
            try {
                Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(
                    Path.Combine(DownloadFolderName, _downloadHandler.GetFolderName()),
                    Path.Combine(installPath, "skins", Entry.Name), true);
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            return 0;
        }

        public int InstallFonts(List<CatalogEntry.ExtraInfo.Font> fontList) {
            foreach (CatalogEntry.ExtraInfo.Font font in fontList) {
                // TODO: check if user wants to use the experimental install method (copy, add to registry, SendMessage())
                Process.Start(font.FileName);
            }
            return 0;
        }

        public int FullCleanup() {
            try {
                using (ZipArchive archive = ZipFile.OpenRead(_filename)) {
                    foreach (ZipArchiveEntry entry in archive.Entries) {
                        string fullname = Entry.FileDownload.CreateFolder
                            ? Path.Combine(DownloadFolderName, Entry.Name, entry.FullName)
                            : Path.Combine(DownloadFolderName, entry.FullName);
                        if (File.Exists(fullname)) {
                            File.Delete(fullname);
                        } else if (Directory.Exists(fullname)) {
                            Directory.Delete(fullname, true);
                        }
                    }
                }
                File.Delete(_filename);
            } catch (Exception e) {
                _lastException = e;
                return 1;
            }
            return 0;
        }

        public string GetRemoteVersion() {
            try {
                CreateDownloadHandler();
            } catch (Exception) {
                return null;
            }
            return _downloadHandler.GetLatestVersionString();
        }

        public string GetLocalVersion(string installPath) {
            try {
                string verisonFileContent = File.ReadAllText(Path.Combine(installPath, Entry.LocalVersionInfo.MatchURL));
                Regex versionRegex = new Regex(Entry.LocalVersionInfo.MatchPattern);
                return versionRegex.Match(verisonFileContent).Groups[Entry.LocalVersionInfo.MatchGroup].Value;
            } catch (Exception) {
                return null;
            }
        }
    }
}