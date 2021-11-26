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
    [BsonIgnoreExtraElements]
    public class BaseT
    {
        private DateTime _lastUpdate;

        public DateTime LastUpdate
        {
            get { return _lastUpdate; }
            set
            { _lastUpdate = value; }
        }

        public string UpdateBy { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class BaseUpdateRequest : BaseT, IMongoPreSave<BaseUpdateRequest>
    {        
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        public BaseUpdateRequest() {}

        public BaseUpdateRequest(IMongoDatabase mongoDB, IConfiguration configuration) {
            MongoDB = mongoDB;
            Configuration = configuration;
        }

        [BsonId]
        public string Id { get; set; }
        public UpdateRequestStatus Status { get; set; } = UpdateRequestStatus.InReview;
        [BsonIgnore]
        public string StatusDescription {
            get {
                return this.Status.ToString();
            }
        }   
        public string AXRequestID { get; set; }
        public long AXID { get; set; } = -1;
        public string EmployeeID { get; set; }        
        public string EmployeeName { get; set; }                        
        public string Reason { get; set; }
        public object OldData { get; set; }
        public object NewData { get; set; }
        public DateTime CreatedDate { get; set; }
        public ActionType Action { get; set; } = ActionType.Create;

        public void PreSave(IMongoDatabase db)
        {
            if (String.IsNullOrEmpty(this.EmployeeID))
                throw new Exception("Employee ID Cannot empty!");

            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }
    }

    [BsonIgnoreExtraElements]
    public class BaseDocumentVerification : BaseUpdateRequest, IMongoPreSave<BaseDocumentVerification>
    {

        public BaseDocumentVerification() : base() { 
        }

        public BaseDocumentVerification(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { 

        }

        private string _filepath;
        
        public string Filepath {
            get {
                return _filepath;
            }

            set {
                _filepath = value;
                if (!string.IsNullOrWhiteSpace(_filepath)) {                    
                    Accessible = File.Exists(_filepath);                    
                    Filename = Path.GetFileName(_filepath);
                    Fileext = Path.GetExtension(_filepath);

                    if (Accessible && string.IsNullOrWhiteSpace(Checksum))
                        Checksum = Tools.CalculateMD5(_filepath);                    
                }
                
            } 
        }
        public string Filename { get; set; }
        public string Fileext{ get; set; }        
        public string Checksum{ get; set; }
        [BsonIgnore]
        public bool Accessible { get; set; }

        public bool IsUploadNeeded(IEnumerable<IFormFile> FileUpload, BaseDocumentVerification oldData)
        {
            return (FileUpload != null && oldData == null)
                                || (FileUpload != null && oldData != null && !File.Exists(oldData.Filepath))
                                || (FileUpload != null && oldData != null && File.Exists(oldData.Filepath) && Tools.CalculateMD5(FileUpload.FirstOrDefault()) != oldData.Filepath);
        }

        public void Upload(IConfiguration configuration, BaseDocumentVerification data, BaseDocumentVerification oldData, IEnumerable<IFormFile> FileUpload, Func<BaseDocumentVerification, string> newFileNameFunc)
        {
            var uploadDirectory = Lib.Helper.Configuration.UploadPath(configuration);
            var maxFilesize = Lib.Helper.Configuration.UploadMaxFileSize(configuration);
            var allowedExtension = Lib.Helper.Configuration.UploadAllowedExtensions(configuration);

            if (this.IsUploadNeeded(FileUpload, oldData))
            {
                // Currently we limit only one file upload
                var file = FileUpload.FirstOrDefault();

                // File validation
                if (file.Length > maxFilesize) {
                    throw new Exception($"File upload size could not be more than {Format.FormatFileSize(maxFilesize)}");
                }

                if (!allowedExtension.Contains(Path.GetExtension(file.FileName))) {
                    throw new Exception($"File upload extension should be {string.Join(", ", allowedExtension)}");
                }

                // New file path and name preparation
                var newFilename = Tools.SanitizeFileName(String.Format("{0}_{1}{2}", newFileNameFunc(data), DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension(file.FileName)));
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

        public void Upload(BaseDocumentVerification data, BaseDocumentVerification oldData, IEnumerable<IFormFile> FileUpload, Func<BaseDocumentVerification, string> newFileNameFunc)
        {
            Upload(this.Configuration, data, oldData, FileUpload, newFileNameFunc);
        }

        public void Upload(IConfiguration configuration, BaseDocumentVerification oldData, IEnumerable<IFormFile> FileUpload, Func<BaseDocumentVerification, string> newFileNameFunc)
        {
            Upload(configuration, this, oldData, FileUpload, newFileNameFunc);
        }

        public void Upload(BaseDocumentVerification oldData, IEnumerable<IFormFile> FileUpload, Func<BaseDocumentVerification, string> newFileNameFunc)
        {
            Upload(this.Configuration, this, oldData, FileUpload, newFileNameFunc);
        }

        public byte[] Download() {
            if (this.Accessible)
            {
                return File.ReadAllBytes(this.Filepath);
            }
            else
            {
                throw new Exception("Unable to find file path on database");
            }
        }
    }

    [BsonIgnoreExtraElements]
    public class FieldAttachment
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        public FieldAttachment() { }

        public FieldAttachment(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            MongoDB = mongoDB;
            Configuration = configuration;
        }

        private string _filepath;
        public long AXID { get; set; }
        public object OldData { get; set; }
        public object NewData { get; set; }
        public string Notes { get; set; }
        //[JsonIgnore]
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
                    Filehash = Hasher.Encrypt(_filepath);

                    if (Accessible && string.IsNullOrWhiteSpace(Checksum))
                        Checksum = Tools.CalculateMD5(_filepath);
                }

            }
        }
        public string Filehash { get; set; }
        public string Filename { get; set; }
        public string Fileext { get; set; }
        public string Checksum { get; set; }
        [BsonIgnore]
        public bool Accessible { get; set; }

        public bool IsUploadNeeded(IEnumerable<IFormFile> FileUpload, FieldAttachment oldData)
        {
            return (FileUpload != null && oldData == null)
                                || (FileUpload != null && oldData != null && !File.Exists(oldData.Filepath))
                                || (FileUpload != null && oldData != null && File.Exists(oldData.Filepath) && Tools.CalculateMD5(FileUpload.FirstOrDefault()) != oldData.Filepath);
        }

        public void Upload(IConfiguration configuration, FieldAttachment data, FieldAttachment oldData, IEnumerable<IFormFile> FileUpload, Func<FieldAttachment, string> newFileNameFunc)
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
                }

                data.Filepath = newFilepath;
            }
            else if (oldData != null && string.IsNullOrWhiteSpace(oldData.Filepath))
            {
                data.Filepath = oldData.Filepath;
            }
        }

        public void Upload(FieldAttachment data, FieldAttachment oldData, IEnumerable<IFormFile> FileUpload, Func<FieldAttachment, string> newFileNameFunc)
        {
            Upload(this.Configuration, data, oldData, FileUpload, newFileNameFunc);
        }

        public void Upload(IConfiguration configuration, FieldAttachment oldData, IEnumerable<IFormFile> FileUpload, Func<FieldAttachment, string> newFileNameFunc)
        {
            Upload(configuration, this, oldData, FileUpload, newFileNameFunc);
        }

        public void Upload(FieldAttachment oldData, IEnumerable<IFormFile> FileUpload, Func<FieldAttachment, string> newFileNameFunc)
        {
            Upload(this.Configuration, this, oldData, FileUpload, newFileNameFunc);
        }

        public byte[] Download()
        {
            if (this.Accessible)
            {
                return File.ReadAllBytes(this.Filepath);
            }
            else
            {
                throw new Exception("Unable to find file path on database");
            }
        }
    }
}
