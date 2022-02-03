using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KANO.Api.Notification.Hubs;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using WebPush;

namespace KANO.Api.Notification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {

        private readonly IHubContext<NotificationHub> HubContext;
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private UpdateRequest _updateRequest;
        private NotificationSubscription _notificationSubscription;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public NotificationController(IMongoManager mongo, IConfiguration conf, IHubContext<NotificationHub> hubContext)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            HubContext = hubContext;

            _updateRequest = new UpdateRequest(DB);
            _notificationSubscription = new NotificationSubscription(conf, DB);
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] Core.Model.Notification param)
        {
            param.Timestamp = DateTime.Now;
            if (string.IsNullOrWhiteSpace(param.Receiver))
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Receiver could not be empty");
            }

            if (string.IsNullOrWhiteSpace(param.Sender))
            {
                param.Sender = Core.Model.Notification.DEFAULT_SENDER;
            }
            
            try
            {
                var tasks = new List<Task<TaskRequest<Exception>>>();

                // Send via web-push
                tasks.Add(Task.Run(() =>
                {
                    Exception error = null;
                    try
                    {
                    // VAPID Details
                    var subject = Configuration["Request:PushNotificationSubject"];
                        var publicKey = Configuration["Request:PushNotificationPublicKey"];
                        var privateKey = Configuration["Request:PushNotificationPrivateKey"];
                        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

                        var receiver = DB.GetCollection<NotificationSubscription>().Find(x => x.Receiver == param.Receiver).FirstOrDefault();
                        if (receiver != null)
                        {
                            var subscriptionPackages = new List<SubscriptionPackage>();
                            foreach (var subscription in receiver.Subscriptions)
                            {
                                var push = new PushSubscription(subscription.EndPoint, subscription.Keys.P256DH, subscription.Keys.Auth);
                                var webPushClient = new WebPushClient();
                                try
                                {
                                    var payload = JsonConvert.SerializeObject(param);
                                    webPushClient.SendNotification(push, payload, vapidDetails);
                                    subscriptionPackages.Add(subscription);
                                }
                                catch (WebPushException e)
                                {
                                    Console.WriteLine($"{DateTime.Now} Unable to send notification to {param.Receiver} due to {Format.ExceptionString(e)}");
                                }
                            }
                            receiver.Subscriptions = subscriptionPackages;
                            DB.Save(receiver);
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    return TaskRequest<Exception>.Create("webPush", error);
                }));

                // Send via socket
                tasks.Add(Task.Run(async () =>
                {
                    Exception error = null;
                    try
                    {
                        var users = NotificationHub.UserList;
                        if (users != null && users.Count > 0)
                        {
                            var user = users.Find(x => x.EmployeeID == param.Receiver);
                            if (user != null)
                            {
                                await HubContext.Clients.Client(user.ConnectionID).SendAsync("ReceiveNotification", param);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return TaskRequest<Exception>.Create("socket", error);
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
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                    {
                        var e = (Exception)r.Result;
                        if (e != null)
                            switch (r.Label)
                            {
                                case "webPush":
                                    throw new Exception("Unable to send using web-push", e);
                                case "socket":
                                    throw new Exception("Unable to send using socket", e);
                                default:
                                    break;
                            }
                    }
                }

                DB.Save(param);
                return ApiResult<object>.Ok("Notification has been sent successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Send notification error : {Format.ExceptionString(e, true)}");
            }            
        }

        [HttpGet("get/{employeeID}/{limit=10}/{offset=0}/{filter}")]
        public async Task<IActionResult> Get(string employeeID, int limit = 10, int offset = 0, string filter = "all")
        {
            if (string.IsNullOrWhiteSpace(employeeID))
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Employee id could not be empty");
            }

            var notifications = new List<Core.Model.Notification>();
            var collection = DB.GetCollection<Core.Model.Notification>();

            long total = 0;
            IFindFluent<Core.Model.Notification, Core.Model.Notification> collectionFind;
            switch (filter)
            {
                case "all":
                    collectionFind = collection.Find(x => x.Receiver == employeeID);
                    total = collection.CountDocuments(x => x.Receiver == employeeID);
                    break;
                case "unread":
                    collectionFind = collection.Find(x => x.Receiver == employeeID && x.Read == false);
                    total = collection.CountDocuments(x => x.Receiver == employeeID && x.Read == false);
                    break;
                case "read":
                    collectionFind = collection.Find(x => x.Receiver == employeeID && x.Read == true);
                    total = collection.CountDocuments(x => x.Receiver == employeeID && x.Read == true);
                    break;
                default:
                    collectionFind = collection.Find(x => x.Receiver == employeeID);
                    total = collection.CountDocuments(x => x.Receiver == employeeID);
                    break;
            }

            var result = collectionFind.Limit(limit)
                .Skip(offset)
                .SortByDescending(x => x.Timestamp);

            if (result.Any())
            {
                notifications = result.ToList();
            }

            return ApiResult<List<Core.Model.Notification>>.Ok(notifications, total);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] NotificationSubscription param)
        {
            try
            {
                if (string.IsNullOrEmpty(param.ReceiverName))
                    _notificationSubscription.Subscribe(param.Receiver, param.Subscription);
                else
                    _notificationSubscription.Subscribe(param.Receiver, param.ReceiverName, param.Subscription);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to create subscription : {Format.ExceptionString(e)}");
            }

            return ApiResult<NotificationSubscription>.Ok($"Notification has been subscribed successfully");
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] NotificationSubscription param)
        {
            try
            {
                _notificationSubscription.Unsubscribe(param.Receiver, param.Subscription);
            }
            catch (Exception){}
            //catch (Exception e)
            //{
            //    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to create subscription : {Format.ExceptionString(e)}");
            //}

            return ApiResult<NotificationSubscription>.Ok($"Notification has been unsubscribed successfully");
        }

        [HttpPost("subscription/check")]
        public async Task<IActionResult> Check([FromBody] NotificationSubscription param)
        {
            var subscriptions = DB.GetCollection<NotificationSubscription>().Find(x => x.Receiver == param.Receiver).Project(x=>x.Subscriptions).FirstOrDefault();            
            var subscribed = false;

            if(subscriptions != null && param.Subscription != null){
                if(param.Subscription.Keys != null){
                    subscribed = subscriptions.Find(x=>
                        x.EndPoint == param.Subscription.EndPoint &&
                        x.Keys.Auth == param.Subscription.Keys.Auth &&
                        x.Keys.P256DH == param.Subscription.Keys.P256DH 
                    ) != null;
                }
            }

            return ApiResult<bool>.Ok(subscribed);
        }

        [HttpGet("generate/vapid")]
        public async Task<IActionResult> GenerateVAPID()
        {
            VapidDetails vapidKeys = VapidHelper.GenerateVapidKeys();
            return ApiResult<VapidDetails>.Ok(vapidKeys);
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpPost("msend")]
        public IActionResult MSend([FromBody] Core.Model.Notification param)
        {
            param.Timestamp = DateTime.Now;
            if (string.IsNullOrWhiteSpace(param.Receiver))
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Receiver could not be empty");
            }
            if (string.IsNullOrWhiteSpace(param.Sender))
            {
                param.Sender = Core.Model.Notification.DEFAULT_SENDER;
            }
            try
            {
                DB.Save(param);
                return ApiResult<object>.Ok("Notification has been sent successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Send notification error : {Format.ExceptionString(e, true)}");
            }
        }

        [Authorize]
        [HttpPost("mget")]
        public IActionResult MGet([FromBody] FetchParam param)
        {
            if (string.IsNullOrWhiteSpace(param.EmployeeID))
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Employee id could not be empty");
            }
            var notifications = new List<Core.Model.Notification>();
            var collection = DB.GetCollection<Core.Model.Notification>();
            long total = 0;
            IFindFluent<Core.Model.Notification, Core.Model.Notification> collectionFind;
            switch (param.Filter)
            {
                case "all":
                    collectionFind = collection.Find(x => x.Receiver == param.EmployeeID);
                    total = collectionFind.CountDocuments();
                    break;
                case "unread":
                    collectionFind = collection.Find(x => x.Receiver == param.EmployeeID && x.Read == false);
                    total = collectionFind.CountDocuments();
                    break;
                case "read":
                    collectionFind = collection.Find(x => x.Receiver == param.EmployeeID && x.Read == true);
                    total = collectionFind.CountDocuments();
                    break;
                default:
                    collectionFind = collection.Find(x => x.Receiver == param.EmployeeID);
                    total = collectionFind.CountDocuments();
                    break;
            }
            var result = collectionFind.SortByDescending(x => x.Timestamp).Limit(param.Limit).Skip(param.Offset);
            if (result.Any())
            {
                notifications = result.ToList();
            }
            return ApiResult<List<Core.Model.Notification>>.Ok(notifications, total);
        }

        [Authorize]
        [HttpPost("msetread")]
        public IActionResult MSetRead([FromBody] Core.Model.Notification param)
        {
            Core.Model.Notification notif = DB.GetCollection<Core.Model.Notification>().Find(x => x.Id == param.Id).FirstOrDefault();
            notif.Read = true;
            DB.Save(notif);
            return ApiResult<Core.Model.Notification>.Ok(notif);
        }

        /**
         * Send from Microsoft Dynamic AX 2012
         * 
         */
        [HttpPost("msendfromax")]
        public IActionResult MSendFromAx([FromBody] Core.Model.Notification param)
        {
            param.Timestamp = DateTime.Now;
            if (string.IsNullOrWhiteSpace(param.Receiver))
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Receiver could not be empty");
            }
            if (string.IsNullOrWhiteSpace(param.Sender))
            {
                param.Sender = Core.Model.Notification.DEFAULT_SENDER;
            }
            try
            {
                var api = Configuration.GetSection("Request:FcmApi").Value;
                var key = Configuration.GetSection("Request:FcmKey").Value;
                var user = new User(DB, Configuration);
                //SendToFirebase
                WebRequest webRequest = WebRequest.Create(new Uri(api));
                webRequest.Method = "POST";
                webRequest.Headers.Add($"Authorization: key={key}");
                webRequest.ContentType = "application/json";
                byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                {
                    notification = new { body = param.Message },
                    data = new { module = param.Module, value = param.Message },
                    to = user.GetUserByID(param.Receiver).FirebaseToken,
                    priority = "high",
                    direct_boot_ok = true
                }));
                webRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = webResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                tReader.ReadToEnd();
                            }
                        }
                    }
                }
                DB.Save(param);
                return ApiResult<object>.Ok("Notification has been sent successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Send notification error : {Format.ExceptionString(e, true)}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

    }
}
