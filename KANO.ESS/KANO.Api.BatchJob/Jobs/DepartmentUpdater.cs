using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KANO.Api.BatchJob.Jobs
{
    public class DepartmentUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        public DepartmentUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occured while running service {e}");
                }

                await Task.Delay(60000, stoppingToken);
            }
        }

        public void Run()
        {
            List<Department> departments = new List<Department>();

            var adapter = new HRAdapter(Configuration);
            //var result = adapter.GetDepartments();
            var result = DB.GetCollection<Department>().Find(x=>true).ToList();
            departments = result;
            Console.WriteLine(DateTime.Now + " [" + departments.Count() + "] " +" Department ");

            //var filterDef = Builders<Department>.Filter.Eq("EmployeeID", "8012160022");
            //var updateDef = Builders<Department>.Update.Set("UpdateName", "blablabla");
            //UpdateOptions updateOpt = new UpdateOptions { IsUpsert = true };
            ////var upt = DB.GetCollection<Department>().UpdateOne(x => x.EmployeeID == "8012160022", updateDef);
            //var udateOne = DB.GetCollection<Department>().UpdateOne(filterDef, updateDef, updateOpt);

            //var empId = departments.Select(x => x.EmployeeID).ToArray();
            //var updateDefinition = Builders<Department>.Update.Set("DepartmentName", departments.Where(c => empId.Contains(c.EmployeeID)).Select(g => g.Name));
            //DB.GetCollection<Department>().UpdateMany(c => empId.Contains(c.EmployeeID), updateDefinition);

            //var updateDefination = new List<UpdateDefinition<Department>>();
            //foreach (var t in departments)
            //{
            //    updateDefination.Add(Builders<Department>.Update.Set("RecId", t.RecId));
            //    updateDefination.Add(Builders<Department>.Update.Set("Name", t.Name));
            //    updateDefination.Add(Builders<Department>.Update.Set("NameAlias", t.NameAlias));
            //    updateDefination.Add(Builders<Department>.Update.Set("OperationUnitType", t.OperationUnitType));
            //    updateDefination.Add(Builders<Department>.Update.Set("OperationUnitNumber", t.OperationUnitNumber));
            //    updateDefination.Add(Builders<Department>.Update.Set("LastUpdate", DateTime.Now));
            //    DB.GetCollection<Department>().FindOneAndUpdate(x => x.EmployeeID == t.EmployeeID, Builders<Department>.Update.Combine(updateDefination));
            //}
            //Console.WriteLine("Department updated ");

            //Department temp = new Department();
            //Department newDepartment = new Department();
            //int i = 0;
            //int recNew = 0;
            //int recUpd = 0;
            //foreach (var a in result)
            //{
            //    temp = DB.GetCollection<Department>().Find(x => x.EmployeeID == a.EmployeeID).FirstOrDefault();
            //    if (temp == null)
            //    {
            //        newDepartment = new Department
            //        {
            //            RecId = a.RecId,
            //            EmployeeID = a.EmployeeID,
            //            EmployeeName = a.EmployeeName,
            //            Name = a.Name,
            //            NameAlias = a.NameAlias,
            //            OperationUnitNumber = a.OperationUnitNumber,
            //            OperationUnitType = a.OperationUnitType,
            //            LastUpdate = DateTime.Now
            //        };
            //        DB.Save(newDepartment);
            //        recNew++;
            //    }
            //    else
            //    {
            //        newDepartment = new Department
            //        {
            //            Id = temp.Id,
            //            RecId = a.RecId,
            //            EmployeeID = a.EmployeeID,
            //            EmployeeName = a.EmployeeName,
            //            Name = a.Name,
            //            NameAlias = a.NameAlias,
            //            OperationUnitNumber = a.OperationUnitNumber,
            //            OperationUnitType = a.OperationUnitType,
            //            LastUpdate = DateTime.Now
            //        };
            //        DB.Save(newDepartment);
            //        recUpd++;
            //    }
            //    i++;
            //}

            //Console.WriteLine(DateTime.Now + " Department updated new [" + recNew + "], update [" + recUpd + "]");
        }
    }
}
