using KANO.Core.Lib.Extension;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using KANO.Core.Service;
using KANO.Core.Lib.Helper;

namespace KANO.Core.Model
{
    [Collection("Travel")]
    [BsonIgnoreExtraElements]
    public class Travel : BaseDocumentVerification, IMongoPreSave<Travel>
    {
        public string TravelID { get; set; }
        public RequestIntention Intention { get; set; }        
        public string IntentionDescription { 
            get {
                return Enum.GetName(typeof(RequestIntention), this.Intention);
            } 
        }
        public string RequestForID { get; set; }
        public string RequestForName { get; set; }
        public bool IsGuest { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateRange Schedule { get; set; }
        public long TravelPurpose { get; set; } //TravelPurpose
        public string TravelPurposeDescription { get; set; } //TravelPurpose
        public long Transportation { get; set; } //Transportation    
        public string TransportationDescription { get; set; } //Transportation    
        public DateTime TransactionDate { get; set; }                
        public Boolean NeedPassportExtension { get; set; }
        public Boolean NeedVisaExtension { get; set; }        
        public string Description { get; set; }
        //public FieldAttachment SPPD { get; set; }
        [BsonIgnore]
        public List<SPPD> SPPD { get; set; }
        
        // update 
        public string CreatedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string CanceledBy { get; set; }
        public DateTime ClosedDate { get; set; }
        public DateTime CanceledDate { get; set; }
        public DateTime VerifiedDate { get; set; }
        public KESSTEServices.KESSTrvExpTravelReqStatus TravelRequestStatus { get; set; }
        public long Transportasi { get; set; }       
        public string Note { get; set; }
        public string NoteRevision { get; set; }
        public string RevisionBy { get; set; }
        public DateTime RevisionDate { get; set; }
        public KESSWRServices.KESSTrvExpFacilityAreaType TravelType { get; set; }
        public string TravelTypeDescription { get {
                return this.TravelType.ToString();
            } 
        }
        public List<FieldAttachment> DocumentList { get; set; }

        public Travel() : base() { }

        public Travel(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public new void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);            
        }              
        

        public List<Travel> GetS(string EmployeeID, DateRange range)
        {
            if (range == null)
            {
                throw new Exception();
            }

            var adapter = new TravelAdapter(this.Configuration);
            return adapter.GetTravel(EmployeeID, range);            
        }      

