using KANO.Core.Lib.Extension;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Agenda")]
    [BsonIgnoreExtraElements]
    public class Agenda : BaseT, IMongoPreSave<Agenda>
    {

        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        public string Id { get; set; }
        public string AgendaID { get; set; }
        public string Issuer { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public long AXID { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public string DateDesc { get; set; }
        public string TimeDesc { get; set; }
        public KESSCNTServices.KESSAgendaForType AgendaFor { get; set; }
        public string AgendaForDescription { 
            get {
                return this.AgendaFor.ToString();
            } 
        }
        public AgendaType AgendaType { get; set; } = AgendaType.Common;
        public List<string> EmployeeRecipients { get; set; } = new List<string>();
        public List<FieldAttachment> Attachments { get; set; } = new List<FieldAttachment>();
        public DateRange Schedule { get; set; }
        public string Hash { get; set; }

        public void PreSave(IMongoDatabase db)
        {            
            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }


        public Agenda() { }

        public Agenda(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            MongoDB = mongoDB;
            Configuration = configuration;
        }

        // Aggregate employee agenda
        public List<Agenda> GetS(string employeeID, DateRange dateRange) {
            var result = new List<Agenda>();

            var tasks = new List<Task<TaskRequest<List<Agenda>>>>();

            // Fetch travel agenda
            tasks.Add(Task.Run(() =>
            {
                var travels = new List<Travel>();
                var mapTravelPurposes = new Dictionary<string, TravelPurpose>();
                var adapter = new TravelAdapter(this.Configuration);

                var travelTasks = new List<Task<TaskRequest<object>>>();

                // Get travel purpose
                travelTasks.Add(Task.Run(() =>
                {
                    var travelPurposes = adapter.GetTravelPurposes();
                    foreach (var travelPurpose in travelPurposes) {
                        mapTravelPurposes[travelPurpose.AXID.ToString()] = travelPurpose;
                    }
                    return TaskRequest<object>.Create("travelPurpose", true);
                }));

                // Get travel list
                travelTasks.Add(Task.Run(() =>
                {
                    travels = adapter.GetTravel(employeeID, dateRange);
                    travels.RemoveAll(x => x.TravelRequestStatus != KESSTEServices.KESSTrvExpTravelReqStatus.Verified);
                    return TaskRequest<object>.Create("travel", true);
                }));

                var tTravel = Task.WhenAll(travelTasks);
                try
                {
                    tTravel.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                // Mapping travel with purpose
                var travelAgenda = new List<Agenda>();
                foreach (var travel in travels) {
                    TravelPurpose travelPurpose = null;
                    if (mapTravelPurposes.TryGetValue(travel.TravelPurpose.ToString(), out travelPurpose)) {
                        travel.TravelPurposeDescription = travelPurpose.Description;
                    }

                    if (travel.SPPD.Count() == 0)
                    {
                        continue;
                    }
                    else if(travel.SPPD.First().Status != KESSTEServices.KESSTrvExpSPPDStatus.Approved){
                        continue;
                    }

                    var agenda = new Agenda
                    {
                        AgendaType = AgendaType.Travel,
                        Issuer = travel.TravelPurposeDescription,
                        Name = $"From {travel.Origin?.ToUpper()} <i class=\"mdi mdi-arrow-right\"></i> To {travel.Destination?.ToUpper()}",
                        Description = travel.Description,
                        Schedule = travel.Schedule, 
                        AgendaFor = KESSCNTServices.KESSAgendaForType.Employee,
                    };

                    foreach (var sppd in travel.SPPD) {
                        foreach (var attachment in sppd.Attachments) {
                            agenda.Attachments.Add(attachment);
                        }
                    }

                    travelAgenda.Add(agenda);

                }

                return TaskRequest<List<Agenda>>.Create("travelAgenda", travelAgenda);
            }));

            // Fetch common agenda
            tasks.Add(Task.Run(() => {
                var adapter = new ComplaintAdapter(this.Configuration);
                var agenda = adapter.GetAgenda(dateRange, employeeID);
                return TaskRequest<List<Agenda>>.Create("agenda", agenda);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    result.AddRange(r.Result);                                
            }

            return result.OrderByDescending(x => x.Schedule.Start).ToList();
        }

        public List<Agenda> GetRangeMobile(string employeeID, DateRange dateRange)
        {
            var result = new List<Agenda>();
            var tasks = new List<Task<TaskRequest<List<Agenda>>>>
            {
                Task.Run(() =>
                {
                    return TaskRequest<List<Agenda>>.Create("agenda",
                        new ComplaintAdapter(this.Configuration).GetAgendaMobile(employeeID, dateRange));
                })
            };

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            if (t.Status == TaskStatus.RanToCompletion)
                foreach (var r in t.Result)
                    result.AddRange(r.Result);

            return result.OrderByDescending(x => x.Schedule.Start).ToList();
        }
    }

    public enum AgendaType : int
    {
        Common = 1,
        Travel = 2,
    }


}
