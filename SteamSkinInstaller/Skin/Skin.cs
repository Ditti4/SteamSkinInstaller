using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SteamSkinInstaller.DownloadHandler;
using SteamSkinInstaller.Steam;
using SteamSkinInstaller.UI;

namespace SteamSkinInstaller.Skin {
    internal class Skin {
        public static string DownloadFolderName = "SSIDownloads";

        public readonly CatalogEntry Entry;
        public StackPanel DetailsPanel;
        public StatusBar StatusBar;
        public ListBoxItem ListEntry;

        private IDownload _downloadHandler;
        private readonly string _filename;
        private Exception _lastException;

        private Label _isInstalledLabel;
        private Button _installButton;
        private Button _updateButton;
        private Button _applyButton;

        public bool Installed {
            get { return _isInstalledLabel.Visibility == Visibility.Visible; }
            set {
                _isInstalledLabel.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                _installButton.Visibility = !value ? Visibility.Visible : Visibility.Collapsed;
                _updateButton.Visibility = !value ? Visibility.Visible : Visibility.Collapsed;
                _applyButton.IsEnabled = value;
            }
        }
        public Skin(CatalogEntry entry) {
            Entry = entry;
            _filename = Path.Combine(DownloadFolderName, Entry.Name + ".zip");
            StatusBar = new StatusBar();
            GenerateDetailsPanel();
            GenerateListEntry();
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
                //case "direct":
                // TODO: implement this download handler (it should be the easiest one, why did I never implement this?)
                //break;
                default:
                    throw new Exception("Unknown download method " + Entry.FileDownload.Method + " for skin " +
                                        Entry.Name + ".");
            }
        }

