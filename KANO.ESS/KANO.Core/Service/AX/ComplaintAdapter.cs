using Aspose.Words.Fields;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

    }
}
