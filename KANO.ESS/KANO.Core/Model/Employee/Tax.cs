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
    [Collection("Tax")]
    [BsonIgnoreExtraElements]
    public class Tax:BaseDocumentVerification, IMongoPreSave<Tax>
    {
        public KESSHCMServices.HRMTaxCardType Type { set; get; }
        public string TypeDescription { set; get; }
        public string NPWP { set; get; }                

        public Tax() : base() { }

        public Tax(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<TaxResult> GetS(string employeeID, string axRequestID="", bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<List<Tax>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<List<Tax>>.Create("AX", new List<Tax>());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetTaxes(employeeID);
                return TaskRequest<List<Tax>>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = new List<Tax>();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    data = this.MongoDB.GetCollection<Tax>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .ToList();
                }
                else 
                {
                    data = this.MongoDB.GetCollection<Tax>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                    .ToList();
                }
                return TaskRequest<List<Tax>>.Create("DB", data);
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
            var result = new List<TaxResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var data = new List<Tax>();
                var dataUpdateRequest = new List<Tax>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        data = r.Result;
                    else
                        dataUpdateRequest = r.Result;

                // Merge identifications data
                foreach (var d in data)
                {
                    result.Add(new TaxResult
                    {
                        Tax = d
                    });
                }

                foreach (var dur in dataUpdateRequest)
                {
                    var f = result.Find(x => x.Tax != null && x.Tax.AXID == dur.AXID);
                    if (f != null)
                    {
                        f.UpdateRequest = dur;
                    }
                    else
                    {
                        result.Add(new TaxResult
                        {
                            UpdateRequest = dur,
                        });
                    }
                }
            }

            return result;
        }

        public TaxResult Get(string employeeID, long AXID, bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<Tax>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly) {
                    return TaskRequest<Tax>.Create("AX", new Tax());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetTaxes(employeeID).Find(x=>x.AXID == AXID);
                return TaskRequest<Tax>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = this.MongoDB.GetCollection<Tax>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == AXID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Tax>.Create("DB", data);
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
            var result = new TaxResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {               
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Tax = r.Result;
                    else
                        result.UpdateRequest = r.Result;                
            }

            return result;
        }

        public Tax GetByID(string id)
        {
            return this.MongoDB.GetCollection<Tax>()
                                .Find(x => x.Id == id)
                                .FirstOrDefault();
        }

        public Tax Update(Tax tax, bool upsert = true) {
            if (!string.IsNullOrWhiteSpace(tax.Id))
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                this.MongoDB.GetCollection<Tax>().UpdateOne(
                    x => x.Id == tax.Id,
                    Builders<Tax>.Update
                        .Set(b => b.NPWP, tax.NPWP)
                        .Set(b => b.EmployeeID, tax.EmployeeID),
                    updateOptions
                );
            }
            else if(upsert){
                this.MongoDB.Save(tax);
            }

            return tax;
        }

        public void Discard(string employeeID) {
            var result = this.MongoDB.GetCollection<Tax>()
                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview)).ToList();

            var delete = this.MongoDB.GetCollection<Tax>()
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

            var result = this.MongoDB.GetCollection<Tax>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<Tax>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }
    }

    public class TaxForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class TaxResult
    {
        public Tax Tax { get; set; } = null;
        public Tax UpdateRequest { get; set; } = null;
    }
}
