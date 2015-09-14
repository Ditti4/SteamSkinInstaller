using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SteamSkinInstaller.Skin;

namespace SteamSkinInstallerCatalogHelper {
    public class Program {
        private static void Main(string[] args) {
            List<CatalogEntry> skinsList;
            XmlSerializer serializer = new XmlSerializer(typeof (List<CatalogEntry>));
            try {
                using (FileStream file = new FileStream("skins.xml", FileMode.Open)) {
                    skinsList = serializer.Deserialize(file) as List<CatalogEntry>;
                }
            } catch (Exception) {
                skinsList = new List<CatalogEntry>();
            }

            CatalogEntry newSkin = new CatalogEntry();
            newSkin.FileDownload = new CatalogEntry.DownloadInfo();
            newSkin.RemoteVersionInfo = new CatalogEntry.VersionInfo();
            newSkin.LocalVersionInfo = new CatalogEntry.VersionInfo();
            newSkin.ExtraStuff = new CatalogEntry.ExtraInfo();
            newSkin.ExtraStuff.FontList = new List<CatalogEntry.ExtraInfo.Font>();
            newSkin.ExtraStuff.FilesToDeleteOnInstall = new List<string>();
            newSkin.ExtraStuff.FoldersToDeleteOnInstall = new List<string>();

            Console.Write("New skin's name: ");
            newSkin.Name = Console.ReadLine();
            Console.Write("Author: ");
            newSkin.Author = Console.ReadLine();
            Console.Write("Description: ");
            newSkin.Description = Console.ReadLine();
            Console.Write("Website's URL: ");
            newSkin.Website = Console.ReadLine();
            Console.Write("Download method (deviantart, github or direct): ");
            newSkin.FileDownload.Method = Console.ReadLine();
            switch (newSkin.FileDownload.Method) {
                case "deviantart":
                    Console.Write("DeviantArt URL: ");
                    newSkin.FileDownload.DeviantURL = Console.ReadLine();
                    break;
                case "github":
                    Console.Write("GitHub username: ");
                    newSkin.FileDownload.GithubUser = Console.ReadLine();
                    Console.Write("GitHub repository: ");
                    newSkin.FileDownload.GithubRepo = Console.ReadLine();
                    Console.Write("Does the repository use release tags? (yes or no) ");
                    newSkin.FileDownload.GithubUseTags = "yes".Equals(Console.ReadLine());
                    break;
                case "direct":
                    Console.Write("Download URL: ");
                    newSkin.FileDownload.DirectURL = Console.ReadLine();
                    break;
            }

            Console.Write("Create a new folder when unpacking the skin (choose yes if you did not place the skin in a folder before zipping it)? (yes or no) ");
            newSkin.FileDownload.CreateFolder = "yes".Equals(Console.ReadLine());

            bool remoteVersion = true;
            if (newSkin.FileDownload.Method == "github") {
                Console.Write("Use different remote version info than GitHub commits/release tags? (yes or no) ");
                remoteVersion = "yes".Equals(Console.ReadLine());
            }
            if (remoteVersion) {
                Console.Write("Remote version match URL: ");
                newSkin.RemoteVersionInfo.MatchURL = Console.ReadLine();
                Console.Write("Remote version match pattern: ");
                newSkin.RemoteVersionInfo.MatchPattern = Console.ReadLine();
                Console.Write("Remote version match group (integer value): ");
                newSkin.RemoteVersionInfo.MatchGroup = Convert.ToInt32(Console.ReadLine());
            }
            Console.Write("Local version match file (relative to the skin's folder, 'resource\\menus\\steam.menu' for example): ");
            newSkin.LocalVersionInfo.MatchURL = Console.ReadLine();
            Console.Write("Local version match pattern: ");
            newSkin.LocalVersionInfo.MatchPattern = Console.ReadLine();
            Console.Write("Local version match group (integer value): ");
            newSkin.LocalVersionInfo.MatchGroup = Convert.ToInt32(Console.ReadLine());

            Console.Write("Add a font? (yes or no) ");
            while ("yes".Equals(Console.ReadLine())) {
                newSkin.ExtraStuff.FontList.Add(new CatalogEntry.ExtraInfo.Font());
                Console.Write("Font file name (including directory, relative to the skin's folder): ");
                newSkin.ExtraStuff.FontList.Last().FileName = Console.ReadLine();
                Console.Write("Full font name (can be found in HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts): ");
                newSkin.ExtraStuff.FontList.Last().FontName = Console.ReadLine();
                Console.Write("Add another font? (yes or no) ");
            }

            Console.Write("Delete a file when installing a new skin? (yes or no) ");
            while ("yes".Equals(Console.ReadLine())) {
                Console.Write("File name (including directory, relative to the skin's folder): ");
                newSkin.ExtraStuff.FilesToDeleteOnInstall.Add(Console.ReadLine());
                Console.Write("Add another file to delete? (yes or no) ");
            }

            Console.Write("Recursively delete a folder when installing a new skin? (yes or no) ");
            while ("yes".Equals(Console.ReadLine())) {
                Console.Write("Folder name (relative to the skin's folder): ");
                newSkin.ExtraStuff.FoldersToDeleteOnInstall.Add(Console.ReadLine());
                Console.Write("Add another folder to delete? (yes or no) ");
            }

            Console.Write("Delete a file when updating an installed skin? (yes or no) ");
            while ("yes".Equals(Console.ReadLine())) {
                Console.Write("File name (including directory, relative to the skin's folder): ");
                newSkin.ExtraStuff.FilesToDeleteOnUpdate.Add(Console.ReadLine());
                Console.Write("Add another file to delete? (yes or no) ");
            }

            Console.Write("Recursively delete a folder when updating an installed skin? (yes or no) ");
            while ("yes".Equals(Console.ReadLine())) {
                Console.Write("Folder name (relative to the skin's folder): ");
                newSkin.ExtraStuff.FoldersToDeleteOnUpdate.Add(Console.ReadLine());
                Console.Write("Add another folder to delete? (yes or no) ");
            }

            skinsList.Add(newSkin);

            using (FileStream file = new FileStream("skins.xml", FileMode.Create)) {
                serializer.Serialize(file, skinsList);
            }
        }
    }
}