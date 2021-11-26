using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using KANO.Core.Lib.Helper;
using System.Diagnostics;

namespace KANO.Core.Service.AX
{
    public class RetirementAdapter
    {
        private readonly Credential credential;

        public RetirementAdapter(IConfiguration config)
        {
            credential = Tools.AXConfiguration(config);
        }

        public KESSMPPServices.KESSMPPServiceClient GetClient()
        {
            var Client = new KESSMPPServices.KESSMPPServiceClient();
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

        public KESSMPPServices.CallContext GetContext()
        {
            return new KESSMPPServices.CallContext()
            {
                Company = credential.Company
            };
        }        

        public  Retirement Get(string employeeID) {
            Retirement retirement = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var request = new KESSMPPServices.KESSMPPServiceGetMPPByEmplIdRequest
                {
                    CallContext = Context,
                    EmplId = employeeID
                };
                var data = Client.getMPPByEmplIdAsync(request).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    if (d.MPPStatus != KESSMPPServices.KESSMPPStatus.Rejected) {
                        retirement = this.mapFromAX(d);
                        break;
                    }                    
                }

                if (retirement != null)
                {
                    retirement.Histories = new List<Retirement>();
                    foreach (var d in data)
                    {
                        retirement.Histories.Add(this.mapFromAX(d));
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

            return retirement;
            
        }

        public Retirement GetByID(string MPPID)
        {
            Retirement retirement = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var request = new KESSMPPServices.KESSMPPServiceGetMPPByIdRequest
                {
                    CallContext = Context,
                    MPPId= MPPID
                };
                var data = Client.getMPPByIdAsync(request).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    retirement = this.mapFromAX(d);
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

            return retirement;

        }

        public Retirement GetByID(long AXID)
        {
            Retirement retirement = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var request = new KESSMPPServices.KESSMPPServiceGetMPPByRecIdRequest
                {
                    CallContext = Context,
                    recId=AXID
                };
                var data = Client.getMPPByRecIdAsync(request).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    retirement = this.mapFromAX(d);
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

            return retirement;

        }

        public List<Retirement> GetS()
        {
            var retirements = new List<Retirement>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var request = new KESSMPPServices.KESSMPPServiceGetMPPListRequest
                {
                    CallContext = Context,                    
                };
                var data = Client.getMPPListAsync(request).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    retirements.Add(this.mapFromAX(d));
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

            return retirements;
        }

        public List<Retirement> GetS(KESSMPPServices.KESSMPPStatus status)
        {
            var retirements = new List<Retirement>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var request = new KESSMPPServices.KESSMPPServiceGetMPPListByStatusRequest
                {
                    CallContext = Context,
                    status=status
                };
                var data = Client.getMPPListByStatusAsync(request).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    retirements.Add(this.mapFromAX(d));
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

            return retirements;
        }

        private Retirement mapFromAX(KESSMPPServices.MPPEmpl data) { 
            return new Retirement
            {
                BirthDate = data.BirthDate,
                CBDate = new DateRange(data.CBDate, data.MPPDateStart.AddDays(-1)),
                CBType = data.CBType == KESSMPPServices.KESSCBType.ThreeMonth ? CBType.Bulan3 : CBType.Bulan2,
                Department = data.Department,
                Description = data.Description,
                Filepath = data.DocumentPath,
                EmployeeID = data.EmplId,
                EmployeeName = data.EmplName,
                MPPDate = new DateRange(data.MPPDateStart, data.MPPDateEnd),
                MPPID = data.MPPId,
                MPPStatus = data.MPPStatus,
                MPPType = data.MPPType == KESSMPPServices.KESSMppType.SixMonth ? MPPType.Bulan6 : MPPType.Bulan12,
                Position = data.Position,
                AXID = data.RecId,
            };
        }


    }
}
