using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using KANO.Core.Service.AX;
using KANO.Core.Service;

namespace KANO.Core.Model
{
    [Collection("MedicalBenefit")]
    [BsonIgnoreExtraElements]
    public class MedicalBenefit : BaseDocumentVerification, IMongoPreSave<MedicalBenefit>
    {        
        public string RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public KESSCRServices.KESSStatusClaimReimburse RequestStatus { get; set; }
        public Family Family { get; set; } 
        public KESSCRServices.KESSJenisRawat TypeID { get; set; } 
        public double TotalAmount { get; set; } //= new EmployeeMedicalBenefit();
        public List<MedicalBenefitDetail> Details { get; set; }

        public MedicalBenefit() : base() { }

        public MedicalBenefit(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public new void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);
            //this.RequestID = "MB-"+this.EmployeeID + "-" + SequenceNo.Get(db, "MedicalBenefitRequest").ClaimAsInt(db).ToString("000000", new CultureInfo("en-US"));
        }

        public MedicalBenefit Get(string requestID)
        {
            var result = new MedicalBenefit();
            var tasks = new List<Task<TaskRequest<MedicalBenefit>>> {
                //Task.Run(() =>
                //{
                //    if (essInternalOnly)
                //    {
                //        return TaskRequest<Family>.Create("AX", new Family());
                //    }
                //    var adapter = new EmployeeAdapter(Configuration);
                //    var family = adapter.GetFamilies(employeeID)
                //                        .Find(x => x.AXID == axID);
                //    return TaskRequest<Family>.Create("AX", family);
                //}),
                Task.Run(() => {
                    var medicalbenefit = this.MongoDB.GetCollection<MedicalBenefit>()
                                    .Find(x => x.RequestID == requestID)
                                    .FirstOrDefault();
                    return TaskRequest<MedicalBenefit>.Create("DB", medicalbenefit);
                })
            };

            // Run process concurently
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "DB")
                        result = r.Result;
            }
            else
            {
                throw new Exception();
            }

            return result;
        }
        
        public List<MedicalBenefit> GetS(string employeeID, DateRange dateRange, int status=-1)
        {
            var range = Tools.normalizeFilter(dateRange);
            var result = new List<MedicalBenefit>();
            var tasks = new List<Task<TaskRequest<List<MedicalBenefit>>>> {
                Task.Run(() =>
                {
                    var adapter = new BenefitAdapter(Configuration);
                    var data = adapter.GetS(employeeID, dateRange);
                    return TaskRequest<List<MedicalBenefit>>.Create("AX", data);
                }),
                Task.Run(() => {
                    var data = new List<MedicalBenefit>();
                    if (status < 0)
                    {
                        data = this.MongoDB.GetCollection<MedicalBenefit>().Find(x => x.EmployeeID == employeeID && x.RequestDate >= range.Start & x.RequestDate <= range.Finish && x.Status == UpdateRequestStatus.InReview).ToList();
                    }
                    else {
                        data = this.MongoDB.GetCollection<MedicalBenefit>().Find(x => x.EmployeeID == employeeID && x.RequestDate >= range.Start & x.RequestDate <= range.Finish && x.Status == (UpdateRequestStatus) status).ToList();
                    }
                    return TaskRequest<List<MedicalBenefit>>.Create("DB", data);
                })
            };

            // Run process concurently
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    result.AddRange(r.Result);
            }
            else
            {
                throw new Exception();
            }

            return result;
        }

        public MedicalBenefit GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<MedicalBenefit>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }

        public void SatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                var name = " ";
                updateOptions.IsUpsert = false;

                tasks.Add(Task.Run(() =>
                {
                    var benefit = MongoDB.GetCollection<MedicalBenefit>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID).FirstOrDefault();
                    if (benefit == null) return TaskRequest<object>.Create("benefit", false);

                    name = " for self ";
                    if (benefit.Family.AXID > 0) name = $" for {benefit.Family.Name} ";

                    var newFilepath = Tools.ArchiveFile(benefit.Filepath);
                    var result = MongoDB.GetCollection<MedicalBenefit>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID,
                        Builders<MedicalBenefit>.Update
                            .Set(d => d.Filepath, newFilepath)
                            .Set(d => d.Status, newStatus),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("benefit", result.IsAcknowledged);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update benefit '{employeeID}' :\n{e.Message}");
                }

                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Medical benefit request{name}is {newStatus.ToString()}",
                             Notification.DEFAULT_SENDER,
                            NotificationModule.BENEFIT,
                            NotificationAction.APPROVE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

        public static List<string> GetRequestIntention()
        {
            return new List<string>(Enum.GetNames(typeof(RequestIntention)));
        }

        public static List<string> GetStatus()
        {
            return new List<string>(Enum.GetNames(typeof(UpdateRequestStatus)));
        }
    }
    public class MedicalBenefitForm {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
    public class MedicalType
    {
        public long AXID { get; set; }
        public string Description { get; set; }
        public int TypeID { get; set; }
    }
    public class DocumentType
    {
        public int TypeID { get; set; }
        public string Description { get; set; }
    }
    public class MedicalFieldAttachmentForm
    {
        public string Field { get; set; }
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
    public class BenefitLimit
    {
        public long AXID { get; set; } = -1;
        public decimal CreditLimitAmount { get; set; }
        public string Grade { get; set; }
        public string GradeDescription { get; set; }        
    }

    public class EmployeeBenefitLimit
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public decimal Balance { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public decimal CreditLimitAmount { get; set; }
        public string Grade { get; set; }
        public string GradeDescription { get; set; }        
    }
}
