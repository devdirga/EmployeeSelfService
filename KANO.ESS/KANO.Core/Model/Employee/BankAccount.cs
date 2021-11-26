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
    [Collection("BankAccount")]
    [BsonIgnoreExtraElements]
    public class BankAccount:BaseDocumentVerification, IMongoPreSave<BankAccount>
    {
        public string Type { set; get; }
        public string Description { set; get; }
        public string AccountID { set; get; }
        public string AccountNumber { set; get; }
        public string Name { set; get; }        

        public BankAccount() : base() { }

        public BankAccount(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<BankAccountResult> GetS(string employeeID, string axRequestID="", bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<List<BankAccount>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<List<BankAccount>>.Create("AX", new List<BankAccount>());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetBankAccounts(employeeID);
                return TaskRequest<List<BankAccount>>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = new List<BankAccount>();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    data = this.MongoDB.GetCollection<BankAccount>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .ToList();
                }
                else 
                {
                    data = this.MongoDB.GetCollection<BankAccount>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                    .ToList();
                }
                return TaskRequest<List<BankAccount>>.Create("DB", data);
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
            var result = new List<BankAccountResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var data = new List<BankAccount>();
                var dataUpdateRequest = new List<BankAccount>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        data = r.Result;
                    else
                        dataUpdateRequest = r.Result;

                // Merge identifications data
                foreach (var d in data)
                {
                    result.Add(new BankAccountResult
                    {
                        BankAccount = d
                    });
                }

                foreach (var dur in dataUpdateRequest)
                {
                    var f = result.Find(x => x.BankAccount != null && x.BankAccount.Name == dur.Name);
                    if (f != null)
                    {
                        f.UpdateRequest = dur;
                    }
                    else
                    {
                        result.Add(new BankAccountResult
                        {
                            UpdateRequest = dur,
                        });
                    }
                }
            }

            return result;
        }

        public BankAccountResult Get(string employeeID, long AXID, bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<BankAccount>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly) {
                    return TaskRequest<BankAccount>.Create("AX", new BankAccount());
                }
                var adapter = new EmployeeAdapter(this.Configuration);
                var data = adapter.GetBankAccounts(employeeID).Find(x=>x.AXID == AXID);
                return TaskRequest<BankAccount>.Create("AX", data);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var data = this.MongoDB.GetCollection<BankAccount>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == AXID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<BankAccount>.Create("DB", data);
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
            var result = new BankAccountResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {               
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.BankAccount = r.Result;
                    else
                        result.UpdateRequest = r.Result;                
            }

            return result;
        }

        public BankAccount GetByID(string id)
        {
            return this.MongoDB.GetCollection<BankAccount>()
                                .Find(x => x.Id==id)
                                .FirstOrDefault();
        }

        public BankAccount Update(BankAccount bankAccount, bool upsert = true) {            
            if (!string.IsNullOrWhiteSpace(bankAccount.Id))
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                this.MongoDB.GetCollection<BankAccount>().UpdateOne(
                    x => x.Id == bankAccount.Id,
                    Builders<BankAccount>.Update
                        .Set(b => b.AccountNumber, bankAccount.AccountNumber)
                        .Set(b => b.EmployeeID, bankAccount.EmployeeID),
                    updateOptions
                );
            }
            else if(upsert){
                this.MongoDB.Save(bankAccount);
            }

            return bankAccount;
        }

        public void Discard(string employeeID) {
            var result = this.MongoDB.GetCollection<BankAccount>()
                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview)).ToList();

            var delete = this.MongoDB.GetCollection<BankAccount>()
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

            var result = this.MongoDB.GetCollection<BankAccount>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<BankAccount>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }

        public BankAccount GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<BankAccount>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }
    }

    public class BankAccountForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class BankAccountResult
    {
        public BankAccount BankAccount { get; set; } = null;
        public BankAccount UpdateRequest { get; set; } = null;
    }
}
