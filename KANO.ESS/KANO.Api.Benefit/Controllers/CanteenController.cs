using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using KANO.Core.Service.AX;
using System.IO;

namespace KANO.Api.Benefit.Controllers
{
    [Route("api/benefit/[controller]")]
    [ApiController]
    public class CanteenController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public CanteenController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpGet("get")]
        public IActionResult GetCanteenAll()
        {
            var result = new List<Canteen>();
            try
            {
                result = DB.GetCollection<Canteen>().Find(x => x.Deleted == false).ToList();
                return ApiResult<List<Canteen>>.Ok(result);
            } catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("get/user/{canteenID}")]
        public IActionResult GetCanteenUser(string canteenID)
        {
            try
            {
                var result = DB.GetCollection<Canteen>().Find(x => x.Id == canteenID).FirstOrDefault();
                if(result== null) throw new Exception($"Unable to find canteen with id : {canteenID}");

                var user = DB.GetCollection<User>().Find(x=>x.Id==result.UserID).FirstOrDefault();
                if(user== null) throw new Exception($"Unable to find user for canteen id : {canteenID}");

                var roleID = user.Roles.FirstOrDefault();
                if(!string.IsNullOrWhiteSpace(roleID)){
                    var role = DB.GetCollection<Group>().Find(x=>x.Id == roleID).FirstOrDefault();
                    if(role != null) user.RoleDescription = role.Name;
                }
                
                return ApiResult<User>.Ok(user);
            } catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("redeem/history/employee/{employeeID}")]
        public IActionResult GetRedeemHistoryEmployee(string employeeID)
        {
            var result = new List<Redeem>();
            try
            {
                //result = DB.GetCollection<Redeem>().Find(x => x.EmployeeID == empId).Sort("{ RedeemedAt: 1}").Limit(5).ToList();
                result = DB.GetCollection<Redeem>().Find(x => x.EmployeeID == employeeID).SortByDescending(e => e.RedeemedAt).ToList();
                return ApiResult<List<Redeem>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("redeem/history")]
        public IActionResult GetRedeemHistoryCanteen([FromBody] GridDateRange param)
        {
            var range = Tools.normalizeFilter(param.Range);
            var result = new List<Redeem>();
            try
            {
                var start = range.Start;
                var finish = range.Finish;
                
                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == param.Username).FirstOrDefault();
                if (canteen != null) {
                    result = DB.GetCollection<Redeem>().Find(x => x.CanteenID == canteen.Id && x.RedeemedAt >= start && x.RedeemedAt <= finish).SortByDescending(e => e.RedeemedAt).ToList();
                    return ApiResult<List<Redeem>>.Ok(result);
                }

                throw new Exception($"Unable to find canteen with user id {param.Username}");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("save")]
        public IActionResult SaveCanteen([FromForm] CanteenForm param)
        {
            var data = JsonConvert.DeserializeObject<Canteen>(param.JsonData);
            try
            {

                data.Upload(Configuration, null, param.FileUpload, x => String.Format("Canteen_{0}_{1}", data.CreatedDate, x.Id));                

                var result = "";
                var message = $"Canten has been saved successfully.";
                if (string.IsNullOrWhiteSpace(data.Id))
                {
                    if (data.CanteenUser == CanteenUser.NewUser)
                    {
                        if (data.User == null)
                        {
                            throw new Exception("Unable to find user information to create new canteen user");
                        }

                        if (string.IsNullOrWhiteSpace(data.User.Email))
                        {
                            throw new Exception("Unable to find user email data to create new canteen user");
                        }

                        if (string.IsNullOrWhiteSpace(data.User.Username))
                        {
                            throw new Exception("Unable to find username data to create new canteen user");
                        }
                        data.User.Email = data.User.Email.Trim().ToLower();

                        var user = DB.GetCollection<User>().Find(x => x.Email == data.User.Email.Trim().ToLower() || x.Username == data.User.Username.Trim().ToLower()).FirstOrDefault();
                        if (user != null)
                        {
                            if (user.Email == data.User.Email.Trim().ToLower())
                            {
                                throw new Exception("Email has been registered. Please choose different email.");
                            }

                            if (user.Username == data.User.Username.Trim().ToLower())
                            {
                                throw new Exception("Username has been registered. Please choose different username.");
                            }
                        }

                        Random random = new Random();
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                        var generatedPassword = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());

                        var newUser = new User
                        {
                            Email = data.User.Email,
                            Username = data.User.Username,
                            FullName = data.Name,
                            NewPassword = generatedPassword,
                            Roles = data.User.Roles,
                        };

                        newUser.UserData["gender"] = "";
                        newUser.UserData["hasSubordinate"] = "false";
                        newUser.Enable = true;
                        message += $" Merchant can log in using {newUser.Username} with credential {generatedPassword}.";
                        newUser.Enable = true;
                        DB.Save(newUser);

                        result = generatedPassword;
                        data.UserID = newUser.Id;
                    }
                    else
                    {
                        var user = DB.GetCollection<User>().Find(x => x.Id == data.UserID).FirstOrDefault();
                        if (user != null)
                        {
                            data.UserID = user.Id;
                        }
                        else
                        {
                            throw new Exception($"Unable to find user with id : {data.UserID}");
                        }
                    }
                }

                DB.Save(data);
                return ApiResult<object>.Ok(result, message);
            } catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving canteen :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("delete/{canteenID}")]
        public IActionResult DeleteCanteen(string canteenID)
        {
            try
            {
                var canteen = DB.GetCollection<Canteen>().Find(x => x.Id == canteenID).FirstOrDefault();
                if (canteen == null) {
                    throw new Exception($"Unable to find canteen with id {canteenID}");
                }

                DB.GetCollection<User>()
                    .FindOneAndDelete(x => x.Id == canteen.UserID);
                canteen.Deleted = true;

                DB.Save(canteen);

                return ApiResult<object>.Ok($"Canten has been deleted successfully");
            } catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error deleting canteen :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("redeem")]
        public IActionResult SaveRedeem([FromBody]Redeem param)
        {
            try
            {                
                var listId = DB.GetCollection<Voucher>().Find(z => z.EmployeeID == param.EmployeeID && z.ExpiredDate >= DateTime.Now && z.Used == false).SortBy(s => s.ExpiredDate).Project(x=>x.Id).Limit(param.RedeemedVoucherTotal).ToList();
                if (listId.Count < param.RedeemedVoucherTotal) {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"You have exceeded voucher limit. You only have {listId.Count} valid voucher");
                }
                
                var updateDefinition = Builders<Voucher>.Update
                    .Set(x => x.Used, true)
                    .Set(x => x.CanteenID, param.CanteenID)
                    .Set(y => y.UsedDate, DateTime.Now);
                var u = DB.GetCollection<Voucher>("Voucher").UpdateMany(x=> listId.Contains(x.Id), updateDefinition);

                DB.Save(param);

                var canteen = DB.GetCollection<Canteen>().Find(x => x.Id == param.CanteenID).FirstOrDefault();
                if (canteen != null) {
                    var user = DB.GetCollection<User>().Find(x => x.Id == canteen.UserID).FirstOrDefault();
                    if (user != null) {                        
                        new Notification(Configuration, DB).Create(
                            user.Username,
                            $"{param.EmployeeName} redeemed {listId.Count} voucher(s)",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.CANTEEN,
                            NotificationAction.NONE,
                            NotificationType.Success
                        ).Send();
                    }
                }

                return ApiResult<object>.Ok($"You have redeemed {listId.Count} voucher(s) successfully");
            } catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving order use voucher :\n{Format.ExceptionString(e)}");
            }
        }
        
        [HttpGet("voucher/create/{employeeID}")]
        public IActionResult CreateVoucher(string employeeID) {
            return this.CreateVoucher(employeeID, "");
        }

        [HttpGet("voucher/create/{employeeID}/{date}")]
        public IActionResult CreateVoucher(string employeeID, string date)
        {            
            try
            {
                var employee = DB.GetCollection<User>().Find(z => z.Id == employeeID).FirstOrDefault();
                //var adapter = new EmployeeAdapter(Configuration);
                //var employee = adapter.Get(employeeID);
                if (employee == null) {
                    throw new Exception($"Unable to find employee '{employeeID}'");
                }

                var generatedDateFor = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(date)) {
                    try
                    {
                        generatedDateFor = DateTime.ParseExact(date, "dd-MM-yyyy", null);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("format date should be dd-MM-YYY", e);
                    }
                    
                }

                var voucher = new Voucher {
                    EmployeeID = employeeID,
                    EmployeeName = employee.FullName,                    
                    //EmployeeName = employee.EmployeeName,                    
                    GeneratedForDate = generatedDateFor,
                };
                
                DB.Save(voucher);
                return ApiResult<object>.Ok($"Create voucher has been saved successfully");
            } catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error when create voucher :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("voucher/info/{employeeID}")]
        public IActionResult GetVoucher(string employeeID)
        {
            try
            {
                var now = DateTime.Now;
                var nowDate = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, now.Day), DateTimeKind.Utc);
                var tasks = new List<Task<TaskRequest<long>>>();

                tasks.Add(Task.Run(() =>
                {
                    var monthsAgo = DateTime.Now.AddMonths(-1);
                    var totalRem = DB.GetCollection<Voucher>().CountDocuments(z => z.ExpiredDate >= nowDate && z.Used == false && z.EmployeeID == employeeID);
                    return TaskRequest<long>.Create("remaining", totalRem);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var totalUsed = DB.GetCollection<Voucher>().CountDocuments(z => z.Used == true && z.EmployeeID == employeeID);
                    return TaskRequest<long>.Create("used", totalUsed);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var aWeekBeforeExpired = (new DateTime(now.Year, now.Month,now.Day, 23, 59, 59)).AddDays(7);                    
                    var totalAlmost = DB.GetCollection<Voucher>().CountDocuments(z => z.ExpiredDate >= nowDate && z.ExpiredDate < aWeekBeforeExpired  && z.Used == false && z.EmployeeID == employeeID);                    
                    return TaskRequest<long>.Create("almost", totalAlmost);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var totalExp = DB.GetCollection<Voucher>().CountDocuments(z => z.ExpiredDate < nowDate && z.Used == false && z.EmployeeID == employeeID);
                    return TaskRequest<long>.Create("expired", totalExp);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var aWeekBeforeExpired = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59)).AddDays(7);
                    var totalAlmost = DB.GetCollection<Voucher>().CountDocuments(z => z.ExpiredDate >= nowDate && z.ExpiredDate < aWeekBeforeExpired && z.Used == false && z.EmployeeID == employeeID);
                    return TaskRequest<long>.Create("almost", totalAlmost);
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

                var result = new VoucherInfo();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                    {
                        if (r.Label == "remaining")
                        {
                            result.VoucherRemaining = r.Result;
                        }
                        else if (r.Label == "used")
                        {
                            result.VoucherUsed = r.Result;
                        }
                        else if (r.Label == "expired")
                        {
                            result.VoucherExpired = r.Result;
                        }
                        else
                        {
                            result.VoucherAlmostExpired = r.Result;
                        }
                    }

                }

                return ApiResult<VoucherInfo>.Ok(result);
            } catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while get voucher information:\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("image/{canteenID}")]
        public IActionResult Image(string canteenID)
        {

            try
            {
                var canteen = DB.GetCollection<Canteen>().Find(x => x.Id == canteenID).FirstOrDefault();
                if (canteen != null && !string.IsNullOrWhiteSpace(canteen.Filepath)) {
                    var bytes = canteen.Download();
                    var filepath = Path.GetFileName(canteen.Filepath);
                    return File(bytes, "application/force-download", filepath);
                }
                var canteenName = canteen == null ? canteenID : canteen.Name;
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get image for canteen {canteenName}");

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to canteen image :\n{Format.ExceptionString(e)}");
            }
        }
        
        [HttpGet("claim/info/{username}")]
        public IActionResult GetClaimInfo( string username)
        {
            try
            {
                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == username).FirstOrDefault();
                if (canteen != null)
                {
                    var tasks = new List<Task<TaskRequest<long>>>();
                    tasks.Add(Task.Run(() =>
                    {
                        var monthsAgo = DateTime.Now.AddMonths(-1);
                        var totalRem = DB.GetCollection<ClaimCanteen>().CountDocuments(x => x.CanteenUserID == canteen.UserID && x.Status == RedeemStatus.Claim);
                        return TaskRequest<long>.Create("claimed", totalRem);
                    }));

                    tasks.Add(Task.Run(() =>
                    {
                        var totalUsed = DB.GetCollection<ClaimCanteen>().CountDocuments(x => x.CanteenUserID == canteen.UserID && x.Status == RedeemStatus.Paid);
                        return TaskRequest<long>.Create("paid", totalUsed);
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

                    var result = new ClaimCanteenInfo();
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        foreach (var r in t.Result)
                        {
                            if (r.Label == "claimed")
                            {
                                result.TotalClaimed = r.Result;
                            }
                            else if (r.Label == "paid")
                            {
                                result.TotalPaid = r.Result;
                            }
                        }

                    }

                    return ApiResult<ClaimCanteenInfo>.Ok(result);
                    
                }
                throw new Exception($"Unable to find canteen with user id {username}");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("claim/history")]
        public async Task<IActionResult> GetHistoryClaim([FromBody] GridDateRange param)
        {
            var range = Tools.normalizeFilter(param.Range);
            var result = new List<ClaimCanteen>();
            try
            {
                var start = range.Start;
                var finish = range.Finish;

                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == param.Username).FirstOrDefault();
                if (canteen != null)
                {
                    //result = DB.GetCollection<ClaimCanteen>().Find(f => f.CanteenId == canteen.Username).SortBy(s => s.ClaimDate).ToList();
                    result = DB.GetCollection<ClaimCanteen>().Find(x => x.CanteenUserID == canteen.UserID && (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish)).SortByDescending(e => e.ClaimDate).ToList();

                    return ApiResult<List<ClaimCanteen>>.Ok(result);
                }
                throw new Exception($"Unable to find canteen with user id {param.Username}");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpPost("claim")]
        public async Task<IActionResult> GetClaimCanteen([FromBody] GridDateRange param)
        {
            var range = Tools.normalizeFilter(param.Range);
            var result = new List<Redeem>();
            var warkop = new List<RedeemGroup>();
            try
            {
                var start = range.Start;
                var finish = range.Finish;
                
                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == param.Username).FirstOrDefault();
                if (canteen != null)
                {
                    result = DB.GetCollection<Redeem>().Find(x => x.CanteenID == canteen.Id && ((int)x.Status < 1 || x.Status == null)  && (x.RedeemedAt >= start && x.RedeemedAt <= finish)).SortBy(e => e.RedeemedAt).ToList();
                    
                    var queryGroup =
                            from student in result
                            group student by student.RedeemedAt.ToString("yyyy-MM-dd") into newGroup
                            orderby newGroup.Key
                            select newGroup;
                    
                    foreach(var xx in queryGroup.ToList())
                    {
                        warkop.Add(new RedeemGroup
                        {
                            RedeemDate = xx.Key,
                            SubTotal = xx.Sum(x=>x.RedeemedVoucherTotal)
                        });
                        /*
                        foreach (var student in xx)
                        {
                            Console.WriteLine($"\t{student.CanteenName}, {student.RedeemedAt}");
                        }
                        */
                    }
                    /*
                    // Simple groupby
                    List<RedeemGroup> warkop = result
                        .GroupBy(l => l.RedeemedAt.ToString("yyyy-MM-dd"))
                        .Select(cl => new RedeemGroup
                        {
                            RedeemDate = cl.Key,
                            TotalByDate = cl.Sum(s=>s.RedeemedVoucherTotal)
                        }).ToList();
                    */
                    /*
                    // Linq sum groupby use collection as object anonymous (Josss)
                    var teamTotalScores =
                        from player in result
                        group player by player.RedeemedAt.ToString("yyyy-MM-dd") into playerGroup
                        select new
                        {
                            RedeemDate = playerGroup.Key,
                            Total = playerGroup.Sum(x => x.RedeemedVoucherTotal),
                        };
                    return ApiResult<object>.Ok(warkop);
                    */
                    return ApiResult<List<RedeemGroup>>.Ok(warkop);
                }
                throw new Exception($"Unable to find canteen with user id {param.Username}");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");

            }
        }

        [HttpPost("claim/save")]
        public async Task<IActionResult> SaveClaimCanteen([FromBody] ClaimCanteen param)
        {
            try
            {
                param.Status = RedeemStatus.Claim;
                DB.Save(param);
                
                var arr = new List<DateTime>();
                param.DataRedeem.ForEach(xx => {
                    arr.Add(DateTime.Parse(xx.RedeemDate));
                });

                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == param.CanteenUserID).FirstOrDefault();

                var rc = DB.GetCollection<Redeem>().Find(f => f.CanteenID == canteen.Id).Project(x => x.RedeemedAt).ToList();
                var ls = rc.Where(o => arr.Any(s => o.ToString("yyyy-MM-dd").Contains(s.ToString("yyyy-MM-dd")))).ToList();
                //var rc = DB.GetCollection<Redeem>().Find(f => f.CanteenID == canteen.Id).Project(x => x.RedeemedAt.ToString("yyyy-MM-dd")).ToList();
                //var ls = rc.Where(o => arr.Any(s => o.ToString().Contains(s.ToString()))).ToList();
                var updateDefinition = Builders<Redeem>.Update
                    .Set(w => w.ClaimId, param.Id)
                    .Set(x => x.ClaimDate, DateTime.Now)
                    .Set(y => y.Status, RedeemStatus.Claim);

                var u = DB.GetCollection<Redeem>("Redeem").UpdateMany(x => ls.Contains(x.RedeemedAt) && x.CanteenID == canteen.Id, updateDefinition);
                return ApiResult<object>.Ok($"Claim has been saved successfully");
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while save data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("history/paymentclaim")]
        public async Task<IActionResult> HistoryPaymentClaim([FromBody] GridDateRange param)
        {
            var range = Tools.normalizeFilter(param.Range);
            var result = new List<ClaimCanteen>();
            try
            {
                var start = range.Start;
                var finish = range.Finish;

                if (param.CanteenUserID == null)
                {
                    result = DB.GetCollection<ClaimCanteen>().Find(x => (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish)).SortByDescending(e => e.ClaimDate).ToList();

                }
                else
                {
                    result = DB.GetCollection<ClaimCanteen>().Find(x => x.CanteenUserID == param.CanteenUserID && (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish)).SortByDescending(e => e.ClaimDate).ToList();
                }

                return ApiResult<List<ClaimCanteen>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("payment/save")]
        public async Task<IActionResult> SavePaymentCanteen([FromBody] List<ClaimCanteen> param)
        {
            try
            {
                var dataPaid = new PaymentClaim();
                dataPaid.DatePaid = DateTime.Now;
                dataPaid.DataClaim = param;
                DB.Save(dataPaid);

                List<string> ls = new List<string>();
                param.ForEach(xx => {
                    ls.Add(xx.Id);
                });

                var updateDefinition = Builders<ClaimCanteen>.Update
                    .Set(w => w.PaymentId,dataPaid.Id)
                    .Set(x => x.DatePaid, DateTime.Now)
                    .Set(y => y.Status, RedeemStatus.Paid);
                DB.GetCollection<ClaimCanteen>("ClaimCanteen").UpdateMany(x => ls.Contains(x.Id), updateDefinition);

                var updateDefinition2 = Builders<Redeem>.Update
                    .Set(w => w.PaymentId, dataPaid.Id)
                    .Set(x => x.DatePaid, DateTime.Now)
                    .Set(y => y.Status, RedeemStatus.Paid);
                DB.GetCollection<Redeem>("Redeem").UpdateMany(x => ls.Contains(x.ClaimId), updateDefinition2);
                return ApiResult<object>.Ok($"Payment claim has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while save data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("redeem/history/all")]
        public IActionResult GetHistoryRedeem([FromBody] GridDateRange param)
        {
            var result = new List<Redeem>();
            try
            {
                var start = param.Range.Start;
                var finish = param.Range.Finish;

                if (start > finish)
                {
                    var t = start;
                    start = t;
                    finish = start;
                }

                var canteen = DB.GetCollection<Canteen>().Find(x => x.UserID == param.CanteenUserID).FirstOrDefault();

                var total = 0;
                if (param.Status == (int)RedeemStatus.Claim)
                {
                    if (param.CanteenUserID == null)
                    {
                        total = (int)DB.GetCollection<Redeem>().CountDocuments(x => (int)x.Status == (int)param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish));
                        result = DB.GetCollection<Redeem>().Find(x => (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish)).Skip(param.Skip).Limit(param.PageSize).SortBy(e => e.RedeemedAt).ToList();

                    }
                    else
                    {
                        total = (int)DB.GetCollection<Redeem>().CountDocuments(x => x.CanteenID == canteen.Id && (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish));
                        result = DB.GetCollection<Redeem>().Find(x => x.CanteenID == canteen.Id && (int)x.Status == param.Status && (x.ClaimDate >= start && x.ClaimDate <= finish)).Skip(param.Skip).Limit(param.PageSize).SortBy(e => e.RedeemedAt).ToList();
                    }
                } else if (param.Status == (int)RedeemStatus.Paid)
                {
                    if (param.CanteenUserID == null)
                    {
                        total = (int)DB.GetCollection<Redeem>().CountDocuments(x => (int)x.Status == (int)param.Status && (x.DatePaid >= start && x.DatePaid <= finish));
                        result = DB.GetCollection<Redeem>().Find(x => (int)x.Status == param.Status && (x.DatePaid >= start && x.DatePaid <= finish)).Skip(param.Skip).Limit(param.PageSize).SortBy(e => e.RedeemedAt).ToList();

                    }
                    else
                    {
                        total = (int)DB.GetCollection<Redeem>().CountDocuments(x => x.CanteenID == canteen.Id && (int)x.Status == param.Status && (x.DatePaid >= start && x.DatePaid <= finish));
                        result = DB.GetCollection<Redeem>().Find(x => x.CanteenID == canteen.Id && (int)x.Status == param.Status && (x.DatePaid >= start && x.DatePaid <= finish)).Skip(param.Skip).Limit(param.PageSize).SortBy(e => e.RedeemedAt).ToList();
                    }
                }
                

                return ApiResult<List<Redeem>>.Ok(result, total);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("voucher/request/{employeeID}")]
        public IActionResult RequestVoucher(string employeeID)
        {
            var result = new List<Voucher>();
            try
            {
                if (string.IsNullOrWhiteSpace(employeeID))
                    throw new Exception("employee id param is null or empty");

                var employeeAdapter = new EmployeeAdapter(Configuration);
                var employee = employeeAdapter.Get(employeeID);
                if (employee.WorkerTimeType != "NS")
                    throw new Exception("voucher is generated for Non-Shift employee only");

                DateTime currentDate = DateTime.Now;
                DateTime firstDate = currentDate.AddDays(-30);
                
                var user = DB.GetCollection<User>().Find(z=>z.Id == employeeID).FirstOrDefault();

                var noVoucher = new List<NoVoucher>();
                result = DB.GetCollection<Voucher>().Find(x => x.EmployeeID == employeeID && (x.GeneratedForDate >= firstDate && x.GeneratedForDate <= currentDate)).ToList();
                var attendance = GetAttendances(employeeID);
                attendance.ForEach(dt =>
                {
                    var tt = result.Find(f => f.GeneratedForDate == dt.LoggedDate);
                    if (dt.AbsenceCode == "H")
                    {
                        if (tt == null)
                        {
                            noVoucher.Add(new NoVoucher
                            {
                                EventDate = dt.LoggedDate,
                                ActionOnEvent = ActionVoucher.GenerateVoucher,
                            });
                        }                        
                    }
                    else
                    {
                        if (tt == null)
                        {
                            noVoucher.Add(new NoVoucher
                            {
                                EventDate = dt.LoggedDate,
                                ActionOnEvent = ActionVoucher.RequestVoucher,
                            });
                        }
                    }
                });

                VoucherRequest voucherRequest = new VoucherRequest();
                VoucherRequestDetail voucherRequestDetail = new VoucherRequestDetail();
                if (noVoucher.Where(p => p.ActionOnEvent == ActionVoucher.RequestVoucher).Count() > 0)
                {
                    voucherRequest = new VoucherRequest
                    {
                        EmployeeID = employeeID,
                        EmployeeName = user.FullName,
                    };
                    DB.Save(voucherRequest);
                }

                var workflowAdapter = new WorkFlowRequestAdapter(this.Configuration);                
                noVoucher.ForEach(nn => {
                    if (nn.ActionOnEvent == ActionVoucher.GenerateVoucher)
                    {
                        try
                        {
                            var voucherCreated = new Voucher(DB, Configuration);
                            voucherCreated.EmployeeName = user.FullName;
                            voucherCreated.EmployeeID = employeeID;
                            voucherCreated.GeneratedForDate = nn.EventDate;                            
                            DB.Save(voucherCreated);
                        }
                        catch
                        {
                        }
                    } else if (nn.ActionOnEvent == ActionVoucher.RequestVoucher)
                    {
                        try
                        {
                            var ck = DB.GetCollection<VoucherRequestDetail>().CountDocuments(c => 
                                c.EmployeeID == employeeID && 
                                c.GeneratedForDate == nn.EventDate && 
                                (c.Status == UpdateRequestStatus.InReview));

                            if (ck == 0)
                            {
                                var updateRequest = new UpdateRequest();
                                voucherRequestDetail = new VoucherRequestDetail
                                {
                                    VoucherRequestID = voucherRequest.Id,
                                    EmployeeID = employeeID,
                                    EmployeeName = user.FullName,
                                    GeneratedForDate = nn.EventDate,
                                };
                                var reqToAx = workflowAdapter.RequestCanteenVoucher(voucherRequest, voucherRequestDetail);
                                voucherRequestDetail.AXRequestID = reqToAx;
                                if (string.IsNullOrEmpty(reqToAx))
                                    throw new Exception();

                                updateRequest.Create(reqToAx, employeeID, UpdateRequestModule.CANTEEN_VOUCHER, $"Canteen voucher for {nn.EventDate.ToString("dddd, dd-MM-yyyy")}");

                                DB.Save(updateRequest);
                                DB.Save(voucherRequestDetail);   
                            }
                                                     
                        } catch(Exception e)
                        {}
                        
                    }
                });
                //DB.GetCollection<Voucher>().InsertMany(createdVoucher);
                return ApiResult<List<NoVoucher>>.Ok(noVoucher);
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }

        public List<Absent> GetAttendances(string employeeID)
        {
            DateTime currentDate = DateTime.Now;
            DateTime firstDate = currentDate.AddDays(-30);
            DateRange range = new DateRange();
            range.Start = firstDate;
            range.Finish = currentDate;

            //range.Start = new DateTime(2019, 12, 16);
            //range.Finish = new DateTime(2019, 12, 18);

            var adapter = new TimeManagementAdapter(this.Configuration);
            var timeAttendances = adapter.Get(employeeID, range).ToList();

            // FTP
            /*
            var timeAttendances = new List<TimeAttendance>();
            timeAttendances = new List<TimeAttendance>{
                new TimeAttendance {
                    EmployeeID = employeeID,
                    LoggedDate = new DateTime(2020,4,23),
                    AbsenceCode = "H",
                },
                new TimeAttendance {
                    EmployeeID = employeeID,
                    LoggedDate = new DateTime(2020,4,22),
                    AbsenceCode = "O",
                },
                new TimeAttendance {
                    EmployeeID = employeeID,
                    LoggedDate = new DateTime(2020,4,21),
                    AbsenceCode = "H",
                }
            };
            */
            List<Absent> absent = new List<Absent>();
            timeAttendances.ForEach(a=> {
                absent.Add(new Absent {
                    EmployeeID = a.EmployeeID,
                    LoggedDate = a.LoggedDate,
                    AbsenceCode = a.AbsenceCode
                });
            });
            return absent;
        }

        [HttpPost("saveprofile")]
        public IActionResult SaveCanteenProfile([FromForm] CanteenForm param)
        {
            Canteen canteen = JsonConvert.DeserializeObject<Canteen>(param.JsonData);
            try
            {
                canteen.Upload(Configuration, null, param.FileUpload, x => String.Format("Canteen_{0}_{1}", canteen.CreatedDate, x.Id));
                DB.Save(canteen);
                return ApiResult<object>.Ok(canteen, "Canten has been saved successfully.");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving canteen :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("getprofile")]
        public IActionResult GetCanteenProfile([FromBody] Canteen param)
        {
            var result = new List<Canteen>();
            try
            {
                result = DB.GetCollection<Canteen>().Find(x => x.Deleted == false && x.UserID == param.UserID).ToList();
                return ApiResult<List<Canteen>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading data :\n{Format.ExceptionString(e)}");
            }
        }
    }
}