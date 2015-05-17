using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamSkinInstaller.DownloadHandler {
    class BetterWebClient : WebClient {
        protected CookieContainer Cookies;
        protected string Referer;

        public BetterWebClient() {
            Cookies = new CookieContainer();
        }

        public BetterWebClient(CookieContainer cookies) {
            Cookies = cookies;
        }
        public BetterWebClient(string referer) {
            Cookies = new CookieContainer();
            Referer = referer;
        }

        public BetterWebClient(CookieContainer cookies, string referer) {
            Cookies = cookies;
            Referer = referer;
        }

        protected override WebRequest GetWebRequest(Uri url) {
            WebRequest request = base.GetWebRequest(url);
            if(request is HttpWebRequest) {
                (request as HttpWebRequest).KeepAlive = false;
                (request as HttpWebRequest).UserAgent =
                    "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2049.0 Safari/537.36";
                (request as HttpWebRequest).CookieContainer = Cookies;
                (request as HttpWebRequest).Referer = Referer;
            }
            return request;
        }

        public string GetCookies() {
            return ResponseHeaders.Get("Set-Cookie");
        }
    }
}
