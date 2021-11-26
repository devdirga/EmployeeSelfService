using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Canteen")]
    [BsonIgnoreExtraElements]
    public class Canteen : BaseDocumentVerification, IMongoPreSave<Canteen>
    {                
        public string Name { get; set; }
        public string PICName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string UserID { get; set; }        
        public string CreatedByID { get; set; }
        public string CreatedByName { get; set; }
        public List<MenuMerchant> Menu { get; set; } = new List<MenuMerchant>();
        public bool Availability { get; set; } = true;
        [BsonIgnore]
        public User User { get; set; }
        public bool Deleted { get; set; } = false;
        [BsonIgnore]
        public CanteenUser CanteenUser { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }

        public bool IsUploadNeeded(IEnumerable<IFormFile> FileUpload, Canteen oldData)
        {
            return (FileUpload != null && oldData == null) || (FileUpload != null && oldData != null && !File.Exists(oldData.Filepath))
                                || (FileUpload != null && oldData != null && File.Exists(oldData.Filepath) && Tools.CalculateMD5(FileUpload.FirstOrDefault()) != oldData.Filepath);
        }

        public void Upload(IConfiguration configuration, Canteen data, Canteen oldData, IEnumerable<IFormFile> FileUpload, Func<Canteen, string> newFileNameFunc)
        {
            var uploadDirectory = Lib.Helper.Configuration.UploadPath(configuration);
            var maxFilesize = Lib.Helper.Configuration.UploadMaxFileSize(configuration);
            var allowedExtension = Lib.Helper.Configuration.UploadAllowedExtensions(configuration);

            if (this.IsUploadNeeded(FileUpload, oldData))
            {
                // Currently we limit only one file upload
                var file = FileUpload.FirstOrDefault();

                // File validation
                if (file.Length > maxFilesize)
                {
                    throw new Exception($"File upload size could not be more than {Format.FormatFileSize(maxFilesize)}");
                }

                if (!allowedExtension.Contains(Path.GetExtension(file.FileName)))
                {
                    throw new Exception($"File upload extension should be {string.Join(", ", allowedExtension)}");
                }

                // New file path and name preparation
                var newFilename = Tools.SanitizeFileName(String.Format("{0}_{1}{2}", newFileNameFunc(data), DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension(file.FileName)));
                newFilename = newFilename.Replace(" ", "_");
                var newFilepath = Path.Combine(uploadDirectory, newFilename);

                // Upload file
                using (var fileStream = new FileStream(newFilepath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                // We do not delete old, just add OLD behind the old file name 
                if (oldData != null && !string.IsNullOrWhiteSpace(oldData.Filepath))
                {
                    Tools.DeleteFile(oldData.Filepath);
                    data.Id = oldData.Id;
                }

                data.Filepath = newFilepath;
            }
            else if (oldData != null && string.IsNullOrWhiteSpace(oldData.Filepath))
            {
                data.Filepath = oldData.Filepath;
            }
        }

        public void Upload(IConfiguration configuration, Canteen oldData, IEnumerable<IFormFile> FileUpload, Func<Canteen, string> newFileNameFunc)
        {
            Upload(configuration, this, oldData, FileUpload, newFileNameFunc);
        }        
    }

    public class CanteenForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }

    }

    public enum CanteenUser : int { 
        ExistingUser =  1,
        NewUser = 2,
    }

    public class MenuMerchant
    {
        public string Name { get; set; }
    }
}
