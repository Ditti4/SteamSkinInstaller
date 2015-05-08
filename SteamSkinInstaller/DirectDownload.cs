using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SteamSkinInstaller.Exceptions;

namespace SteamSkinInstaller {
    class DirectDownload {
        private string _url;
        private string _filename;
        private HttpWebRequest _request;
        private HttpWebResponse _response;
        private Stream _respoStream;
        private FileStream _fileStream;

        public DirectDownload(string url, string filename, bool overwrite = false) {
            if (url.StartsWith("http") || url.StartsWith("ftp"))
                _url = url;
            else
                throw new DirectDownloadException("Not a valid URL");
            if (filename.Length > 0)
                _filename = filename;
            else
                throw new DirectDownloadException("Invalid file name");
            _request = System.Net.WebRequest.Create(_url) as HttpWebRequest;
            if(_request == null) throw new DirectDownloadException("Couldn't create the HTTP request");
            _response = _request.GetResponse() as HttpWebResponse;
            if(_response == null) throw new DirectDownloadException("Couldn't get a response from the HTTP request");
            if(_response.StatusCode != HttpStatusCode.OK) throw new DirectDownloadException("HTTP request returned a status code not equal to 200:" + _response.StatusDescription);
            _respoStream = _response.GetResponseStream();
            if (!Directory.Exists("DownloadData")) Directory.CreateDirectory("DownloadData");
            if(File.Exists(filename) && !overwrite) throw new Exception(filename + "already exists and the overwrite parameter wasn't set to true");
            _fileStream = File.Create("DownloadData\\" + filename);
            _respoStream.CopyTo(_fileStream);
            _fileStream.Close();
            _respoStream.Close();

        }
    }
}
