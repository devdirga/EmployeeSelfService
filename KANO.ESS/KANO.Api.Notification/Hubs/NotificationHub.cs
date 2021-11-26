using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace KANO.Api.Notification.Hubs
{
    public class NotificationHub : Hub
    {
        public IConfiguration Configuration { get; }
        public IMongoDatabase DB { get; }
        public IMongoManager Mongo { get; }
        public static List<UserConnection> UserList { get; set; } = new List<UserConnection>();

        public NotificationHub(IMongoManager mongo, IConfiguration config)
        {
            Configuration = config;
            Mongo = mongo;
            DB = mongo.Database();
        }

        public async Task SendNotification(Core.Model.Notification notification)
        {
            var user = UserList.Find(x => x.EmployeeID == notification.Receiver);
            if (user != null) {
                await Clients.Client(user.ConnectionID).SendAsync("ReceiveNotification", notification);
            }
        }

        public async Task MarkNotificationsAsRead(List<Core.Model.Notification> notifications)
        {
            if (notifications.Count > 0) {
                foreach (var n in notifications) {
                    n.Read = true;
                    DB.Save(n);
                }
            }
        }
        
        public async Task MarkAllNotificationsAsRead()
        {            
            var user = UserList.Find(x => x.ConnectionID == this.Context.ConnectionId);
            if (user != null)
            {
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;
                DB.GetCollection<Core.Model.Notification>().UpdateMany(
                    x => x.Receiver == user.EmployeeID,
                    Builders<Core.Model.Notification>.Update
                        .Set(b => b.Read, true),
                    updateOptions
                );
            }
        }

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext().Request.Query["s"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                var employeeID = token;//Hasher.Decrypt(HttpUtility.UrlDecode(token)).Split("_")[0];
                var user = UserList.Find(x => x.EmployeeID == employeeID);
                if (user != null)
                {
                    user.ConnectionID = Context.ConnectionId;
                }
                else
                {
                    UserList.Add(new UserConnection{
                        ConnectionID = Context.ConnectionId,
                        EmployeeID = employeeID,
                        ConnectedAt = DateTime.Now
                    });

                    // Cleaning old session to enhance performance
                    if (UserList.Count > 150) {
                        UserList.RemoveAll(x => Math.Abs(x.ConnectedAt.Subtract(DateTime.Now).Hours) > 8);
                    }                    
                }                
            }
            
            await base.OnConnectedAsync();
        }
    }

    public class UserConnection
    {
        public string EmployeeID { set; get; }
        public string ConnectionID { set; get; }
        public DateTime ConnectedAt { set; get; }
    }
}
