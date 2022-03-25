using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Model.Payroll;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class PayRollAdapter
    {
        private IConfiguration Configuration;        
        private Credential credential;

        public PayRollAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);
        }

        public KESSPYServices.KESSPYServiceClient GetClient()
        {
            var Client = new KESSPYServices.KESSPYServiceClient();
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

        public KESSPYServices.CallContext GetContext()
        {
            var Context = new KESSPYServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<PaySlip> GetPaySlips(string employeeID)
        {            
            var result = new List<PaySlip>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getPYPayrollCalculation(Context, employeeID);
                foreach (var d in data)
                {
                    result.Add(new PaySlip
                    {
                        AXID = d.RecId,
                        ProcessID = d.ProcessId,
                        CycleTimeDescription = Enum.GetName(typeof(KESSPYServices.PyCycleTime), d.ProcessType),
                        Month = d.PeriodMonth,
                        Year = d.PeriodYear,
                        MonthYear = $"{d.PeriodMonth}-{d.PeriodYear}",
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Filepath = d.PayslipReportPath,
                        AmountNetto=(double)d.NettoSalary,
                        YearMonth = $"{d.PeriodYear}{d.PeriodMonth}"
                        //PaySlipMonth =DateTime.Now,
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
           
            return result.OrderByDescending(a => a.YearMonth).ToList();
        }

        public PaySlip GetLatestPayslip(string employeeID)
        {
            PaySlip result = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getPYPayrollCalcLoan(Context, employeeID);
                foreach (var d in data)
                {
                    result = new PaySlip
                    {
                        AXID = d.RecId,
                        ProcessID = d.ProcessId,
                        CycleTimeDescription = Enum.GetName(typeof(KESSPYServices.PyCycleTime), d.ProcessType),
                        Month = d.PeriodMonth,
                        Year = d.PeriodYear,
                        MonthYear = $"{d.PeriodMonth}-{d.PeriodYear}",
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Filepath = d.PayslipReportPath,
                        AmountNetto = (double)d.NettoSalary,
                       
                        //PaySlipMonth =DateTime.Now,
                    };

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

            return result;
        }

        public string GenerateReport(string employeeID,string processID)
        {
            string path = "";
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getPYPayrollCalculation(Context, employeeID);
                var payslip = data.ToList().Find(x => x.ProcessId == processID);
                Console.WriteLine($">>> Found {processID} : {payslip == null}");
                if (payslip != null)
                {
                    Console.WriteLine($">>> Generating ...");
                    path = Client.newPYPayslipReport(Context, payslip);
                    Console.WriteLine($">>> Generated : {path}");
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally 
            {
                //if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
                                   
            return path;
        }

        public List<LoanRequest> GetLoanRequest(string employeeID)
        {
            //var data = Client.getTMMasterPeriodTable(Context);
            var loanRequests = new List<LoanRequest>();
            //foreach (var d in data)
            //{
            //    loanRequests.Add(new LoanRequest
            //    {
                    
            //    });
            //}
            return loanRequests;
        }
    }
}
