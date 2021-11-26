using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class ElectronicAddressType
    {
        public long AXID { set; get; }
        public string Type { set; get; }
        public string Description { set; get; }
    }

    [Collection("ElectronicAddress")]
    [BsonIgnoreExtraElements]
    public class ElectronicAddress:BaseDocumentVerification, IMongoPreSave<ElectronicAddress>
    {
        public KESSHCMServices.LogisticsElectronicAddressMethodType Type { set; get; }
        public string TypeDescription { set; get; }
        public string Locator { set; get; }                

        public ElectronicAddress() : base() { }

        public ElectronicAddress(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<ElectronicAddressResult> GetS(string employeeID, string axRequestID="", bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<List<ElectronicAddress>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<List<ElectronicAddress>>.Create("AX", new List<ElectronicAddress>());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetElectronicAddresses(employeeID);
                return TaskRequest<List<ElectronicAddress>>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = new List<ElectronicAddress>();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    data = this.MongoDB.GetCollection<ElectronicAddress>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .ToList();
                }
                else 
                {
                    data = this.MongoDB.GetCollection<ElectronicAddress>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID==axRequestID)
                                    .ToList();
                }
                return TaskRequest<List<ElectronicAddress>>.Create("DB", data);
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
            var result = new List<ElectronicAddressResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var data = new List<ElectronicAddress>();
                var dataUpdateRequest = new List<ElectronicAddress>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        data = r.Result;
                    else
                        dataUpdateRequest = r.Result;

                // Merge identifications data
                foreach (var d in data)
                {
                    result.Add(new ElectronicAddressResult
                    {
                        ElectronicAddress = d
                    });
                }

                foreach (var dur in dataUpdateRequest)
                {
                    var f = result.Find(x => x.ElectronicAddress != null && x.ElectronicAddress.AXID == dur.AXID);
                    if (f != null)
                    {
                        f.UpdateRequest = dur;
                    }
                    else
                    {
                        result.Add(new ElectronicAddressResult
                        {
                            UpdateRequest = dur,
                        });
                    }
                }
            }

            return result;
        }

        public ElectronicAddressResult Get(string employeeID, long AXID, bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<ElectronicAddress>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly) {
                    return TaskRequest<ElectronicAddress>.Create("AX", new ElectronicAddress());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetElectronicAddresses(employeeID).Find(x=>x.AXID == AXID);
                return TaskRequest<ElectronicAddress>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = this.MongoDB.GetCollection<ElectronicAddress>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == AXID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<ElectronicAddress>.Create("DB", data);
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
            var result = new ElectronicAddressResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {               
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.ElectronicAddress = r.Result;
                    else
                        result.UpdateRequest = r.Result;                
            }

            return result;
        }

        public ElectronicAddress GetByID(string id)
        {
            return this.MongoDB.GetCollection<ElectronicAddress>()
                                .Find(x => x.Id == id)
                                .FirstOrDefault();
        }

        public ElectronicAddress Update(ElectronicAddress address, bool upsert = true) {
            if (!string.IsNullOrWhiteSpace(address.Id))
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                this.MongoDB.GetCollection<ElectronicAddress>().UpdateOne(
                    x => x.Id == address.Id,
                    Builders<ElectronicAddress>.Update
                        .Set(b => b.Locator, address.Locator)
                        .Set(b => b.EmployeeID, address.EmployeeID),
                    updateOptions
                );
            }
            else if(upsert){
                this.MongoDB.Save(address);
            }

            return address;
        }

        public void Discard(string employeeID) {
            var result = this.MongoDB.GetCollection<ElectronicAddress>()
                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview)).ToList();

            var delete = this.MongoDB.GetCollection<ElectronicAddress>()
                    .DeleteMany(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview));

            //if (delete.DeletedCount >= result.Count) {
            foreach (var data in result)
            {
                Tools.DeleteFile(data.Filepath);
            }
            //}  
        }

        public bool SetAXRequestID(string employeeID, string AXRequestID)
        {
            var updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = false;

            var result = this.MongoDB.GetCollection<ElectronicAddress>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<ElectronicAddress>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }
    }

    public class ElectronicAddressForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class ElectronicAddressResult
    {
        public ElectronicAddress ElectronicAddress { get; set; } = null;
        public ElectronicAddress UpdateRequest { get; set; } = null;
    }
}
