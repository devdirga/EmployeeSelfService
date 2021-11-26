using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("NotificationSubscriptions")]
    [BsonIgnoreExtraElements]
    public class NotificationSubscription : IMongoPreSave<NotificationSubscription>
    {        
        [BsonIgnore]
        [JsonIgnore]
        private IConfiguration Configuration;
        [BsonIgnore]
        [JsonIgnore]
        private IMongoDatabase MongoDB;        
 
        [BsonId]
        public string Id { get; set; }
        public DateTime CreatedDate { get; set; }               
        public DateTime LastUpdated { get; set; }               
        public string Receiver { get; set; }       
        public string ReceiverName { get; set; }
        [BsonIgnore]
        public SubscriptionPackage Subscription { get; set; }
        public List<SubscriptionPackage> Subscriptions { get; set; } = new List<SubscriptionPackage>();

        public NotificationSubscription(){}

        public NotificationSubscription(IConfiguration config, IMongoDatabase db) {
            Configuration = config;
            MongoDB = db;
        }     

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id)) {
                this.Id = ObjectId.GenerateNewId().ToString();
                this.CreatedDate = DateTime.Now;
            }

            this.LastUpdated = DateTime.Now;

            if (string.IsNullOrWhiteSpace(this.Receiver))
                throw new Exception("Unable to save due to empty employee id");
        }

        public NotificationSubscription GetSubscriptions(string employeeID) {
            return MongoDB.GetCollection<NotificationSubscription>().Find(x => x.Receiver == employeeID).FirstOrDefault();
        }

        public void Subscribe(string employeeID, string employeeName, SubscriptionPackage subscriptionPackage)
        {
            try
            {
                var sub = this.GetSubscriptions(employeeID);
                if (sub == null)
                {
                    sub = new NotificationSubscription
                    {
                        Receiver = employeeID,
                        ReceiverName = employeeName,
                        Subscriptions = new List<SubscriptionPackage>(),
                    };
                }

                var existing = sub.Subscriptions.Find(x => x.EndPoint == subscriptionPackage.EndPoint);
                if (existing == null)
                {
                    sub.Subscriptions.Add(subscriptionPackage);
                    MongoDB.Save(sub);
                }

            }
            catch (Exception)
            {

                throw;
            }            
        }

        public void Subscribe(string employeeID, SubscriptionPackage subscriptionPackage)
        {
            try
            {                
                var sub = this.GetSubscriptions(employeeID);
                if (sub == null)
                {
                    var user = MongoDB.GetCollection<User>().Find(x => x.Username == employeeID).FirstOrDefault();
                    sub = new NotificationSubscription
                    {
                        Receiver = employeeID,
                        ReceiverName = user?.FullName,
                        Subscriptions = new List<SubscriptionPackage>(),
                    };
                }

                var existing = sub.Subscriptions.Find(x => x.EndPoint == subscriptionPackage.EndPoint);
                if (existing == null)
                {
                    subscriptionPackage.Id = ObjectId.GenerateNewId().ToString();
                    sub.Subscriptions.Add(subscriptionPackage);                    
                    MongoDB.Save(sub);
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public void Unsubscribe(string employeeID, SubscriptionPackage subscriptionPackage)
        {
            try
            {
                var sub = this.GetSubscriptions(employeeID);
                if (sub == null) throw new Exception($"Unable to find notification subscription for employee {employeeID}");

                var existing = sub.Subscriptions.Find(x => x.EndPoint == subscriptionPackage.EndPoint);
                if (existing == null) throw new Exception($"Unable to find subscription for employee {employeeID}");

                sub.Subscriptions.Remove(existing);
                MongoDB.Save(sub);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool HasSubscribed(string employeeID, SubscriptionPackage subscriptionPackage) {
            var sub = this.GetSubscriptions(employeeID);            
            if (sub == null) return false;

            return sub.Subscriptions.Find(x => x.EndPoint == subscriptionPackage.EndPoint) != null;
        }
    }

    public class SubscriptionPackage {        
        public string Id { set; get; }
        public string EndPoint { set; get; }
        public long ExpirationTime { set; get; }
        public SubscriptionKeys Keys { set; get; }
    }

    public class SubscriptionKeys {        
        public string P256DH { set; get; }
        public string Auth { set; get; }
    }

    public class VAPIDKeys { 
        public string PublicKey { set; get; }
        [JsonIgnore]
        public string PrivateKey { set; get; }
    }
}
