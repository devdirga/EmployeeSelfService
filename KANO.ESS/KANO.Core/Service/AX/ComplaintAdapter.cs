using Aspose.Words.Fields;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KANO.Core.Service.AX
{
    public class ComplaintAdapter
    {
        private readonly Credential credential;

        public ComplaintAdapter(IConfiguration config)
        {
            credential = Tools.AXConfiguration(config);
        }

        public KESSCNTServices.KESSCNTServiceClient GetClient()
        {
            var Client = new KESSCNTServices.KESSCNTServiceClient();
            var uri = new UriBuilder(Client.Endpoint.Address.Uri)
            {
                Host = credential.Host,
                Port = credential.Port
            };
            Client.Endpoint.Address = new System.ServiceModel.EndpointAddress(uri.Uri, new System.ServiceModel.UpnEndpointIdentity(credential.UserPrincipalName));
            Client.ClientCredentials.Windows.ClientCredential.Domain = credential.Domain;
            Client.ClientCredentials.Windows.ClientCredential.UserName = credential.Username;
            Client.ClientCredentials.Windows.ClientCredential.Password = credential.Password;
            return Client;
        }

        public KESSCNTServices.CallContext GetContext()
        {
            return new KESSCNTServices.CallContext()
            {
                Company = credential.Company
            };
        }

        public List<Agenda> GetAgenda(DateRange dateRange, string employeeID = null)
        {
            var result = new List<Agenda>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var range = Tools.normalizeFilter(dateRange);
                var agendas = Client.getAgendaListAsync(Context, range.Start, range.Finish).GetAwaiter().GetResult().response;
                foreach (var agenda in agendas)
                {
                    var employeeIDList = agenda.EmplIdList.Split(";");
                    if (agenda.AgendaForType != KESSCNTServices.KESSAgendaForType.All)
                    {                        
                        if (!string.IsNullOrWhiteSpace(employeeID))
                        {
                            if (!employeeIDList.Contains(employeeID)) continue;
                        }
                    }

                    var data = new Agenda
                    {
                        Schedule = new DateRange(agenda.AgendaDateStart, agenda.AgendaDateEnd),
                        AgendaFor = agenda.AgendaForType,
                        Name = agenda.AgendaName,
                        Description = agenda.Description,
                        Issuer = agenda.Issuer,
                        Location = agenda.Location,
                        Notes = agenda.Notes,
                        AXID = agenda.RecId,
                        Hash = JsonConvert.SerializeObject(agenda).ToMD5(),
                    };

                    // Map attachment
                    data.Attachments = new List<FieldAttachment>();
                    foreach (var document in agenda.DocumentList) {
                        data.Attachments.Add(new FieldAttachment { 
                            Filepath= document.DocumentPath,
                            Notes = document.Notes,
                            AXID=document.RecId,
                        });
                    }

                    // Map recipient list                    
                    data.EmployeeRecipients = new List<string>();
                    foreach (var recipient in employeeIDList) {
                        if(!string.IsNullOrWhiteSpace(recipient)) data.EmployeeRecipients.Add(recipient);
                    }

                    result.Add(data);
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

        public List<Agenda> GetAgendaMobile(string employeeID, DateRange dateRange)
        {
            var result = new List<Agenda>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //DateRange dateRange = new DateRange(start : DateTime.Now,finish: DateTime.Now);
                var range = Tools.normalizeFilter(dateRange);
                var agendas = Client.getAgendaListAsync(Context, range.Start, range.Finish).GetAwaiter().GetResult().response;

                foreach (var agenda in agendas)
                {
                    if (agenda.AgendaDateStart <= DateTime.Now && DateTime.Now <= agenda.AgendaDateEnd)
                    {
                        var employeeIDList = agenda.EmplIdList.Split(";");
                        if (agenda.AgendaForType != KESSCNTServices.KESSAgendaForType.All)
                        {
                            if (!string.IsNullOrWhiteSpace(employeeID))
                            {
                                if (!employeeIDList.Contains(employeeID)) continue;
                            }
                        }

                        CultureInfo provider = CultureInfo.InvariantCulture;
                        String dateDesc = String.Empty;
                        String timeDesc = String.Empty;

                        if (agenda.AgendaDateStart.Date == agenda.AgendaDateEnd.Date)
                        {
                            dateDesc = agenda.AgendaDateStart.Date.ToString("dd MMM yyyy", provider);
                            timeDesc = $"{agenda.AgendaDateStart.ToString("HH:mm", provider)} - {agenda.AgendaDateEnd.ToString("HH:mm", provider)}";
                        }
                        else if ((agenda.AgendaDateStart.Date < agenda.AgendaDateEnd.Date) && (agenda.AgendaDateStart.Date.Month == agenda.AgendaDateEnd.Date.Month) && (agenda.AgendaDateStart.Date.Year == agenda.AgendaDateEnd.Date.Year))
                        {
                            dateDesc = $"{agenda.AgendaDateStart.Date.Day} - {agenda.AgendaDateEnd.Date.ToString("dd MMM yyyy", provider)}";
                            timeDesc = $"{agenda.AgendaDateStart.ToString("HH:mm", provider)} - {agenda.AgendaDateEnd.ToString("HH:mm", provider)}";
                        }
                        else if ((agenda.AgendaDateStart.Date.Month < agenda.AgendaDateEnd.Date.Month))
                        {
                            dateDesc = $"{agenda.AgendaDateStart.Date.ToString("dd MMM", provider)} - {agenda.AgendaDateEnd.Date.ToString("dd MMM yyyy", provider)}";
                            timeDesc = $"{agenda.AgendaDateStart.ToString("HH:mm", provider)} - {agenda.AgendaDateEnd.ToString("HH:mm", provider)}";
                        }
                        else if (agenda.AgendaDateStart.Date.Year < agenda.AgendaDateEnd.Date.Year)
                        {
                            dateDesc = $"{agenda.AgendaDateStart.Date.ToString("dd MMM yyyy", provider)} - {agenda.AgendaDateEnd.Date.ToString("dd MMM yyyy", provider)}";
                            timeDesc = $"{agenda.AgendaDateStart.ToString("HH:mm", provider)} - {agenda.AgendaDateEnd.ToString("HH:mm", provider)}";
                        }

                        var data = new Agenda
                        {
                            Schedule = new DateRange(agenda.AgendaDateStart, agenda.AgendaDateEnd),
                            AgendaFor = agenda.AgendaForType,
                            Name = agenda.AgendaName,
                            Description = agenda.Description,
                            Issuer = agenda.Issuer,
                            Location = agenda.Location,
                            Notes = agenda.Notes,
                            AXID = agenda.RecId,
                            Hash = JsonConvert.SerializeObject(agenda).ToMD5(),
                            DateDesc = dateDesc,
                            TimeDesc = timeDesc,
                            UpdateBy = $"{dateDesc} @ {timeDesc}"
                        };

                        data.Attachments = new List<FieldAttachment>();
                        foreach (var document in agenda.DocumentList)
                        {
                            data.Attachments.Add(new FieldAttachment
                            {
                                Filepath = document.DocumentPath,
                                Notes = document.Notes,
                                AXID = document.RecId,
                            });
                        }

                        data.EmployeeRecipients = new List<string>();
                        foreach (var recipient in employeeIDList)
                        {
                            if (!string.IsNullOrWhiteSpace(recipient)) data.EmployeeRecipients.Add(recipient);
                        }

                        result.Add(data);
                    }
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

        public List<TicketRequest> GetComplaintsByEmplID(String EmployeeID)
        {
            var result = new List<TicketRequest>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var complaints = Client.getComplaintsByEmplIdAsync(Context, EmployeeID).GetAwaiter().GetResult().response;
                foreach (var complaint in complaints)
                {
                    result.Add(new TicketRequest()
                    {
                        AXID = complaint.RecId,
                        Description = complaint.Description,
                        EmailCC = complaint.EmailCC
                    });
                }

                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }
    }
}
