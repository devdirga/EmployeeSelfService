using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KANO.Api.MobilePortal.Controllers
{
    [Route("api/mobileportal/[controller]")]
    [ApiController]
    public class MobileDashboardController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        public MobileDashboardController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpPost("activity/all")]
        public IActionResult GetActivityAll([FromBody] DateRange range)
        {
            var res = new List<ActivityLog>();
            long total = 0;
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                var r = Tools.normalizeFilter(range);

                tasks.Add(Task.Run(() =>
                {

                    res = DB.GetCollection<ActivityLog>()
                    .Find(x =>
                        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                        ||
                        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                     ).SortByDescending(s => s.DateTime).ToList();

                    total = res.Count();

                    return TaskRequest<bool>.Create("ActivityLog", true);
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
            } catch(Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<ActivityLog>>.Ok(res, total);
        }

        [HttpPost("activity/log")]
        public IActionResult GetActivity([FromBody] DateRange range)
        {
            var activity = new List<ActivityLog>();
            var results = new List<DashboardActivity>();
            long total = 0;
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                var r = Tools.normalizeFilter(range);
                // Fetch user data
                tasks.Add(Task.Run(() =>
                {

                    activity = DB.GetCollection<ActivityLog>()
                    .Find(x =>
                        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                        ||
                        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                     ).SortByDescending(s => s.DateTime).ToList();

                    return TaskRequest<bool>.Create("ActivityLog", true);
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

                foreach(var semi in activity)
                {
                    semi.DateTime = semi.DateTime.ToLocalTime();
                };

                var query = activity.GroupBy(i => Tools.normalize(i.DateTime))
                     .Select(i => new
                     {
                         TimeAbsence = i.Key,
                         Total = i.Count()
                     }).ToList();

                //var query = activity.GroupBy(z => z.DateTime.ToShortDateString())
                //    .Select(g => new { TimeAbsence = DateTime.Parse(g.Key), Total = g.Key.Count() });

                //var result = DB.GetCollection<ActivityLog>().Aggregate()
                //    .Match(x =>
                //        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                //        ||
                //        (x.DateTime >= r.Start && x.DateTime <= r.Finish)
                //    )
                //    .Group(
                //        k => new DateTime(k.DateTime.Year, k.DateTime.Month, k.DateTime.Day),
                //        g => new { TimeAbsence = g.Key, Total = g.Count() }
                //    )
                //    .SortByDescending(d => d.TimeAbsence)
                //    .ToList();
                //var query = from p in DB.GetCollection<ActivityLog>().AsQueryable()
                //            where (p.DateTime >= r.Start && p.DateTime <= r.Finish) || (p.DateTime >= r.Start && p.DateTime <= r.Finish)
                //            group p by new DateTime(p.DateTime.Year, p.DateTime.Month, p.DateTime.Day, 0, 0 ,0) into g
                //            orderby g.Key
                //            select new { TimeAbsence = g.Key, Total = g.Count() };
                foreach (var q in query)
                {
                    results.Add(new DashboardActivity
                    {
                        TimeAbsence = q.TimeAbsence,
                        Total = q.Total
                    });
                }
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }
            var res = results;
            return ApiResult<List<DashboardActivity>>.Ok(res, total);
        }

        [HttpPost("activity/log/detail")]
        public IActionResult GetActivityDetail([FromBody]ParamMbuh param)
        {
            var results = new List<DashboardActivityDetail>();
            var activity = new List<ActivityLog>();
            var employee = new List<Employee>();
            var locations = new List<Location>();
            var groupMap = new Dictionary<string, string>();
            long total = 0;

            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                //DateTime currentDayStart = DateTime.Parse(dt);
                //DateTime currentDayEnds = DateTime.Parse(dt).AddDays(1);
                // Fetch user data
                tasks.Add(Task.Run(() =>
                {
                    total = DB.GetCollection<ActivityLog>()
                        .Find(a => a.DateTime > param.temp.Date && a.DateTime <= param.temp.AddDays(1).Date)
                        .ToList().Count();
                    return TaskRequest<bool>.Create("TotalLog", true);
                }));

                tasks.Add(Task.Run(() =>
                {
                    //var r = Tools.normalizeFilter(p);
                    //activity = DB.GetCollection<ActivityLog>().Find(a => a.DateTime > dt && a.DateTime <= dt.AddDays(1)).ToList();
                    activity = DB.GetCollection<ActivityLog>()
                        .Find(a => a.DateTime > param.temp.Date && a.DateTime <= param.temp.AddDays(1).Date)
                        .Limit(param.Take)
                        .Skip(param.Skip)
                        .ToList();

                    return TaskRequest<bool>.Create("ActivityLog", true);
                }));

                tasks.Add(Task.Run(() =>
                {
                    locations = DB.GetCollection<Location>().Find(x => true).ToList();

                    return TaskRequest<bool>.Create("Location", true);
                }));

                tasks.Add(Task.Run(() =>
                {
                    employee = DB.GetCollection<Employee>().Find(x => true).ToList();

                    return TaskRequest<bool>.Create("Employee", true);
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

                var find = new Location();
                var emp = new Employee();
                string locname = "";
                foreach(var nella in activity)
                {
                    find = locations.Find(g => g.Code == nella.LocationID);
                    if (find == null)
                    {
                        locname = "-";
                    } else
                    {
                        locname = find.Name;
                    }

                    emp = employee.Find(g => g.EmployeeID == nella.UserID);
                    if (emp == null)
                    {
                        results.Add(new DashboardActivityDetail
                        {
                            EmployeeID = nella.UserID,
                            FullName = "-",
                            Department = "-",
                            TimeAbsence = nella.DateTime,
                            LocationID = nella.LocationID,
                            LocationName = locname,
                            Longitude = nella.Longitude,
                            Latitude = nella.Longitude
                        });
                    } else
                    {
                        results.Add(new DashboardActivityDetail
                        {
                            EmployeeID = nella.UserID,
                            FullName = emp.EmployeeName,
                            Department = emp.Department,
                            TimeAbsence = nella.DateTime,
                            LocationID = nella.LocationID,
                            LocationName = locname,
                            Longitude = nella.Longitude,
                            Latitude = nella.Longitude
                        });
                    }
                    
                }
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }
            var res = results.OrderByDescending(s => s.TimeAbsence).ToList();
            return ApiResult<List<DashboardActivityDetail>>.Ok(res, total);
        }
    }
}
