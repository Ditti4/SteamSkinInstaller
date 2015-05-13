using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamSkinInstaller {
    class BetterWebClient : WebClient {
        protected override WebRequest GetWebRequest(Uri url) {
            WebRequest request = base.GetWebRequest(url);
            if(request is HttpWebRequest) {
                (request as HttpWebRequest).KeepAlive = false;
                (request as HttpWebRequest).UserAgent =
                    "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2049.0 Safari/537.36";
            }
            return request;
        }
    }
}
