﻿using System;
using System.Net;

namespace SteamSkinInstaller.Util {
    [System.ComponentModel.DesignerCategory("Code")]
    internal class BetterWebClient : WebClient {
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
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null) {
                webRequest.KeepAlive = false;
                webRequest.UserAgent =
                    "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2049.0 Safari/537.36";
                webRequest.CookieContainer = Cookies;
                webRequest.Referer = Referer;
            }
            return request;
        }

        public string GetCookies() {
            return ResponseHeaders.Get("Set-Cookie");
        }
    }
}