using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class TimeManagementAdapter
    {
        private IConfiguration Configuration;        
        private Credential credential;

        public TimeManagementAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);            
        }

        public KESSTMServices.KESSTMServiceClient GetClient()
        {
            var Client = new KESSTMServices.KESSTMServiceClient();
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

        public KESSTMServices.CallContext GetContext()
        {
            var Context = new KESSTMServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<TimeAttendance> Get(string employeeID, DateRange range, bool holidayOnly = false, bool leaveOnly = false)
        {
            var attendances = new List<TimeAttendance>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getTMEmplScheduleDetailAsync(Context, employeeID, range.Start, range.Finish, holidayOnly, leaveOnly).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    if (!string.IsNullOrEmpty(d.SchedulePatternId)) {
                        if (holidayOnly && d.SchedulePatternId.ToLower().StartsWith("ns") && d.Holiday == KESSTMServices.NoYes.Yes)
                        {
                        }
                        
                        if (holidayOnly && !d.SchedulePatternId.ToLower().StartsWith("ns") && (!(d.NormalIN == 0 && d.NormalOUT == 0) && d.Holiday == KESSTMServices.NoYes.Yes))
                        {
                            continue;
                        }
                    }
                    

                    var ClockIn = (d.ClockIN == 0 && d.ClockOUT == 0) ? default(DateTime) : Helper.secondsToDateTime(d.TransDate, d.ClockIN);
                    var ClockOut = (d.ClockIN == 0 && d.ClockOUT == 0) ? default(DateTime) : Helper.secondsToDateTime(d.TransDate, d.ClockOUT);
                    attendances.Add(new TimeAttendance
                    {
                        AbsenceCode = d.AbsenceCodeId,
                        ActualLogedDate = new DateRange(Helper.secondsToDateTime(d.TransDate, d.ClockIN), Helper.secondsToDateTime(d.TransDate, d.ClockOUT)),
                        ScheduledDate = new DateRange(Helper.secondsToDateTime(d.TransDate, d.NormalIN), Helper.secondsToDateTime(d.TransDate, d.NormalOUT)),
                        Days = d.Days,
                        Absent = (d.ClockIN == 0 && d.ClockOUT == 0),
                        EmployeeID = d.EmplId,
                        LoggedDate = d.TransDate,
                        AXID = d.RecId,
                        IsLeave= d.Leave == KESSTMServices.NoYes.Yes,
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
            
            return attendances.OrderByDescending(x => x.LoggedDate).ToList();
        }

        public List<TimeAttendance> GetSubordinate(string employeeID, DateRange range)
        {
            var attendances = new List<TimeAttendance>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getTMEmplScheduleSubordinateAsync(Context, employeeID, range.Start, range.Finish);
                foreach (var d in data.Result.response)
                {
                    var ClockIn = (d.ClockIN == 0 && d.ClockOUT == 0) ? default(DateTime) : Helper.secondsToDateTime(d.TransDate, d.ClockIN);
                    var ClockOut = (d.ClockIN == 0 && d.ClockOUT == 0) ? default(DateTime) : Helper.secondsToDateTime(d.TransDate, d.ClockOUT);
                    attendances.Add(new TimeAttendance
                    {
                        AbsenceCode = d.AbsenceCodeId,
                        ActualLogedDate = new DateRange(Helper.secondsToDateTime(d.TransDate, d.ClockIN), Helper.secondsToDateTime(d.TransDate, d.ClockOUT)),
                        ScheduledDate = new DateRange(Helper.secondsToDateTime(d.TransDate, d.NormalIN), Helper.secondsToDateTime(d.TransDate, d.NormalOUT)),
                        Days = d.Days,
                        Absent = (d.ClockIN == 0 && d.ClockOUT == 0),
                        EmployeeID = d.EmplId,
                        ReportToEmployeeID = d.ReportToEmplId,
                        LoggedDate = d.TransDate,
                        EmployeeName = d.EmplName,

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

            return attendances.OrderByDescending(x => x.LoggedDate).ToList();
        }

        public List<Period> GetPeriodTable()
        {
            var periods = new List<Period>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {

                Client.OpenAsync().RunSynchronously();
                var data = Client.getTMMasterPeriodTableAsync(Context);
                foreach (var d in data.Result.response)
                {
                    periods.Add(new Period
                    {
                        PeriodID = d.PeriodId,
                        Range = new DateRange(d.StartDate, d.EndDate),
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
            
            return periods;
        }

        public List<AbsenceImported> GetAbsenceImported(string employeeID)
        {
            var AbsenceImporteds = new List<AbsenceImported>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getTMAbsenImportedAsync(Context, employeeID);
                foreach (var d in data.Result.response)
                {
                    var clock = Helper.secondsToDateTime(d.PresenceDate, d.Clock);
                    AbsenceImporteds.Add(new AbsenceImported
                    {
                        EmplIdField = d.EmplId,
                        EmplNameField = d.EmplName,
                        InOutField = d.InOut,
                        Clock = clock,
                        PresenceDateField = d.PresenceDate,
                        RecIdField = d.RecId

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
                        
            return AbsenceImporteds;
        }


    }
}