        public TravelResult GetByTravelID (string EmployeeID, string TravelID, bool essInternalOnly = false)
        {
            var result = new TravelResult();
            var tasks = new List<Task<TaskRequest<Travel>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<Travel>.Create("AX", new Travel());
                }
                var adapter = new TravelAdapter(Configuration);
                var travel = adapter.GetTravelByTravelRequestID(EmployeeID, TravelID);
                return TaskRequest<Travel>.Create("AX", travel);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var family = this.MongoDB.GetCollection<Travel>()
                                .Find(x => x.EmployeeID == EmployeeID && x.AXID == AXID)
                                .FirstOrDefault();
                return TaskRequest<Travel>.Create("DB", family);
            }));

            // Run process concurently
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
                    if (r.Label == "AX")
                        result.Travel = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }

            return result;
        }

        public UpdateRequest CreateUpdateRequest(string instanceID, IMongoDatabase database, IConfiguration configuration)
        {

            this.MongoDB = database;
            this.Configuration = configuration;
            return this.CreateUpdateRequest(instanceID);
        }

        public UpdateRequest CreateUpdateRequest(string instanceID) {
            var updateRequest = MongoDB.GetCollection<UpdateRequest>().Find(x => x.AXRequestID == instanceID).FirstOrDefault();

            if (updateRequest != null) {
                return updateRequest;
            }

            updateRequest = new UpdateRequest();

            var strStartDate = Format.StandarizeDate(this.Schedule.Start);
            var strFinishDate = Format.StandarizeDate(this.Schedule.Finish);
            var strStartDateTime = Format.StandarizeDateTime(this.Schedule.Start);
            var strFinishDateTime = Format.StandarizeDateTime(this.Schedule.Finish);

            var strSchedule = "";
            if (strStartDate == strFinishDate)
            {
                var strStartTime = Format.StandarizeTime(this.Schedule.Start);
                var strFinishTime = Format.StandarizeTime(this.Schedule.Finish);

                if (strStartDateTime == strFinishDateTime)
                {
                    strSchedule = $"{strStartDate} at {strStartTime}";
                }
                else
                {
                    strSchedule = $"{strStartDate} at {strStartTime} - {strFinishTime}";
                }
            }
            else
            {
                strSchedule = $"{strStartDateTime} - {strFinishDateTime}";
            }

            updateRequest.Create(AXRequestID, this.EmployeeID, UpdateRequestModule.TRAVEL_REQUEST, $"SPPD Travel request {strSchedule} ({this.TravelID})");
            updateRequest.AXRequestID = instanceID;
            
            MongoDB.Save(updateRequest);
            return updateRequest;
        }

        public void SatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var travel = MongoDB.GetCollection<Travel>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID).FirstOrDefault();
                    if (travel == null) return TaskRequest<object>.Create("travel", false);

                    var newFilepath = Tools.ArchiveFile(travel.Filepath);
                    var result = MongoDB.GetCollection<Travel>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID ,
                        Builders<Travel>.Update
                            .Set(d => d.Filepath, newFilepath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("travel", result.IsAcknowledged);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update travel data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"SPPD Travel request is {newStatus.ToString()}",
                             Notification.DEFAULT_SENDER,
                            NotificationModule.TRAVEL,  
                            NotificationAction.APPROVE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

        public Travel GetByAXRequestID(string employeeID, string axRequestID, bool withMasterData = false)
        {

            var tasks = new List<Task<TaskRequest<object>>>();
            var adapter = new TravelAdapter(this.Configuration);

            // Fetch data Travel
            tasks.Add(Task.Run(() =>
            {
                var travel = this.MongoDB.GetCollection<Travel>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();

                if (travel == null) {
                    return TaskRequest<object>.Create("Travel", null);
                }
                
                return TaskRequest<object>.Create("Travel", adapter.GetTravelByTravelRequestID(employeeID, travel.TravelID));
            }));

            if (withMasterData)
            {
                // Fetch data Transportation
                tasks.Add(Task.Run(() =>
                {
                    return TaskRequest<object>.Create("Transportation", adapter.GetTransportations());
                }));

                // Fetch data Purpose
                tasks.Add(Task.Run(() =>
                {
                    return TaskRequest<object>.Create("Purpose", adapter.GetTravelPurposes());
                }));
            }

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
                Travel travel = null;
                var transportations = new List<Transportation>();
                var purposes = new List<TravelPurpose>();

                foreach (var r in t.Result)
                {
                    if (r.Label == "Travel")
                    {
                        travel = (Travel)r.Result;
                    }
                    else if (r.Label == "Transportation" && r.Result != null)
                    {
                        transportations = (List<Transportation>)r.Result;
                    }
                    else if (r.Label == "Purpose" && r.Result != null)
                    {
                        purposes = (List<TravelPurpose>)r.Result;
                    }
                }


                if (travel != null && withMasterData) {
                    var transportationMap = new Dictionary<string, string>();
                    transportations.ForEach(tr => {
                        transportationMap[tr.TransportationID] = tr.Description;
                    });

                    var transportation = transportations.Find(x => x.AXID == travel.Transportation);
                    var purpose = purposes.Find(x => x.AXID == travel.TravelPurpose);

                    travel.TransportationDescription = (transportation != null) ? transportation.Description : "";
                    travel.TravelPurposeDescription = (purpose != null) ? purpose.Description : "";

                    travel.SPPD.ForEach(sppd => {
                        sppd.TransportationDetails.ForEach(transportationDetail => {
                            if(!string.IsNullOrWhiteSpace(transportationDetail.TransportationID)) transportationDetail.TransportationDescription= transportationMap[transportationDetail.TransportationID];
                        });
                    });
                }
                return travel;

            }

            return null;
            
        }
        public Travel GetByInstanceID(string EmployeeID, string InstanceID, bool essInternalOnly = false)
        {
            var adapter = new TravelAdapter(Configuration);
            var travel = adapter.GetTravelByInstanceID(EmployeeID, InstanceID);

            return travel;
        }
    }      

    public enum RequestIntention : int 
    { 
        Self = 0,
        Other = 1,
    }

    public class TravelResult
    {
        public Travel Travel { get; set; } = null;
        public Travel UpdateRequest { get; set; } = null;
    }

    public class TravelForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class TravelRequestEmployee
    {
        public string EmployeeID { get; set; }        
        public string As { get; set; }        
    }

    public class TravelPurpose
    {
        public long AXID { get; set; }
        public string PurposeID { get; set; }
        public string Description { get; set; }
        public Boolean IsOverseas {  get; set; }
        public TravelPurpose() { }
        public TravelPurpose(long AXID, string PurposeID, string Description, Boolean IsOverseas)
        {
            this.AXID = AXID;
            this.PurposeID = PurposeID;
            this.IsOverseas = IsOverseas;
            this.Description = Description;
        }
    }

    public class Transportation {
        public long AXID { get; set; }
        public string TransportationID { get; set; }
        public string Description { get; set; }
        
        public Transportation() { }
        public Transportation(long AXID, string TransportationID, string Description)
        {
            this.AXID = AXID;
            this.TransportationID = TransportationID;
            this.Description = Description;
        }
    }

    public enum TravelType : int
    {
        Domestic = 0,
        International = 1
    }




}
