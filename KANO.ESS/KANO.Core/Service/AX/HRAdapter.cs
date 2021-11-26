using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace KANO.Core.Service.AX
{
    public class HRAdapter
    {
        private IConfiguration Configuration;
        private Credential credential;

        public HRAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);
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

        public List<AbsenceCode> Get()
        {
            var absenceCodes = new List<AbsenceCode>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHRMAbsenceCodeAsync(Context);
                foreach (var d in data.Result.response)
                {
                    absenceCodes.Add(new AbsenceCode
                    {                        
                        DescriptionField = d.Description,
                        GroupIdField = d.GroupId,
                        IdField = d.Id,
                        IsAttachment = (NoYes)d.AttachmentForUpdate == NoYes.Yes,
                        IsEditable = (NoYes)d.EnableForRequest== NoYes.Yes,
                        IsOnList = (NoYes)d.AvailableForUpdate == NoYes.Yes,

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
                                   
            return absenceCodes;
        }
        
        //public List<Department> GetDepartments()
        //{
        //    var dept = new List<Department>();
        //    var client = this.GetClient();
        //    var context = this.GetContext();

        //    try
        //    {
        //        var data = client.getHRMDepartmentAsync(context);
        //        foreach(var d in data.Result.response)
        //        {
        //            dept.Add(new Department
        //            {
                        
        //                RecId = d.RecId,
        //                Name = d.Name,
        //                NameAlias = d.NameAlias,
        //                EmployeeID = d.EmplId,
        //                EmployeeName = d.WorkerName,
        //                OperationUnitNumber = d.OMOperatingUnitNumber,
        //                OperationUnitType = d.OMOperatingUnitType
        //            });
        //        }
        //    } catch(Exception e)
        //    {
        //        throw;
        //    } finally {
        //        if (client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) client.CloseAsync().Wait();
        //    }

        //    return dept;
        //}

    }
}
