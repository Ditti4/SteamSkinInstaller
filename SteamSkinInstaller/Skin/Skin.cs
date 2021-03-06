﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using SteamSkinInstaller.DownloadHandler;

namespace SteamSkinInstaller.Skin {
    internal class Skin {
        public static string DownloadFolderName = "SSIDownloads";
        public readonly CatalogEntry Entry;
        private IDownload _downloadHandler;
        private readonly string _filename;
        private Exception _lastException;

        public Skin(CatalogEntry entry) {
            Entry = entry;
            _filename = Path.Combine(DownloadFolderName, Entry.Name + ".zip");
        }

        private void CreateDownloadHandler() {
            if (_downloadHandler != null) {
                return;
            }
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
                    throw new Exception("Unknown download method " + Entry.FileDownload.Method + " for skin " + Entry.Name + ".");
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
                    return 1;
                case 2:
                    MessageBox.Show(
                        "Somehow I wasn't able to download the file archive although everything looks fine. " +
                        "Don't blame me, though, blame yourself for being low on disk space, your boss or somebody else.",
                        "Error downloading file");
                    return 1;
            }
            if (Unpack() != 0) {
                MessageBox.Show(
                    "Hrm, looks like I wasn't able to extract the skin archive. This probably ... okay, hopefully isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to extract the archive");
                return 1;
            }
            if (CleanupOnInstall() != 0) {
                MessageBox.Show(
                    "Looks like something went wrong when trying to delete a few files and folders which are supposed to be deleted. " +
                    "Just in case we're going to stop the install process because I don't know what exactly went wrong. Here's the " +
                    "exact error message which you may use to resolve the problem:\n\n" +
                    _lastException.Message, "Error while trying to clean up");
                return 1;
            }
            Remove(installPath);
            if (MoveToSkinFolder(installPath) != 0) {
                MessageBox.Show(
                    "An error occured while trying to move the extracted files. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to move the skin folder");
                return 1;
            }
            File.WriteAllText(Path.Combine(installPath, "skins", Entry.Name, ".version"), GetRemoteVersion());
            FullCleanup();
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
                    return 1;
                case 2:
                    MessageBox.Show(
                        "Somehow I wasn't able to download the file archive although everything looks fine. " +
                        "Don't blame me, though, blame yourself for being low on disk space, your boss or somebody else.",
                        "Error downloading file");
                    return 1;
            }
            if (Unpack() != 0) {
                MessageBox.Show(
                    "Hrm, looks like I wasn't able to extract the skin archive. This probably ... okay, hopefully isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to extract the archive");
                return 1;
            }
            if (CleanupOnInstall() != 0) {
                MessageBox.Show(
                    "Looks like something went wrong when trying to delete a few files and folders which are supposed to be deleted. " +
                    "Just in case we're going to stop the install process because I don't know what exactly went wrong. Here's the " +
                    "exact error message which you may use to resolve the problem:\n\n" +
                    _lastException.Message, "Error while trying to clean up");
                return 1;
            }
            if (CleanupOnUpdate() != 0) {
                MessageBox.Show(
                    "Looks like something went wrong when trying to delete a few files and folders which are supposed to be deleted. " +
                    "Just in case we're going to stop the install process because I don't know what exactly went wrong. Here's the " +
                    "exact error message which you may use to resolve the problem:\n\n" +
                    _lastException.Message, "Error while trying to clean up");
                return 1;
            }
            if (MoveToSkinFolder(installPath) != 0) {
                MessageBox.Show(
                    "An error occured while trying to move the extracted files. This probably isn't my fault so " +
                    "please make sure you have enough free disk space and try again later. Exact error message:\n\n" +
                    _lastException.Message,
                    "Error trying to move the skin folder");
                return 1;
            }
            File.WriteAllText(Path.Combine(installPath, "skins", Entry.Name, ".version"), GetRemoteVersion());
            FullCleanup();
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
                    archive.ExtractToDirectory(Entry.FileDownload.CreateFolder ? Path.Combine(DownloadFolderName, Entry.Name) : DownloadFolderName);
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

        public void Remove(string installPath) {
            string fullPath = Path.Combine(installPath, "skins", Entry.Name);
            if (Directory.Exists(fullPath)) {
                Directory.Delete(fullPath, true);
            }
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
                return File.ReadAllText(Path.Combine(installPath, "skins", Entry.Name, ".version"));
            } catch (Exception) {
                return null;
            }
        }
    }
}