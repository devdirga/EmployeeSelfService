using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using KANO.Core.Lib.Helper;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("VoucherRequest")]
    [BsonIgnoreExtraElements]
    public class VoucherRequest : BaseT, IMongoPreSave<VoucherRequest>
    {        
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        public VoucherRequest() { }

        public VoucherRequest(IMongoDatabase mongoDB, IConfiguration configuration){ 
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }                

        public string EmployeeID { get; set; }        
        public string EmployeeName { get; set; }      
        [BsonId]
        public string Id { get; set; }        
        public DateTime CreatedDate { get; set; }        
        public string Note { get; set; }
        // public List<VoucherRequestDetail> Detail { get; set; } = new List<VoucherRequestDetail>();

		public void PreSave(IMongoDatabase db)
		{
			if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
		}
	}

    [Collection("VoucherRequestDetail")]
    [BsonIgnoreExtraElements]
    public class VoucherRequestDetail: BaseUpdateRequest, IMongoPreSave<VoucherRequestDetail>
    {
        public string VoucherRequestID { get; set; }                
        public DateTime GeneratedForDate { get; set; }                

        public VoucherRequestDetail() : base() {}
        //public string AXRequestID { get; set; }

        public VoucherRequestDetail(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update & Genererate Voucher Request
                tasks.Add(Task.Run(() =>
                {
                    var voucherRequest = MongoDB.GetCollection<VoucherRequestDetail>()
                               .Find(x => x.AXRequestID == AXRequestID && x.EmployeeID == employeeID)
                               .FirstOrDefault();
                    voucherRequest.Status = newStatus;
                    MongoDB.Save(voucherRequest);
                    try
                    {
                        var vouchers = MongoDB.GetCollection<Voucher>()
                               .Find(x => x.EmployeeID == employeeID)
                               .ToList();

                        var newVoucher = new Voucher(MongoDB, Configuration);

                        newVoucher.EmployeeID = employeeID;
                        newVoucher.EmployeeName = voucherRequest.EmployeeName;
                        newVoucher.GeneratedForDate = voucherRequest.GeneratedForDate;

                        MongoDB.Save(newVoucher);
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }
                    
                    return TaskRequest<object>.Create("updateRequest", true);

                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update voucher request data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"{request.Description} is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.CANTEEN,
                            NotificationAction.NONE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
    }
}
