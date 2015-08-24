namespace SteamSkinInstaller.DownloadHandler {
    public interface IDownload {
        string GetLatestVersionString();
        void GetFile();
        string GetFolderName();
    }
}