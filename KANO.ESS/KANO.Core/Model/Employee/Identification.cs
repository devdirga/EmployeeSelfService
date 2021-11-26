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
    public class IdentificationType {
        public long AXID { set; get; }
        public string Type { set; get; }
        public string Description { set; get; }        
    }

    [Collection("Identification")]
    [BsonIgnoreExtraElements]
    public class Identification: BaseDocumentVerification, IMongoPreSave<Identification>
    {
        public string Type { set; get; }
        public string IssuingAggency { set; get; }        
        public string Description { set; get; }        
        public string Number { set; get; }                

        public Identification() : base() { }

        public Identification(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<IdentificationResult> GetS(string employeeID, string axRequestID = "", bool essInternalOnly = false) {
            var tasks = new List<Task<TaskRequest<List<Identification>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<List<Identification>>.Create("AX", new List<Identification>());
                }

                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetIdentifications(employeeID);
                return TaskRequest<List<Identification>>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = new List<Identification>();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    data = this.MongoDB.GetCollection<Identification>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .ToList();
                }
                else 
                {
                    data = this.MongoDB.GetCollection<Identification>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID==axRequestID)
                                    .ToList();
                }
                return TaskRequest<List<Identification>>.Create("DB", data);
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
            var result = new List<IdentificationResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var data = new List<Identification>();
                var dataUpdateRequest = new List<Identification>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        data = r.Result;
                    else
                        dataUpdateRequest = r.Result;

                // Merge identifications data
                foreach (var d in data)
                {
                    result.Add(new IdentificationResult
                    {
                        Identification = d
                    });
                }

                foreach (var dur in dataUpdateRequest)
                {

                    var f = result.Find(x => x.Identification != null && x.Identification.Type == dur.Type);
                    if (f != null)
                    {
                        f.UpdateRequest = dur;
                    }
                    else
                    {
                        result.Add(new IdentificationResult
                        {
                            UpdateRequest = dur,
                        });
                    }
                }
            }

            return result;
        }

        public IdentificationResult Get(string employeeID, long axID)
        {
            var tasks = new List<Task<TaskRequest<Identification>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetIdentifications(employeeID).Find(x=>x.AXID == axID);
                return TaskRequest<Identification>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = this.MongoDB.GetCollection<Identification>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == axID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Identification>.Create("DB", data);
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
            var result = new IdentificationResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {                
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Identification = r.Result;
                    else
                        result.UpdateRequest = r.Result;               

            }

            return result;
        }

        public Identification GetByID(string id)
        {
            return this.MongoDB.GetCollection<Identification>()
                                .Find(x => x.Id == id)
                                .FirstOrDefault();
        }

        public Identification Update(Identification identification, bool upsert = true)
        {
            if (!string.IsNullOrWhiteSpace(identification.Id))
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                this.MongoDB.GetCollection<Identification>().UpdateOne(
                    x => x.Id == identification.Id,
                    Builders<Identification>.Update
                        .Set(b => b.Number, identification.Number)
                        .Set(b => b.EmployeeID, identification.EmployeeID),
                    updateOptions
                );
            }
            else if (upsert)
            {
                this.MongoDB.Save(identification);
            }

            return identification;
        }

        public void Discard(string employeeID) {
            var identifications = this.MongoDB.GetCollection<Identification>()
                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview)).ToList();

            var delete = this.MongoDB.GetCollection<Identification>()
                    .DeleteMany(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview));

            //if (delete.DeletedCount >= identifications.Count) {
                foreach (var data in identifications) {
                    Tools.DeleteFile(data.Filepath);
                }
            //}
        }

        public bool SetAXRequestID(string employeeID, string AXRequestID)
        {
            var updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = false;

            var result = this.MongoDB.GetCollection<Identification>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<Identification>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }

        public Identification GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<Identification>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }
    }

    public class IdentificationForm {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class IdentificationResult
    {
        public Identification Identification { get; set; } = null;
        public Identification UpdateRequest { get; set; } = null;
    }

}
