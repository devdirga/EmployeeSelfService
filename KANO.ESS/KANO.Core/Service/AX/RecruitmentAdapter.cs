using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class RecruitmentAdapter  
    {
        private IConfiguration Configuration;
        private Credential credential;

        public RecruitmentAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(Configuration);
        }

        public KESSHRMServices.KESSHRMServiceClient GetClient()
        {
            var Client = new KESSHRMServices.KESSHRMServiceClient();
            var uri = new UriBuilder(Client.Endpoint.Address.Uri);
            uri.Host = credential.Host;
            uri.Port = credential.Port;
            Client.Endpoint.Address = new System.ServiceModel.EndpointAddress(
                uri.Uri,
                new System.ServiceModel.UpnEndpointIdentity(credential.UserPrincipalName));
            Client.ClientCredentials.Windows.ClientCredential.Domain = credential.Domain;
            Client.ClientCredentials.Windows.ClientCredential.UserName = credential.Username;
            Client.ClientCredentials.Windows.ClientCredential.Password = credential.Password;
            return Client;
        }

        public KESSHRMServices.CallContext GetContext()
        {
            var Context = new KESSHRMServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<Recruitment> GetRequests(string employeeID)
        {
            // if (range == null) throw new ArgumentNullException(nameof(range));

            var recruitment = new List<Recruitment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getHRMRecruitmentByReqAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                    
                   recruitment.Add(this.mapFromAX(d));
                }                                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return recruitment;
        }

        public List<Recruitment> GetHistory(string employeeID)
        {
            // if (range == null) throw new ArgumentNullException(nameof(range));

            var recruitment = new List<Recruitment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getHRMRecruitmentByApplicantAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                    
                   recruitment.Add(this.mapFromAX(d));
                }                                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return recruitment;
        }
        
        public List<Recruitment> GetOpenings(string employeeID, DateRange range)
        {
            // if (range == null) throw new ArgumentNullException(nameof(range));

            var recruitment = new List<Recruitment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getHRMOpenRecruitmentAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                    
                   recruitment.Add(this.mapFromAX(d));
                }                                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return recruitment;
        }

        public List<Recruitment> GetS(string employeeID, DateRange range, RecruitmentStatus status = RecruitmentStatus.Scheduled)
        {
            var Recruitment = new List<Recruitment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                //var data = Client.getListClaimReimbursementAsync(Context, employeeID, range.Start, range.Finish).GetAwaiter().GetResult().response;
                //foreach (var d in data)
                //{
                //    benefit.Add(this.mapToAX(d));
                //}

                // Recruitment.Add(new Recruitment { 
                //     RecruitmentID = "RE-00025",
                //     RecruitmentType=RecruitmentType.Job,
                //     RecruitmentStatus=RecruitmentStatus.Scheduled,
                //     RecruiterID="7312020022",
                //     RecruiterName="ABDULLOH",
                //     PositionID=null,
                //     PositionDescription=null,
                //     JobID="JOBS-01",
                //     JobDescription="Finance Administration",
                //     Description="Recruitment for finance staff",
                //     NumberOfOpenings=2,
                //     OpenDate=DateTime.Now.AddMonths(2),
                //     CloseDate=DateTime.Now.AddMonths(4).AddDays(5),
                //     Deadline=DateTime.Now.AddMonths(3).AddDays(-10),
                // });
                

                // Recruitment.Add(new Recruitment { 
                //     RecruitmentID = "RE-00027",
                //     RecruitmentType=RecruitmentType.Position,
                //     RecruitmentStatus=RecruitmentStatus.Scheduled,
                //     RecruiterID="3660303287",
                //     RecruiterName="CHUSNUN CHIDOM",
                //     PositionID=null,
                //     PositionDescription=null,
                //     JobID="",
                //     JobDescription="",
                //     Description="Recruitment for recruitment staff",
                //     NumberOfOpenings=1,
                //     OpenDate=DateTime.Now.AddMonths(-2),
                //     CloseDate=DateTime.Now.AddMonths(-4).AddDays(5),
                //     Deadline=DateTime.Now.AddMonths(-3).AddDays(-10),
                // });

                // Recruitment.Add(new Recruitment { 
                //     RecruitmentID = "RE-00016",
                //     RecruitmentType=RecruitmentType.Position,
                //     RecruitmentStatus=RecruitmentStatus.Scheduled,
                //     RecruiterID="3660303287",
                //     RecruiterName="CHUSNUN CHIDOM",
                //     PositionID="1-00-01-00-01-00-001-00004",
                //     PositionDescription="Office & Supply Management Staff",
                //     JobID=null,
                //     JobDescription=null,
                //     Description="Recruitment for purchasing staff",
                //     NumberOfOpenings=1,
                //     OpenDate=DateTime.Now.AddMonths(-3),
                //     CloseDate=DateTime.Now.AddMonths(-1).AddDays(7),
                //     Deadline=DateTime.Now.AddMonths(-1).AddDays(-10),
                // });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return Recruitment;
        }

        public List<Job> GetJobs()
        {
            var data = new List<Job>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var result = Client.getHRMJobsDetailsAsync(Context).GetAwaiter().GetResult().response;
                foreach (var r in result)
                {                                        
                    data.Add(new Job{ 
                        AXID=r.RecId,
                        Description=r.Description,
                        JobID=r.JobId,
                        TitleID=r.TitleId,
                        JobTypeID=r.JobTypeId,
                        MaxPositions=r.MaxPositions,
                        Objectives=r.PositionObjectives,
                        CompensationLevelID=r.CompensationLevelId,
                    });
                }            
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return data;
        }              

        public List<Position> GetPositions(bool isOpen = false)
        {
            var data = new List<Position>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                KESSHRMServices.KESSHRMPositionDetail[] result;
                if(isOpen){
                    result = Client.getHRMOpenPositionsDetailsAsync(Context).GetAwaiter().GetResult().response;
                }else{
                    result = Client.getHRMPositionsDetailsAsync(Context).GetAwaiter().GetResult().response;
                }
                foreach (var r in result)
                {                    
                    data.Add(new Position{ 
                        AXID=r.RecId,
                        AvailableForAssignment=r.AvailableForAssignment,
                        CompLocationID=r.CompLocationId,
                        Department=r.Department,
                        Description=r.Description,
                        PositionID=r.PositionId,
                        PositionTypeID=r.PositionTypeId,
                        GradePosition=r.GradePosition,
                        JobID=r.JobId,
                        TitleID=r.TitleId,
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return data;
        }                

        public Application GetApplication(string employeeID, string recruitmentID, bool withSchedule=true)
        {
            Application data = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {           
                var result = Client.getHRMHistoryRecruitmentAsync(Context, employeeID, recruitmentID).GetAwaiter().GetResult().response;
                foreach (var r in result) {
                    data = this.mapFromAX(r);

                    if(withSchedule){
                        var schedules = this.GetHistorySchedule(r.ApplicationId);                                        
                        data.ScheduleHistories = new List<ApplicantSchedule>();
                        if(schedules != null) data.ScheduleHistories.AddRange(schedules.OrderBy(x=>x.Date));
                    }
                    break;
                }                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return data;
        }

        public List<ApplicantSchedule> GetHistorySchedule(string applicationID)
        {
            var data = new List<ApplicantSchedule>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {           
                var result = Client.getHRMHistScheduleRecAsync(Context, applicationID).GetAwaiter().GetResult().response;
                foreach(var r in result){
                    var startTime = Helper.secondsToDateTime(r.InterviewDate, r.InterviewTime);
                    var endTime = Helper.secondsToDateTime(r.InterviewDate, r.InterviewEndTime);
                    data.Add(new ApplicantSchedule{
                        ApplicationID=r.ApplicationId,
                        Date=r.InterviewDate,
                        Schedule=new DateRange(startTime, endTime),
                        Location=r.InterviewLocation,
                        ApplicantScheduleStatus=r.InterviewStatus,
                        ApplicantStep= r.ApplicantInterviewStatus,
                        AXID =r.RecId,
                        RecruiterID=r.RecruiterEmplId,
                        RecruiterName=r.RecruiterEmplName
                    });
                }
                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return data;
        }

                        

        private Recruitment mapFromAX(KESSHRMServices.KESSHRMRecruitment data){
            return new Recruitment {                     
                        AXID=data.RecId,
                        Department=data.Department,
                        EstimationStartedDate=data.EstimatedStartedDate,
                        RecruitmentID = data.RecruitingId,
                        RecruitmentType=RecruitmentType.Position,
                        RecruitmentStatus=data.Status,
                        RecruiterID=data.EmplIdRecruiter,
                        PositionID=data.PositionId,
                        // PositionID="1-00-01-00-00-00-000-00001",
                        // PositionDescription="General Staff",
                        JobID=data.JobId,
                        // JobDescription=null,
                        Description=data.Description,
                        NumberOfOpenings=(int)data.RecruitingQty,
                        RequisitionApprovalDate=data.RequisitionApprovalDate,
                        OpenDate=data.OpenDate,
                        CloseDate=data.ClosedDate,
                        Deadline=data.ApplicationDeadlineDate,
                        Filepath= data.DocumentPath,
            };
        }

        private Application mapFromAX(KESSHRMServices.KESSHRMApplicationHistory data){
            return new Application{                     
                        ApplicationID=data.ApplicantId,
                        ExpireDate=data.ApplicationExpireDate,
                        ApplicationStatus=data.ApplicationStatus,
                        ApplicationAction=data.CorrespondanceAction,
                        DateOfReception=data.DateOfReception,
                        Department=data.Department,
                        EmployeeID=data.EmplId,
                        JobID=data.JobId,
                        AXID=data.RecId,
                        RecruitmentID=data.RecruitingId,
                        StartDate= data.StartDateTime,                        
            };
        }
    }
}
