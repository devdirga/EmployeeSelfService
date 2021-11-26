using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class LeaveAdapter
    {
        private IConfiguration Configuration;       
        private Credential credential;       

        public LeaveAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(Configuration);
        }

        public KESSLMServices.KESSLMServiceClient GetClient() {           
            var Client = new KESSLMServices.KESSLMServiceClient();            
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

        public KESSLMServices.CallContext GetContext() {
            var Context = new KESSLMServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<Leave> Get(string employeeID)
        {
            var leaves = new List<Leave>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveEmployeeAsync(Context, employeeID);
                var x = data.Result.response;
                foreach (var d in x)
                {      
                    leaves.Add(new Leave
                    {
                        AXID= d.RecId,                       
                        Status = (UpdateRequestStatus)d.ApprovalStatus,
                        Description = d.Description,
                        EmployeeID = d.EmplId,
                        Schedule = new DateRange(d.StartDate, d.EndDate),
                        Type = d.LeaveType.ToString(),
                        SubtituteEmployeeID = d.SubtituteEmplId,
                        
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
            
            return leaves;
        }        

        public List<LeaveMaintenance> GetMaintenance(string employeeID)
        {
            var leaves = new List<LeaveMaintenance>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveMaintenanceAsync(Context, employeeID);
                foreach (var d in data.Result.response)
                {
                    leaves.Add(new LeaveMaintenance
                    {
                        Available = d.Available == KESSLMServices.NoYes.Yes,
                        AvailabilitySchedule = new DateRange(d.AvailableStartDate, d.AvailableEndDate),
                        CFexpiredDate = d.CFExpiredDate,
                        EmployeeID = d.EmplId,
                        IsClosed = d.IsClosed == KESSLMServices.NoYes.Yes,
                        CF = d.LeaveCF,
                        Description = d.LeaveDescription,
                        Remainder = d.LeaveRemainder,
                        Rights = d.LeaveRights,
                        Year = d.LeaveYear,
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
                                             
            return leaves;
        }

        public List<LeaveType> GetLeaveType(string employeeID)
        {
            var leaveType = new List<LeaveType>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveTypeAsync(Context, employeeID);
                foreach (var d in data.Result.response)
                {                    
                    leaveType.Add(new LeaveType
                    {
                        CategoryId = d.CategoryId,
                        TypeId = d.TypeId,
                        Description = d.Description,
                        EffectiveDateFrom = d.EffectiveDateFrom,
                        EffectiveDateTo = d.EffectiveDateTo,
                        IsClosed = d.IsClosed == KESSLMServices.NoYes.Yes,
                        MaxDayLeave = d.MaxNumberOfLeave,
                        Remainder = d.LeaveRemainder,
                        ConsumeDay = d.ConsumeDay
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
            
            return leaveType;
        }

        public List<LeaveSubordinate> GetLeaveSubordinate(string empId)
        {
            List<LeaveSubordinate> leaveSubordinates = new List<LeaveSubordinate>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveEmplSubordinateAsync(Context, empId);
                foreach (var d in data.Result.response)
                {
                    leaveSubordinates.Add(new LeaveSubordinate
                    {
                        Status = (UpdateRequestStatus)d.ApprovalStatus,
                        Description = d.Description,
                        EmplId = d.EmplId,
                        EmplName = d.EmplName,
                        RecId = d.RecId,
                        ReportToEmplId = d.ReportToEmplId,
                        ReportToEmplName = d.ReportToEmplName,
                        StartDate = d.StartDate,
                        EndDate = d.EndDate,
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
            
            return leaveSubordinates;
        }

        public List<LeaveHistory> GetLeaveHistory(string employeeID)
        {
            List<LeaveHistory> leaveHistories = new List<LeaveHistory>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveEmployeeAsync(Context, employeeID);
                foreach (var d in data.Result.response)
                {
                    leaveHistories.Add(new LeaveHistory
                    {
                        EmplId = d.EmplId,
                        EmplName = d.EmplName,
                        RecId = d.RecId,
                        Description = d.Description,
                        Status = (UpdateRequestStatus)d.ApprovalStatus,
                        Schedule = new DateRange(d.StartDate, d.EndDate),
                        StartDate = d.StartDate,
                        EndDate = d.EndDate,
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
            
            return leaveHistories;
        }

        public List<HolidaySchedule> GetHolidays(string employeeID, DateRange dateRange, bool combinedWithLeave = false)
        {
            var tasks = new List<Task<TaskRequest<List<HolidaySchedule>>>>();

            // Fetch data holiday
            tasks.Add(Task.Run(() =>
            {
                var adp = new TimeManagementAdapter(Configuration);
                var holidaySchedules = adp.Get(employeeID, new DateRange(dateRange.Start, dateRange.Finish), true, false);
                var holidays = new List<HolidaySchedule>();
                foreach (var x in holidaySchedules)
                {
                    holidays.Add(new HolidaySchedule
                    {
                        EmployeeID = x.EmployeeID,
                        LoggedDate = x.LoggedDate.ToLocalTime(),
                        AbsenceCode = x.AbsenceCode,
                        IsLeave = x.IsLeave
                    });
                }

                return TaskRequest<List<HolidaySchedule>>.Create("holiday", holidays);
            }));

            if (combinedWithLeave)
            {
                // Fetch data leave
                tasks.Add(Task.Run(() =>
                {
                    var adp = new TimeManagementAdapter(Configuration);
                    var holidaySchedules = adp.Get(employeeID, new DateRange(dateRange.Start, dateRange.Finish), false, combinedWithLeave);
                    var holidays = new List<HolidaySchedule>();
                    foreach (var x in holidaySchedules)
                    {
                        holidays.Add(new HolidaySchedule
                        {
                            EmployeeID = x.EmployeeID,
                            LoggedDate = x.LoggedDate.ToLocalTime(),
                            AbsenceCode = x.AbsenceCode,
                            IsLeave = x.IsLeave
                        });
                    }

                    return TaskRequest<List<HolidaySchedule>>.Create("leave", holidays);
                }));
            }

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
            var result = new List<HolidaySchedule>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    result.AddRange(r.Result);
            }

            result.GroupBy(x => x.LoggedDate).Select(y=> y.ToList().Find(z => z.IsLeave) ?? y.First());
            return result;            
        }

        public List<Employee> GetLeaveSubtitutions(string employeeID)
        {            
            List<Employee> leaveSubtitutions = new List<Employee>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getLMLeaveEmplSubtituteAsync(Context, employeeID);
                foreach (var employee in data.Result.response)
                {
                    leaveSubtitutions.Add(new Employee()
                    {
                        EmployeeID = employee.SubtituteEmplId,
                        EmployeeName = employee.SubtituteEmplName,
                        AXID = employee.RecId,
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
                                              
            return leaveSubtitutions;
        }

        public List<Employee> GetEmployees(string employeeID)
        {
            throw new NotImplementedException();
        }
    }
}
