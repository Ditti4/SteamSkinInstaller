﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SteamSkinInstaller.Skin {
    internal class Catalog {
        private readonly XmlSerializer _serializer;
        private readonly string _filename;

        public Catalog(string filename) {
            _serializer = new XmlSerializer(typeof (List<CatalogEntry>));
            _filename = filename;
        }

        /* errorcode: 0 - no error
         *            1 - file not found
         *            2 - error while trying to deserialize
         */

        public List<CatalogEntry> GetEntries(out int errorcode) {
            List<CatalogEntry> returnList = null;
            if (!File.Exists(_filename)) {
                errorcode = 1;
            } else {
                try {
                    using (FileStream inFileStream = File.Open(_filename, FileMode.Open)) {
                        returnList = (List<CatalogEntry>) _serializer.Deserialize(inFileStream);
                        errorcode = 0;
                    }
                } catch (Exception) {
                    errorcode = 2;
                }
            }
            return returnList;
        }

        public List<Skin> GetSkins(out int errorcode) {
            List<Skin> returnList = null;
            List<CatalogEntry> entryList = GetEntries(out errorcode);
            if (errorcode == 0) {
                returnList = entryList.Select(catalogEntry => new Skin(catalogEntry)).ToList();
            }
            return returnList;
        }

        /* return: 0 - no error
         *         1 - error while trying to serialize
         */

        public int SaveEntries(List<CatalogEntry> inList) {
            try {
                using (FileStream outFileStream = File.Open(_filename, FileMode.Create)) {
                    _serializer.Serialize(outFileStream, inList);
                    return 0;
                }
            } catch (Exception) {
                return 1;
            }
        }

        public int SaveSkins(List<Skin> inList) {
            return SaveEntries(inList.Select(skin => skin.Entry).ToList());
        }
    }
}