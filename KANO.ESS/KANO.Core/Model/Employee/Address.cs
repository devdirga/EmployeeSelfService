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
    [Collection("Address")]
    [BsonIgnoreExtraElements]
    public class Address : BaseDocumentVerification, IMongoPreSave<Address>
    {
        public string Street { set; get; }
        public string City { set; get; }
        [BsonIgnore]
        public string Value { set; get; }
        [BsonIgnore]
        public string OriginalValue { set; get; }
        public Address() : base() { }
        public Address(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public AddressResult Get(string employeeID, string axRequestID = "", bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<Address>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<Address>.Create("AX", new Address());
                }

                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetAddress(employeeID);
                return TaskRequest<Address>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = new Address();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    data = this.MongoDB.GetCollection<Address>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .FirstOrDefault();
                }
                else
                {
                    data = this.MongoDB.GetCollection<Address>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                    .FirstOrDefault();
                }

                return TaskRequest<Address>.Create("DB", data);
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
            var result = new AddressResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Address = r.Result;
                    else
                        result.UpdateRequest = r.Result;

            }

            return result;
        }       

        public AddressResult Get(string employeeID, long axID)
        {
            var tasks = new List<Task<TaskRequest<Address>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetAddress(employeeID);
                return TaskRequest<Address>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = this.MongoDB.GetCollection<Address>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXID == axID)
                                    .FirstOrDefault();
                return TaskRequest<Address>.Create("DB", data);
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
            var result = new AddressResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Address = r.Result;
                    else
                        result.UpdateRequest = r.Result;

            }

            return result;
        }

        public Address GetByID(string id)
        {
            return this.MongoDB.GetCollection<Address>()
                                .Find(x => x.Id == id)
                                .FirstOrDefault();
        }


        public Address Update(Address address, bool upsert = true)
        {
            if (!string.IsNullOrWhiteSpace(address.Id))
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                this.MongoDB.GetCollection<Address>().UpdateOne(
                    x => x.Id == address.Id,
                    Builders<Address>.Update
                        .Set(b => b.Street, address.Street)
                        .Set(b => b.City, address.City)
                        .Set(b => b.EmployeeID, address.EmployeeID),
                    updateOptions
                );
            }
            else if (upsert)
            {
                this.MongoDB.Save(address);
            }

            return address;
        }

        public void Discard(string employeeID)
        {
            var result = this.MongoDB.GetCollection<Address>()
                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview)).ToList();

            var delete = this.MongoDB.GetCollection<Address>()
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

            var result = this.MongoDB.GetCollection<Address>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<Address>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }
    }

    public class AddressForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class AddressResult
    {
        public Address Address { get; set; } = null;
        public Address UpdateRequest { get; set; } = null;
    }
}
