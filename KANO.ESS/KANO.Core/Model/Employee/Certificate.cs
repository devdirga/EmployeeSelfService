using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Certificate")]
    [BsonIgnoreExtraElements]
    public class Certificate : BaseDocumentVerification, IMongoPreSave<Certificate>
    {
        public Certificate Old { set; get; }
        public DateRange Validity { get; set; }
        public bool ReqRenew { get; set; }                
        public string Note { get; set; }
        public string TypeDescription { get; set; }
        public string TypeID { get; set; }
        public KESSWRServices.KESSWorkerRequestPurpose Purpose { get; set; }

        public Certificate() : base() { }

        public Certificate(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<CertificateResult> GetS(string employeeID)
        {
            var tasks = new List<Task<TaskRequest<List<Certificate>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var certificates = adapter.GetCertificates(employeeID);
                return TaskRequest<List<Certificate>>.Create("AX", certificates);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var certificates = this.MongoDB.GetCollection<Certificate>()
                                .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                .ToList();
                return TaskRequest<List<Certificate>>.Create("DB", certificates);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Combine result
            var result = new List<CertificateResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var Certificates = new List<Certificate>();
                var CertificatesUpdateRequest = new List<Certificate>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        Certificates = r.Result;
                    else
                        CertificatesUpdateRequest = r.Result;

                // Merge certificates data
                foreach (var f in Certificates)
                {
                    result.Add(new CertificateResult
                    {
                        Certificate = f,
                    });
                }

                foreach (var fur in CertificatesUpdateRequest)
                {
                    if (fur.AXID > 0)
                    {
                        var f = result.Find(x => x.Certificate.AXID == fur.AXID);
                        if (f != null)
                            f.UpdateRequest = fur;
                    }
                    else
                    {
                        result.Add(new CertificateResult
                        {
                            UpdateRequest = fur,
                        });
                    }
                }
            }
            return result;
        }

        public CertificateResult Get(string employeeID, long axID, bool essInternalOnly = false)
        {
            var result = new CertificateResult();
            var tasks = new List<Task<TaskRequest<Certificate>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly) { 
                    return TaskRequest<Certificate>.Create("AX", new Certificate());
                }
                var adapter = new EmployeeAdapter(Configuration);
                var certificate = adapter.GetCertificates(employeeID)
                                    .Find(x => x.AXID == axID);
                return TaskRequest<Certificate>.Create("AX", certificate);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var certificate = this.MongoDB.GetCollection<Certificate>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == axID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Certificate>.Create("DB", certificate);
            }));

            // Run process concurently
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Certificate = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }
            else
            {
                throw new Exception("Unable to get file");
            }

            return result;
        }

        public Certificate GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<Certificate>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }

        public void Discard(string requestID)
        {
            var certificate = this.MongoDB.GetCollection<Certificate>().Find(x => x.Id == requestID).FirstOrDefault();
            if (certificate != null)
            {
                this.MongoDB.Delete(certificate);
                if (System.IO.File.Exists(certificate.Filepath))
                {
                    System.IO.File.Delete(certificate.Filepath);
                }
            }
        }

        public void SatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Families
                tasks.Add(Task.Run(() =>
                {                    
                    var certificate = MongoDB.GetCollection<Certificate>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (certificate == null) return TaskRequest<object>.Create("certificate", false);

                    var newFilepath = Tools.ArchiveFile(certificate.Filepath);
                    var result = MongoDB.GetCollection<Certificate>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<Certificate>.Update
                            .Set(d => d.Status, newStatus)
                            .Set(d => d.Filepath, newFilepath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("certificate", result.IsAcknowledged);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update certificate data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Certificate request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.CERTIFICATE,
                            NotificationAction.OPEN_EMPLOYEE_CERTIFICATE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

        public List<CertificateResult> GetSMobile(string employeeID)
        {
            // For mobile, Get only from mongodb, file cannot access from AX
            var tasks = new List<Task<TaskRequest<List<Certificate>>>>
            {
                Task.Run(() =>
                {
                    return TaskRequest<List<Certificate>>.Create("AX",
                        new List<Certificate>());
                }),
                Task.Run(() =>
                {
                    return TaskRequest<List<Certificate>>.Create("DB", this.MongoDB.GetCollection<Certificate>()
                        .Find(x => x.EmployeeID == employeeID ).ToList());
                })
            };

            var t = Task.WhenAll(tasks);
            try { t.Wait(); }
            catch (Exception) { throw; }

            // Combine result
            var result = new List<CertificateResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var Certificates = new List<Certificate>();
                var CertificatesUpdateRequest = new List<Certificate>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        Certificates = r.Result;
                    else
                        CertificatesUpdateRequest = r.Result;

                // Merge certificates data
                foreach (var f in Certificates)
                {
                    result.Add(new CertificateResult
                    {
                        Certificate = f,
                    });
                }

                foreach (var fur in CertificatesUpdateRequest)
                {
                    if (fur.AXID > 0)
                    {
                        //var f = result.Find(x => x.Certificate.AXID == fur.AXID);
                        //if (f != null)
                        //{
                        //    f.UpdateRequest = fur;
                        //}                            
                    }
                    else
                    {
                        result.Add(new CertificateResult
                        {
                            UpdateRequest = fur,
                        });
                    }
                }
            }

            return result;
        }

        public Certificate GetByID(String employeeID, String id)
        {
            return this.MongoDB.GetCollection<Certificate>()
                                .Find(x => x.EmployeeID == employeeID && x.Id == id)
                                .FirstOrDefault();
        }
    }

    public class CertificateResult
    {
        public Certificate Certificate { get; set; }
        public Certificate UpdateRequest { get; set; }
    }

    public class CertificateForm 
    {
        public string JsonData { get; set; }
        public string Reason { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class CertificateType
    {
        public long AXID { get; set; }
        public string TypeID { get; set; }
        public string Description { get; set; }
        public bool ReqRenew { get; set; }
    }
}
