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
    public class BenefitAdapter
    {
        private IConfiguration Configuration;
        private Credential credential;

        public BenefitAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(Configuration);
        }

        public KESSCRServices.KESSCRServiceClient GetClient()
        {
            var Client = new KESSCRServices.KESSCRServiceClient();
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

        public KESSCRServices.CallContext GetContext()
        {
            var Context = new KESSCRServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<MedicalBenefit> GetS(string employeeID, DateRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));

            var benefit = new List<MedicalBenefit>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getListClaimReimbursementAsync(Context, employeeID, range.Start, range.Finish).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    benefit.Add(this.mapToAX(d));
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

            return benefit;
        }

        public MedicalBenefit GetByRecID(long AXID)
        {
            if (AXID == null) throw new ArgumentNullException(nameof(AXID));

            MedicalBenefit benefit = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getClaimReimbursementByRecIdAsync(Context, AXID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    benefit = this.mapToAX(d);
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

            return benefit;
        }

        public MedicalBenefit Get(string employeeID, string claimID)
        {
            MedicalBenefit benefit = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getClaimReimbursementAsync(Context, employeeID, claimID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    benefit = this.mapToAX(d);
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

            return benefit;
        }

        public static List<string> GetTypes() {
            return new List<string>(Enum.GetNames(typeof(KESSCRServices.KESSJenisRawat)));
        }

        public List<BenefitLimit> GetListLimit()
        {
            var benefit = new List<BenefitLimit>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getListClaimCreditLimitByGradeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    benefit.Add(new BenefitLimit
                    {
                        AXID = d.RecId,
                        CreditLimitAmount = d.CreditLimitAmount,
                        Grade = d.GradePosition,
                        GradeDescription = d.GradePositionDesc,
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
            return benefit;
        }

        public EmployeeBenefitLimit GetLimit(string employeeID)
        {
            EmployeeBenefitLimit benefit = null;
            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var data = Client.getClaimCreditLimitEmplAsync(Context, employeeID).GetAwaiter().GetResult().response;
                if (data.Length > 0)
                {
                    var d = data[0];
                    benefit = new EmployeeBenefitLimit
                    {
                        Balance = d.SaldoClaim,
                        CreditLimitAmount = d.CreditLimitClaim,
                        Grade = d.GradePosition,
                        GradeDescription = d.GradePositionDesc,
                        Department = d.Department,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Position = d.Position,
                    };
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

            return benefit;
        }

        private MedicalBenefit mapToAX(KESSCRServices.CRClaimReimburseHealthy claim) {
            var details = new List<MedicalBenefitDetail>();
            var totalAmmount = 0.0;
            foreach (var document in claim.DocumentList)
            {

                var tmpDetail = new MedicalBenefitDetail();
                if (!string.IsNullOrWhiteSpace(document.Notes)) {
                    tmpDetail = JsonConvert.DeserializeObject<MedicalBenefitDetail>(document.Notes);
                }

                details.Add(new MedicalBenefitDetail
                {
                    Attachment = new FieldAttachment
                    {
                        Filepath = document.DocumentPath,
                    },
                    AXID = document.RecId,
                    Amount = (double) document.Amount,
                    Description = tmpDetail.Description,
                    TypeID = tmpDetail.TypeID,                    
                });
                totalAmmount += (double)document.Amount;
            }

            var medical = new MedicalBenefit
            {
                AXID = claim.RecId,
                RequestDate = claim.ClaimDate,
                RequestID = claim.ClaimId,
                EmployeeID = claim.EmplId,
                EmployeeName = claim.EmplName,
                TypeID = claim.JenisRawat,
                RequestStatus = claim.StatusClaimReimburse,
                Details = details,
                TotalAmount= totalAmmount,                
            };

            if (claim.FamilyRelationship == KESSCRServices.KESSFamililyRelationship.Worker)
            {
                medical.Family = null;
            }
            else
            {
                medical.Family = new Family
                {
                    Relationship = claim.FamilyRelationship.ToString(),
                    Name = claim.PasienName,
                    AXID = 1,
                };
            }

            return medical;
        }

        
    }
}
