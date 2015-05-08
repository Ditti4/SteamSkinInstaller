using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamSkinInstaller.Exceptions {
    class DirectDownloadException : Exception {
        public DirectDownloadException(string message) : base(message) { }
    }
}