        public int Install() {
            ClientProperties steamClient = ClientProperties.GetInstance();
            string installPath = (steamClient == null) ? null : steamClient.GetInstallPath();
            switch (Download()) {
                case 1:
                    MessageBox.Show(
                        "Okay, here's the thing: something went horribly wrong when trying to download the skin archive. " +
                        "This is a bug in this very tool right here. In case you do have a GitHub account, please submit a bug report at https://github.com/Ditti4/SteamSkinInstaller " +
                        "using the following details (you can hit Ctrl + C to copy the whole dialog):\n\n" + _lastException.Message,
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

        public int Update() {
            ClientProperties steamClient = ClientProperties.GetInstance();
            string installPath = (steamClient == null) ? null : steamClient.GetInstallPath();
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

        private void GenerateDetailsPanel() {
            DetailsPanel = new StackPanel();
            Image previewImage = new Image();
            Label skinNameLabel = new Label();
            Label skinAuthorLabel = new Label();
            TextBlock skinDescriptionBlock = new TextBlock();
            StackPanel buttonPanel = new StackPanel();
            _installButton = new Button();
            _updateButton = new Button();
            _applyButton = new Button();
            Button websiteButton = new Button();
            StackPanel changelogPanel = new StackPanel();
            StackPanel changelogEntry = new StackPanel();
            Label changelogLabel = new Label();
            TextBlock changelogBlock = new TextBlock();

            DetailsPanel.VerticalAlignment = VerticalAlignment.Stretch;
            DetailsPanel.Orientation = Orientation.Vertical;

            // TODO: display a real and correct preview image here
            previewImage.Source = new BitmapImage(new Uri("dummy.png", UriKind.Relative));
            previewImage.Margin = new Thickness(0);
            previewImage.VerticalAlignment = VerticalAlignment.Top;
            previewImage.MaxHeight = 500;

            skinNameLabel.FontSize = 18;
            skinNameLabel.FontWeight = FontWeights.Bold;
            skinNameLabel.Margin = new Thickness(5, 5, 0, 0);
            skinNameLabel.Padding = new Thickness(2);
            skinNameLabel.Content = Entry.Name;

            skinAuthorLabel.FontSize = 14;
            skinAuthorLabel.Foreground = (SolidColorBrush) (new BrushConverter().ConvertFrom("#808080"));
            skinAuthorLabel.Margin = new Thickness(10, 0, 0, 0);
            skinAuthorLabel.Padding = new Thickness(2);
            skinAuthorLabel.Content = Entry.Author;

            skinDescriptionBlock.Margin = new Thickness(7, 5, 7, 5);
            skinDescriptionBlock.VerticalAlignment = VerticalAlignment.Stretch;
            skinDescriptionBlock.TextWrapping = TextWrapping.Wrap;
            skinDescriptionBlock.Text = Entry.Description;

            buttonPanel.VerticalAlignment = VerticalAlignment.Stretch;
            buttonPanel.Orientation = Orientation.Horizontal;
            buttonPanel.Margin = new Thickness(10, 0, 0, 0);

            _installButton.Style = (Style) Application.Current.FindResource("KewlButton");
            _installButton.Content = "INSTALL";
            _installButton.Click += (sender, args) => Install();

            _updateButton.Style = (Style) Application.Current.FindResource("KewlButton");
            _updateButton.Content = "UPDATE";
            _updateButton.Visibility = Visibility.Collapsed;
            _updateButton.Click += (sender, args) => Update();

            _applyButton.Style = (Style) Application.Current.FindResource("KewlButton");
            _applyButton.Content = "APPLY";
            _applyButton.IsEnabled = false;
            _applyButton.Click += (sender, args) => {
                ClientProperties steamClient = ClientProperties.GetInstance();
                if (steamClient == null) {
                    return;
                }
                steamClient.SetSkin(Entry.Name);
            };

            websiteButton.Style = (Style) Application.Current.FindResource("KewlButton");
            websiteButton.Content = "WEBSITE";
            websiteButton.Click += (sender, args) => Process.Start(Entry.Website);

            // TODO: actually get a changelog from whatever source and display it here
            /*
            changelogPanel.VerticalAlignment = VerticalAlignment.Stretch;
            changelogPanel.Orientation = Orientation.Vertical;

            changelogEntry.VerticalAlignment = VerticalAlignment.Stretch;
            changelogEntry.Orientation = Orientation.Vertical;
            changelogEntry.Margin = new Thickness(10);

            changelogLabel.FontSize = 20;
            changelogLabel.Content = "1.1.0 (Jan 05, 2015)";

            changelogBlock.Padding = new Thickness(2);
            changelogBlock.Text = "* Just another relase, no changes\n* Need to fill some more space here …";
            */

            buttonPanel.Children.Add(_installButton);
            buttonPanel.Children.Add(_updateButton);
            buttonPanel.Children.Add(_applyButton);
            buttonPanel.Children.Add(websiteButton);

            changelogEntry.Children.Add(changelogLabel);
            changelogEntry.Children.Add(changelogBlock);

            changelogPanel.Children.Add(changelogEntry);

            DetailsPanel.Children.Add(previewImage);
            DetailsPanel.Children.Add(skinNameLabel);
            DetailsPanel.Children.Add(skinAuthorLabel);
            DetailsPanel.Children.Add(skinDescriptionBlock);
            DetailsPanel.Children.Add(buttonPanel);
            DetailsPanel.Children.Add(changelogPanel);
        }

        private void GenerateListEntry() {
            ListEntry = new ListBoxItem();
            DockPanel entryPanel = new DockPanel();
            Label skinNameLabel = new Label();
            Label skinLastUpdateLabel = new Label();
            _isInstalledLabel = new Label();

            ListEntry.Style = (Style) Application.Current.FindResource("KewlListBoxItem");

            entryPanel.VerticalAlignment = VerticalAlignment.Stretch;

            skinNameLabel.FontSize = 16;
            skinNameLabel.Margin = new Thickness(10, 10, 10, 0);
            skinNameLabel.Padding = new Thickness(0);
            skinNameLabel.Content = Entry.Name;

            DockPanel.SetDock(skinNameLabel, Dock.Top);

            skinLastUpdateLabel.Margin = new Thickness(10, 0, 10, 10);
            skinLastUpdateLabel.Padding = new Thickness(0);
            skinLastUpdateLabel.Content = "Last updated: Jan 05, 2015"; // TODO: dummy at the moment

            DockPanel.SetDock(skinLastUpdateLabel, Dock.Bottom);

            _isInstalledLabel.FontSize = 24;
            _isInstalledLabel.VerticalAlignment = VerticalAlignment.Center;
            _isInstalledLabel.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#808080");
            _isInstalledLabel.Visibility = Visibility.Hidden;
            _isInstalledLabel.Content = "✓";

            DockPanel.SetDock(_isInstalledLabel, Dock.Right);

            entryPanel.Children.Add(_isInstalledLabel);
            entryPanel.Children.Add(skinNameLabel);
            entryPanel.Children.Add(skinLastUpdateLabel);

            ListEntry.Content = entryPanel;

            // TODO: comment in the next line after adjusting the main layout
            //SkinList.Items.Add(root);
        }

        public DockPanel ToDockPanel() {
            return StatusBar;
        }

        public static implicit operator DockPanel(Skin skin) {
            return skin.ToDockPanel();
        }

        public StackPanel ToStackPanel() {
            return DetailsPanel;
        }

        public static implicit operator StackPanel(Skin skin) {
            return skin.ToStackPanel();
        }

        public ListBoxItem ToListBoxItem() {
            return ListEntry;
        }

        public static implicit operator ListBoxItem(Skin skin) {
            return skin.ToListBoxItem();
        }
    }
}