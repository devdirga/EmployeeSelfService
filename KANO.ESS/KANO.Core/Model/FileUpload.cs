using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;

namespace KANO.Core.Model
{    
    public class FileUpload
    {
        private string _filepath;
        [JsonIgnore]
        public string Filepath
        {
            get
            {
                return _filepath;
            }

            set
            {
                _filepath = value;
                if (!string.IsNullOrWhiteSpace(_filepath))
                {
                    Accessible = File.Exists(_filepath);
                    Filename = Path.GetFileName(_filepath);
                    Fileext = Path.GetExtension(_filepath);

                    if (Accessible && string.IsNullOrWhiteSpace(Checksum))
                        Checksum = Tools.CalculateMD5(_filepath);
                }

            }
        }
        public string Filename { get; set; }
        public string Fileext { get; set; }
        public string Checksum { get; set; }
        [BsonIgnore]
        public bool Accessible { get; set; }
        public string Description { get; set; }
    }
}
