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
    public class AbsenceAdapter
    {
        private IConfiguration Configuration;
        private Credential credential;

        public AbsenceAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(Configuration);
        }

        public KESSWRServices.KESSWRServiceClient GetClient()
        {
            var Client = new KESSWRServices.KESSWRServiceClient();
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

        public KESSWRServices.CallContext GetContext()
        {
            var Context = new KESSWRServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public string DoAbsenceClockInOut(AbsenceInOut absenceImported)
        {   
            var result = String.Empty;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var req = new KESSWRServices.TMAbsenImported
                {
                    Clock = Helper.AXClock(DateTime.Now),
                    EmplId = absenceImported.EmplIdField,
                    EmplName = absenceImported.EmplNameField,
                    InOut = absenceImported.InOutField,
                    PresenceDate = absenceImported.PresenceDateField,
                    RecId = absenceImported.RecIdField,
                    TermNo = absenceImported.TermNo
                };
                result = Client.newTMAbsenceInOutAsync(Context, req).GetAwaiter().GetResult().response;
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
