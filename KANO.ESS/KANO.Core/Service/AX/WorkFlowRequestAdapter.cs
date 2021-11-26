using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;

namespace KANO.Core.Service.AX
{
    public class WorkFlowRequestAdapter
    {
        private IConfiguration Configuration;        
        private readonly Credential credential;

        public WorkFlowRequestAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);
        }

        public KESSWRServices.KESSWRServiceClient GetClient()
        {
            var Client = new KESSWRServices.KESSWRServiceClient();
            var uri = new UriBuilder(Client.Endpoint.Address.Uri)
            {
                Host = credential.Host,
                Port = credential.Port
            };
            Client.Endpoint.Address = new System.ServiceModel.EndpointAddress(uri.Uri,new System.ServiceModel.UpnEndpointIdentity(credential.UserPrincipalName));
            Client.ClientCredentials.Windows.ClientCredential.Domain = credential.Domain;
            Client.ClientCredentials.Windows.ClientCredential.UserName = credential.Username;
            Client.ClientCredentials.Windows.ClientCredential.Password = credential.Password;
            return Client;
        }

        public KESSWRServices.CallContext GetContext()
        {
            return new KESSWRServices.CallContext()
            {
                Company = credential.Company
            };
        }

        public string RequestTimeAttendance(TimeAttendance timeAttendance)
        {
            if (timeAttendance == null)
            {
                throw new ArgumentNullException(nameof(timeAttendance));
            }

            KESSWRServices.KESSWRServiceUpdTMEmplScheduleDetailResponse result;                        

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {                
                var employeeScheduleDetail = new KESSWRServices.TMEmplScheduleDetail
                {
                    EmplId = timeAttendance.EmployeeID,
                    EmplName = timeAttendance.EmployeeName,
                    AbsenceCodeId = timeAttendance.AbsenceCode,
                    TransDate = timeAttendance.LoggedDate,
                    ClockIN = Helper.AXClock(timeAttendance.ActualLogedDate.Start),
                    ClockOUT = Helper.AXClock(timeAttendance.ActualLogedDate.Finish),
                    RecId = timeAttendance.AXID,
                    DocumentPath = this.pathEscape(timeAttendance.Filepath),
                };

                result = Client.updTMEmplScheduleDetailAsync(Context, employeeScheduleDetail, timeAttendance.Reason).GetAwaiter().GetResult();
            }
            catch (Exception) {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();

            }
            
            
            return (result != null)?result.response:"";
        }
        
        public string RequestLeave(Leave leave)
        {
            if (leave == null)
            {
                throw new ArgumentNullException(nameof(leave));
            }

            KESSWRServices.KESSWRServiceNewLMLeaveEmployeeResponse result;                                

            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var newLeave = new KESSWRServices.LMLeaveEmployee
                {
                    EmplId = leave.EmployeeID,
                    Description = leave.Description,
                    StartDate = leave.Schedule.Start,
                    EndDate = leave.Schedule.Finish,
                    LeaveType = int.Parse(leave.Type),
                    SubtituteEmplId = leave.SubtituteEmployeeID,
                    DocumentPath = this.pathEscape(leave.Filepath),
                };
                result = Client.newLMLeaveEmployeeAsync(Context, newLeave, leave.Description).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }    
            
            return (result != null)?result.response:"";
        }

        public string RequestCertificate(Certificate certificate, string reason)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            KESSWRServices.KESSWRServiceNewOrUpdHCMPersonCertificateResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var newOrUpdcertificate = new KESSWRServices.HCMPersonCertificate
                {
                    EmplId = certificate.EmployeeID,
                    Description = certificate.TypeDescription,
                    CertificateTypeId = certificate.TypeID,
                    ExpirationDate=certificate.Validity.Finish,
                    IssueDate=certificate.Validity.Start,
                    Note=certificate.Note,
                    Purpose=certificate.Purpose,
                    RecId=certificate.AXID==-1 ? 0: certificate.AXID,
                    RequireRenewal=certificate.ReqRenew ? KESSWRServices.NoYes.Yes:KESSWRServices.NoYes.No,                    
                    DocumentPath = this.pathEscape(certificate.Filepath),
                };
                result = Client.newOrUpdHCMPersonCertificateAsync(Context, newOrUpdcertificate, reason).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response : "";
        }

        public string RequestFamily(Family family, string reason)
        {
            if (family == null)
            {
                throw new ArgumentNullException(nameof(family));
            }

            KESSWRServices.KESSWRServiceNewOrUpdHCMPersonContactResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var newOrUpdateFamily = new KESSWRServices.HCMPersonContact
                {
                    EmplId = family.EmployeeID,
                    BirthDate= family.Birthdate.ToLocalTime(),
                    BirthPlace = family.Birthplace,
                    EmplName = family.EmployeeName,
                    Gender = (KESSWRServices.HcmPersonGender) family.Gender,
                    KTP = family.NIK,
                    PersonFirstName = family.FirstName,
                    PersonLastName = family.LastName,
                    PersonMiddleName = family.MiddleName,
                    PersonName = family.Name,
                    RelationshipTypeDescription = family.RelationshipDescription,
                    RelationshipTypeId = family.Relationship,
                    Religion = (KESSWRServices.Religion) family.Religion,
                    Purpose = family.Purpose,
                    RecId = family.AXID == -1 ? 0 : family.AXID,
                    DocumentPath = this.pathEscape(family.Filepath),                    
                };
                result = Client.newOrUpdHCMPersonContactAsync(Context, newOrUpdateFamily, reason).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response : "";
        }

        public string RequestEmployee(EmployeeResult employee, List<IdentificationResult> identifications, List<BankAccountResult> bankAccounts, List<TaxResult> taxes, List<ElectronicAddressResult> electronicAddresses, AddressResult address, string reason)        
        {
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }

            KESSWRServices.KESSWRServiceNewOrUpdHCMEmployeeResumeResponse result;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                KESSWRServices.HCMWorker worker = null;
                KESSWRServices.HCMLogisticsPostalAddress workerAddress = null;
                List<KESSWRServices.HCMPersonIdentification> workerIdentifications = new List<KESSWRServices.HCMPersonIdentification>();
                List<KESSWRServices.HCMWorkerTaxInfo> workerTaxes = new List<KESSWRServices.HCMWorkerTaxInfo>();
                List<KESSWRServices.HCMWorkerBankAccount> workerBankAccounts = new List<KESSWRServices.HCMWorkerBankAccount>();
                List<KESSWRServices.HCMLogisticsElectronicAddress> workerElectronicAddresses = new List<KESSWRServices.HCMLogisticsElectronicAddress>();

                // Start process
                var tasks = new List<Task<TaskRequest<object>>>();

                // Update worker
                tasks.Add(Task.Run(() =>
                {
                    worker = new KESSWRServices.HCMWorker
                    {
                        EmplId = employee.UpdateRequest.EmployeeID,
                        EmplName = employee.UpdateRequest.EmployeeName,
                        RecId = this.getRecID(employee.UpdateRequest.AXID),
                    };
                    return TaskRequest<object>.Create("woker", worker);
                }));

                // Update person detail
                //tasks.Add(Task.Run(() =>
                //{
                //    personDetail = new KESSWRServices.HCMPersonDetails
                //    {
                //        EmplId = employee.UpdateRequest.EmployeeID,
                //        EmplName = employee.UpdateRequest.EmployeeName,
                //        IsExpatriate = (KESSWRServices.NoYes) (employee.UpdateRequest.IsExpatriate ? 1: 0),
                //        MaritalStatus = (KESSWRServices.HcmPersonMaritalStatus) employee.UpdateRequest.MaritalStatus,
                //        DocumentPath = ""
                //    };
                //    return TaskRequest<object>.Create("personDetail", personDetail);
                //}));

                // Update address
                if (address.UpdateRequest != null)
                {                    
                    tasks.Add(Task.Run(() =>
                    {
                        var d = address.UpdateRequest;
                        workerAddress = new KESSWRServices.HCMLogisticsPostalAddress
                        {
                            EmplId = d.EmployeeID,
                            EmplName = d.EmployeeName,
                            City = d.City,
                            Street = d.Street,
                            DocumentPath = this.pathEscape(d.Filepath),
                            RecId = this.getRecID(d.AXID),
                            Purpose=this.getPurpose(d.AXID)
                        };


                        return TaskRequest<object>.Create("address", workerAddress);
                    }));
                }

                // Update identification
                tasks.Add(Task.Run(() =>
                {
                    identifications.ForEach((data) =>
                    {
                        if (data.UpdateRequest != null)
                        {
                            var d = data.UpdateRequest;
                            workerIdentifications.Add(new KESSWRServices.HCMPersonIdentification
                            {
                                Description = d.Description,
                                DocumentPath = this.pathEscape(d.Filepath),
                                EmplId = d.EmployeeID,
                                EmplName = d.EmployeeName,
                                IssuingAgencyId = d.IssuingAggency,
                                Number = d.Number,
                                RecId = this.getRecID(d.AXID),
                                TypeId = d.Type,
                                Purpose = this.getPurpose(d.AXID)
                            });
                        }
                    });

                    return TaskRequest<object>.Create("identification", workerIdentifications);
                }));

                // Update tax info
                tasks.Add(Task.Run(() =>
                {
                    taxes.ForEach((data) =>
                    {
                        if (data.UpdateRequest != null)
                        {
                            var d = data.UpdateRequest;
                            workerTaxes.Add(new KESSWRServices.HCMWorkerTaxInfo
                            {
                                DocumentPath = this.pathEscape(d.Filepath),
                                EmplId = d.EmployeeID,
                                EmplName = d.EmployeeName,
                                RecId = this.getRecID(d.AXID),
                                CardType = (KESSWRServices.HRMTaxCardType)d.Type,
                                NPWP = d.NPWP,
                                Purpose = this.getPurpose(d.AXID)
                            });
                        }
                    });

                    return TaskRequest<object>.Create("tax", workerTaxes);
                }));

                // Update bank account
                tasks.Add(Task.Run(() =>
                {
                    bankAccounts.ForEach((data) =>
                    {
                        if (data.UpdateRequest != null)
                        {
                            var d = data.UpdateRequest;
                            workerBankAccounts.Add(new KESSWRServices.HCMWorkerBankAccount
                            {
                                DocumentPath = this.pathEscape(d.Filepath),
                                EmplId = d.EmployeeID,
                                EmplName = d.EmployeeName,
                                RecId = this.getRecID(d.AXID),
                                AccountId = d.AccountID,
                                AccountNum = d.AccountNumber,
                                Name = d.Name,
                                Purpose = this.getPurpose(d.AXID)
                            });
                        }
                    });

                    return TaskRequest<object>.Create("bankAccount", workerTaxes);
                }));


                // Update logistic address
                tasks.Add(Task.Run(() =>
                {
                    electronicAddresses.ForEach((data) =>
                    {
                        if (data.UpdateRequest != null)
                        {
                            var d = data.UpdateRequest;
                            workerElectronicAddresses.Add(new KESSWRServices.HCMLogisticsElectronicAddress
                            {
                                DocumentPath = this.pathEscape(d.Filepath),
                                EmplId = d.EmployeeID,
                                EmplName = d.EmployeeName,
                                RecId = this.getRecID(d.AXID),
                                Locator = d.Locator,
                                Type = (KESSWRServices.LogisticsElectronicAddressMethodType)d.Type,
                                Purpose = this.getPurpose(d.AXID)
                            });
                        }
                    });

                    return TaskRequest<object>.Create("electronicAddress", workerTaxes);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to update employee '{employee.UpdateRequest.EmployeeID}' :\n{e.Message}", e);
                }

                result = Client.newOrUpdHCMEmployeeResumeAsync(
                        Context,
                        worker,
                        workerIdentifications.ToArray(),
                        workerTaxes.ToArray(),
                        workerBankAccounts.ToArray(),
                        workerElectronicAddresses.ToArray(),
                        workerAddress,
                        reason
                    )
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response : "";
        }       

        public string RequestBenefit(MedicalBenefit benefit)
        {
            if (benefit == null)
            {
                throw new ArgumentNullException(nameof(benefit));
            }

            KESSWRServices.KESSWRServiceNewCRClaimReimbursementResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                object objRelationship = KESSWRServices.KESSFamililyRelationship.Worker;
                Enum.TryParse(typeof(KESSWRServices.KESSFamililyRelationship), benefit.Family.Relationship, out objRelationship);

                var cl = new KESSWRServices.CRClaimReimburseHealthy();

                var documents = new List<KESSWRServices.CRDocuRef>();

                foreach (var detail in benefit.Details)
                {
                    documents.Add(new KESSWRServices.CRDocuRef
                    {
                        Amount = (decimal)detail.Amount,
                        Notes = JsonConvert.SerializeObject(detail),
                        DocumentPath = this.pathEscape(detail.Attachment.Filepath),
                        DocuName = detail.Attachment.Filename,                        
                    });
                }
                

                var newClaim = new KESSWRServices.CRClaimReimburseHealthy
                {
                    ClaimDate=benefit.RequestDate,
                    ClaimId=benefit.RequestID,
                    EmplId=benefit.EmployeeID,
                    EmplName=benefit.EmployeeName,
                    FamilyRelationship=(benefit.Family.AXID > 0)? (KESSWRServices.KESSFamililyRelationship)objRelationship : KESSWRServices.KESSFamililyRelationship.Worker,
                    JenisRawat=(KESSWRServices.KESSJenisRawat)benefit.TypeID,
                    PasienName= (benefit.Family.AXID > 0)?benefit.Family.Name:benefit.EmployeeName,
                    DocumentList = documents.ToArray(),                    
                };
                result = Client.newCRClaimReimbursementAsync(Context, newClaim, benefit.Reason).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response : "";
        }

        private string pathEscape(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) path = "";
            return path.Replace("\\", "\\\\");
        }

        private long getRecID(long AXID) {
            return (AXID < -1) ? 0 : AXID;
        }

        private KESSWRServices.KESSWorkerRequestPurpose getPurpose(long AXID)
        {
            return (AXID < -1) ? KESSWRServices.KESSWorkerRequestPurpose.Insert : KESSWRServices.KESSWorkerRequestPurpose.Update;
        }

        public static string CancelLeave(string AXRequestID)
        {
            bool process;
            try
            {
                process = true;
                return process.ToString( new CultureInfo("en-US"));
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string RequestTravelExpense(Travel travel)
        {
            if (travel == null)
            {
                throw new ArgumentNullException(nameof(travel));
            }

            KESSWRServices.KESSWRServiceNewOrUpdTETravelRequestResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var newTravel = new KESSWRServices.TETravelRequest
                {
                    EmplId = travel.EmployeeID,
                    AsalKeberangkatan = travel.Origin,
                    TujuanDinas = travel.TravelPurpose,
                    TujuanKeberangkatan = travel.Destination,
                    TglMulai = travel.Schedule.Start,
                    TglAkhir = travel.Schedule.Finish,
                    JamBerangkat = Convert.ToInt32(travel.Schedule.Start.TimeOfDay.TotalSeconds),
                    JamAkhir = Convert.ToInt32(travel.Schedule.Finish.TimeOfDay.TotalSeconds),
                    CreatedBy = travel.CreatedBy,
                    CreatedDate = DateTime.Now,
                    TravelReqStatus = (KESSWRServices.KESSTrvExpTravelReqStatus)travel.TravelRequestStatus,
                    Note = travel.Note,
                    Transportasi = travel.Transportation,
                    TravelReqPurpose = KESSWRServices.KESSTravelRequestPurpose.Created,   
                    TravelAreaType = travel.TravelType,
                    DocumentPath = pathEscape(travel.Filepath)
                };

                result = Client.newOrUpdTETravelRequestAsync(Context, newTravel).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";
        }

        public string RevisionTravelExpense (Travel travel)
        {
            if (travel == null)
            {
                throw new ArgumentNullException(nameof(travel));
            }

            KESSWRServices.KESSWRServiceNewOrUpdTETravelRequestResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var newTravel = new KESSWRServices.TETravelRequest
                {
                    TravelReqId = travel.TravelID,
                    EmplId = travel.EmployeeID,
                    AsalKeberangkatan = travel.Origin,
                    TujuanDinas = travel.TravelPurpose,
                    TujuanKeberangkatan = travel.Destination,
                    TglMulai = travel.Schedule.Start,
                    TglAkhir = travel.Schedule.Finish,
                    JamBerangkat = Convert.ToInt32(travel.Schedule.Start.TimeOfDay.TotalSeconds),
                    JamAkhir = Convert.ToInt32(travel.Schedule.Finish.TimeOfDay.TotalSeconds),
                    CreatedBy = travel.CreatedBy,
                    CreatedDate = DateTime.Now,
                    TravelReqStatus = (KESSWRServices.KESSTrvExpTravelReqStatus)travel.TravelRequestStatus,
                    Note = travel.Note,
                    Transportasi = travel.Transportation,
                    TravelReqPurpose = KESSWRServices.KESSTravelRequestPurpose.Update,
                    RevisionBy = travel.RevisionBy,
                    RevisionDate = travel.RevisionDate,
                    TravelAreaType = travel.TravelType
                };

                result = Client.newOrUpdTETravelRequestAsync(Context, newTravel).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";

        }

        public string CloseTravelExpense(string TravelRequestID)
        {
           
            KESSWRServices.KESSWRServiceNewOrUpdTETravelRequestResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var newTravel = new KESSWRServices.TETravelRequest
                {
                    TravelReqId = TravelRequestID,
                    TravelReqPurpose = KESSWRServices.KESSTravelRequestPurpose.Closed,
                };

                result = Client.newOrUpdTETravelRequestAsync(Context, newTravel).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";
        }

        public string RegisterTraining (TrainingRegistration request, KESSWRServices.HRMCourseAttendeeStatus status = KESSWRServices.HRMCourseAttendeeStatus.Registered)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewHRMRegisterCourseResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var docuRefs = new List<KESSWRServices.CRDocuRef>();
                foreach(var reference in request.References){
                    docuRefs.Add(new KESSWRServices.CRDocuRef {                         
                        DocSource=(reference.Type==ReferenceType.Certificate)? KESSWRServices.KESSDocSource.Certificate: KESSWRServices.KESSDocSource.Document,
                        DocumentPath =reference.Attachment.Filepath,
                        DocuName= reference.Attachment.Filename,                        
                        RecId=reference.AXID,
                        RefTableId=0
                    });
                }

                var course = new KESSWRServices.HRMCourseAttendee
                {
                    CourseAttendeeStatus=status,
                    CourseId=request.TrainingID,
                    CourseRegistrationDate=request.RegistrationDate,
                    EmplId=request.EmployeeID,
                    DocumentList= docuRefs.ToArray(),
                    RecId =request.AXID == -1 ? 0: request.AXID
                };                

                result = Client.newHRMRegisterCourseAsync(Context, course).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";            
        }

        public string RequestRetirement (Retirement request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewMPPRequestResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var mpp = new KESSWRServices.MPPEmpl {
                    BirthDate = request.BirthDate,
                    CBDate = request.CBDate.Start,
                    CBType = request.CBType == CBType.Bulan2 ? KESSWRServices.KESSCBType.TwoMonth : KESSWRServices.KESSCBType.ThreeMonth,
                    Department = request.Department,
                    Position = request.Position,
                    PositionId = request.Position,
                    Description = request.Reason,
                    DocumentPath = request.Filepath,
                    EmplId = request.EmployeeID,
                    EmplName = request.EmployeeName,
                    MPPDateStart = request.MPPDate.Start,
                    MPPDateEnd = request.MPPDate.Finish,
                    MPPType = request.MPPType == MPPType.Bulan12 ? KESSWRServices.KESSMppType.TwelveMonth : KESSWRServices.KESSMppType.SixMonth,                    
                };
                result= Client.newMPPRequestAsync(Context, mpp, request.Reason).GetAwaiter().GetResult();                
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";
        }

        public string RequestRecruitment (Recruitment request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewHRMRecruitmentResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var newRecruitment = new KESSWRServices.KESSHRMRecruitment {                     
                    Description=request.Description,
                    DocumentPath = this.pathEscape(request.Filepath),
                    EmplIdRecruiter=request.EmployeeID,
                    EstimatedStartedDate=request.EstimationStartedDate,
                    JobId=request.JobID,
                    PositionId=request.PositionID,
                    Qty=request.NumberOfOpenings,                                        
                    RecruitingQty=request.NumberOfOpenings,                                        
                    RecruitingId=request.RecruitmentID,
                };
                result = Client.newHRMRecruitmentAsync(Context,newRecruitment, request.Description).GetAwaiter().GetResult();                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response :"";
        }

        public string ApplyToRecruitment (Application request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewHRMApplyRecruitmentResponse result;

            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {
                var apply = new KESSWRServices.KESSHRMApplicationHistory { 
                    ApplicationExpireDate=request.Recruitment.Deadline,
                    Department=request.Recruitment.Department,
                    EmplId=request.EmployeeID,
                    JobId=request.Recruitment.JobID??request.JobID,
                    RecruitingId=request.RecruitmentID,
                    StartDateTime=request.Recruitment.OpenDate
                };                

                result = Client.newHRMApplyRecruitmentAsync(Context, apply).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";
        }

        public string RequestComplaint (TicketRequest request, string reason = "")
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewComplaintsRequestResponse result;
            var Client = this.GetClient();
            var Context = this.GetContext();
            
            try
            {
                var complaint = new KESSWRServices.ComplaintsTicket
                {
                    Description=request.Description,
                    DocumentPath= this.pathEscape(request.Filepath),
                    EmailCC=JsonConvert.SerializeObject(request.EmailCC),
                    EmailFrom=request.EmailFrom,
                    EmplId=request.EmployeeID,
                    EmplName=request.EmployeeName,
                    Subject=request.Subject,
                    TicketDate=request.TicketDate,
                    TicketMedia=(KESSWRServices.KESSTicketMedia)request.TicketMedia,
                    TicketType=(KESSWRServices.KESSTicketType)request.TicketType,
                    //ComplaintsId=request.TicketID
                    
                };                
                result = Client.newComplaintsRequestAsync(Context, complaint, reason).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";

        }
        
        public string RequestCanteenVoucher (VoucherRequest request, VoucherRequestDetail detail, string reason = "")
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            KESSWRServices.KESSWRServiceNewCanteenVoucherRequestResponse result;
            var Client = this.GetClient();
            var Context = this.GetContext();
            
            try
            {
                var voucher = new KESSWRServices.CanteenVoucherRequest
                {
                    EmplId=request.EmployeeID,
                    EmplName=request.EmployeeName,
                    GeneratedForDate= detail.GeneratedForDate,
                };                
                result = Client.newCanteenVoucherRequestAsync(Context, voucher, reason).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return (result != null) ? result.response.ToString(new CultureInfo("en-US")) : "";

        }


        public string GetAbsenceInstanceID(string employeeID, DateTime loggedDate, KESSWRServices.KESSWorkflowTrackingStatus status = KESSWRServices.KESSWorkflowTrackingStatus.InReview)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();

            try
            {               
                var result = Client.getWRInstanceByEmplIdAsync(Context, employeeID, loggedDate, status).GetAwaiter().GetResult().response;
                if (result.Length > 0) {
                    return result[0].InstanceId;
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

            return "";

        }

    }
}
