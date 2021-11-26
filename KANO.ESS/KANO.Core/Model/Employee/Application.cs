using KANO.Core.Lib.Extension;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Application")]
    [BsonIgnoreExtraElements]
    public class Application:BaseDocumentVerification, IMongoPreSave<Application>
    {
        public DateRange Schedule { get; set; }
        public DateTime StartDate { get; set; }
        public string ApplicationID { get; set; }
        public string PositionID { get; set; }
        public string PositionDescription { get; set; }
        public string Department { get; set; }
        public DateTime ExpireDate { get; set; }        
        public DateTime DateOfReception { get; set; }        
        public string RecruitmentDescription { get; set; }
        public string RecruitmentID { get; set; }
        public string JobID { get; set; }
        public string JobDescription { get; set; }
        public string Venue { get; set; }
        public string RecruitmentCycle { get; set; }
        [BsonIgnore]
        public Recruitment Recruitment { get; set; }
        public KESSHRMServices.HRMApplicationCorrespondanceAction ApplicationAction { get; set; }
        public List<ApplicantSchedule> ScheduleHistories { get; set; } = new List<ApplicantSchedule>();
        public List<ApplicationStatusHistory> StatusHistories { get; set; } = new List<ApplicationStatusHistory>();
        public string ApplicationActionDescription { 
            get {
                return this.ApplicationAction.ToString();
            }
        }
        public KESSHRMServices.HRMApplicationStatus ApplicationStatus { get; set; }
        public string ApplicationStatusDescription { 
            get {
                return this.ApplicationStatus.ToString();
            }
        }

        public Application() : base() { }

        public Application(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);

        }

        public void AddHistory(KESSHRMServices.HRMApplicationStatus status) {
            this.StatusHistories.Add(new ApplicationStatusHistory {
                Id = ObjectId.GenerateNewId().ToString(),
                RecordDate = DateTime.Now,
                Status = status,
            });
        }
    }

    public class ApplicantSchedule{
        public string ApplicationID{get; set;}
        public DateTime Date{get; set;}
        public DateRange Schedule{get; set;}
        public string Location{get; set;}
        public KESSHRMServices.HRMInterviewStatus ApplicantScheduleStatus{get; set;}
        public string ApplicantScheduleStatusDescription{
            get{
                return this.ApplicantScheduleStatus.ToString();
            }
        }
        public KESSHRMServices.KESSApplicantInterviewStatus ApplicantStep { get; set; }
        public string ApplicantStepDescription
        {
            get
            {
                return this.ApplicantStep.ToString();
            }
        }
        public long AXID{get; set;}
        public string RecruiterID{get; set;}
        public string RecruiterName{get; set;}
    }

    public class ApplicationStatusHistory {
        public string Id { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Now;        
        public string Data { get; set; }
        public KESSHRMServices.HRMApplicationStatus Status { get; set; }       
    }   

    public enum ApplicationStatus : int
    {
        Received = 0,
        Confirmed = 1,
        Interview = 2,
        Rejection = 3,
        Withdraw = 4,
        Employed = 5,
        TesAdministrasi = 6,
        TesPotensiAkademik = 7,
        TesPsikologi = 8,
        MedicalCheckUp = 9,
    }

    public class ApplicationForm {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
}
