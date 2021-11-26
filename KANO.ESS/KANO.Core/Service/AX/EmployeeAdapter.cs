using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;

namespace KANO.Core.Service.AX
{
    public class EmployeeAdapter
    {        
        private IConfiguration Configuration;
        private Credential credential;

        public EmployeeAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);
        }

        public KESSHCMServices.KESSHCMServiceClient GetClient()
        {
            var Client = new KESSHCMServices.KESSHCMServiceClient();
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

        public KESSHCMServices.CallContext GetContext()
        {
            var Context = new KESSHCMServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public Employee Get(string employeeID)
        {
            Employee result = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMWorkerAsync(Context, employeeID).GetAwaiter().GetResult().response;
                if (data.Length > 0)
                {
                    var d = data[0];
                    result = new Employee
                    {
                        ProfilePicture =d.ImagePath,
                        AXID = d.RecId,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Department=d.Department,    
                        Position=d.Position,
                        LastEmploymentDate=d.LastWorkedDateTime,
                        WorkerTimeType = d.WorkerTimeType,
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
            
            return result;
        }

        public Employee GetFullDetail(string employeeID)
        {
            var data = this.GetDetail(employeeID);
            data.BankAccounts = this.GetBankAccounts(employeeID);
            data.Identifications = this.GetIdentifications(employeeID);

            return data;
        }

        public List<IdentificationType> GetIdentificationTypes()
        {
            var result = new List<IdentificationType>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var data = Client.getHCMIdentificationTypeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    result.Add(new IdentificationType
                    {
                        AXID = d.RecId,
                        Type = d.TypeId,
                        Description = d.Description
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
                                            
            return result;
        }


        public List<ElectronicAddressType> GetElectronicAddressTypes()
        {
            var result = new List<ElectronicAddressType>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var types = Enum.GetNames(typeof(KESSHCMServices.LogisticsElectronicAddressMethodType)).ToList<string>();
                var i = 0;
                foreach (var type in types) {
                    result.Add(new ElectronicAddressType
                    {
                        AXID = -1,
                        Type = (i++).ToString(),
                        Description = type
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

            return result;
        }

        public Employee GetDetail(string employeeID)
        {            
            var d = this.Get(employeeID);
            var Client = this.GetClient();
            var Context = this.GetContext();
            if (d != null)
            {
                //Client.OpenAsync().GetAwaiter();
                var tasks = new List<Task<TaskRequest<object>>>();

                // Fetch main data
                tasks.Add(Task.Run(() =>
                {
                    var data = Client.getHCMLogisticsPostalAddressAsync(Context, employeeID).GetAwaiter().GetResult().response;
                    if (data.Length > 0)
                    {
                        return TaskRequest<object>.Create("address", data[0]);
                    }
                    return TaskRequest<object>.Create("address", new KESSHCMServices.HCMLogisticsPostalAddress());
                }));                

                // Fetch address data
                tasks.Add(Task.Run(() =>
                {
                    var data = Client.getHCMLogisticsPostalAddressAsync(Context, employeeID).GetAwaiter().GetResult().response;
                    if (data.Length > 0)
                    {
                        return TaskRequest<object>.Create("address", data[0]);
                    }
                    return TaskRequest<object>.Create("address", new KESSHCMServices.HCMLogisticsPostalAddress());
                }));                

                // Fetch detail data
                tasks.Add(Task.Run(() => {                    
                    var data = Client.getHCMPersonDetailsAsync(Context, employeeID).GetAwaiter().GetResult().response;
                    if (data.Length > 0)
                    {
                        return TaskRequest<object>.Create("detail", data[0]);
                    }
                    return TaskRequest<object>.Create("detail", new KESSHCMServices.HCMPersonDetails());
                }));
                
                // Fetch private detail data
                tasks.Add(Task.Run(() => {                    
                    var data = Client.getHCMPersonPrivateDetailsAsync(Context, employeeID).GetAwaiter().GetResult().response;
                    if (data.Length > 0)
                    {
                        return TaskRequest<object>.Create("privateDetail", data[0]);
                    }
                    return TaskRequest<object>.Create("privateDetail", new KESSHCMServices.HcmPersonPrivateDetails());
                }));



                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally {
                    if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
                }

                // Combine result
                var result = new EmployeeResult();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                        switch (r.Label)
                        {
                            case "address":
                                var address = (KESSHCMServices.HCMLogisticsPostalAddress)r.Result;
                                d.Address.AXID = address.RecId;
                                d.Address.Street = address.Street;
                                d.Address.City = Format.TitleCase(address.City ?? "");
                                break;                           
                            case "detail":
                                var detail = (KESSHCMServices.HCMPersonDetails)r.Result;                                
                                d.IsExpatriate = detail.IsExpatriate == KESSHCMServices.NoYes.Yes;
                                d.MaritalStatus = (MaritalStatus)detail.MaritalStatus;                                                        
                                break;
                            case "privateDetail":
                                var privateDetail = (KESSHCMServices.HcmPersonPrivateDetails)r.Result;
                                d.Birthplace = privateDetail.BirthPlace;
                                d.Birthdate = privateDetail.BirthDate;
                                d.Gender = (Gender)privateDetail.Gender;
                                d.Religion = (Religion)privateDetail.Religion;
                                break;
                            default:
                                break;
                        }                    

                }
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
                return d;
            }
            if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            return null;
        }

        public Address GetAddress(string employeeID) {
            Address result = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var dataAddress = Client.getHCMLogisticsPostalAddressAsync(Context, employeeID).GetAwaiter().GetResult().response;
                if (dataAddress.Length > 0)
                {
                    var data = dataAddress[0];
                    result = new Address
                    {
                        City = Format.TitleCase(data.City),
                        EmployeeID = data.EmplId,
                        EmployeeName = data.EmplName,
                        AXID = data.RecId,
                        Street = data.Street,
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

            return result;
        }

        public List<Identification> GetIdentifications(string employeeID)
        {
            var result = new List<Identification>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var personIdentifications = Client.getHCMPersonIdentificationAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var pid in personIdentifications)
                {
                    result.Add(new Identification
                    {
                        Description = pid.Description,
                        EmployeeID = pid.EmplId,
                        EmployeeName = pid.EmplName,
                        IssuingAggency = pid.IssuingAgencyId,
                        Number = pid.Number,
                        AXID = pid.RecId,
                        Type = pid.TypeId,
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally 
            {
                Client.CloseAsync();
            }
            
            return result;
        }

        public List<BankAccount> GetBankAccounts(string employeeID)
        {
            var result = new List<BankAccount>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var bankAccounts = Client.getHCMWorkerBankAccountAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var ba in bankAccounts)
                {
                    result.Add(new BankAccount
                    {
                        AccountID = ba.AccountId,
                        AccountNumber = ba.AccountNum,
                        EmployeeID = ba.EmplId,
                        EmployeeName = ba.EmplName,
                        Name = ba.Name,
                        AXID = ba.RecId,
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
                                           
            return result;
        }

        public List<Tax> GetTaxes(string employeeID)
        {
            var result = new List<Tax>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var taxInfos = Client.getHCMWorkerTaxInfoAsync(Context, employeeID).GetAwaiter().GetResult().response;
                if (taxInfos.Length > 0)
                {
                    foreach (var d in taxInfos)
                    {
                        if (d.CardType == KESSHCMServices.HRMTaxCardType.Main)
                        {
                            result.Add(new Tax
                            {
                                Type = d.CardType,
                                TypeDescription = Enum.GetName(typeof(KESSHCMServices.HRMTaxCardType), d.CardType),
                                NPWP = d.NPWP,
                                EmployeeName = d.EmplName,
                                EmployeeID = d.EmplId,
                                AXID = d.RecId,
                            });
                        }
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

        public List<ElectronicAddress> GetElectronicAddresses(string employeeID)
        {
            var result = new List<ElectronicAddress>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var electronicAddresses = Client.getHCMLogisticsElectronicAddressAsync(Context, employeeID).GetAwaiter().GetResult().response;
                if (electronicAddresses.Length > 0)
                {
                    foreach (var d in electronicAddresses)
                    {                        
                        result.Add(new ElectronicAddress
                        {
                            Type = d.Type,
                            TypeDescription = Enum.GetName(typeof(KESSHCMServices.LogisticsElectronicAddressMethodType), d.Type),
                            Locator = d.Locator,
                            EmployeeName = d.EmplName,
                            EmployeeID = d.EmplId,
                            AXID = d.RecId,
                        });
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

        //public string Update(EmployeeResult employee, List<IdentificationResult> identifications, List<BankAccountResult> bankAccounts, List<FamilyResult> families, List<Tax> taxes, Address address, string reason)
        //{
        //    var newEmployee = new KESSHCMServices.HCMPersonDetails();
        //    var newFamilies = new List<KESSHCMServices.HCMPersonContact>();

        //    var emp = (employee.UpdateRequest != null) ? employee.UpdateRequest : employee.Employee;

        //    newEmployee.EmplId = emp.EmployeeID;
        //    newEmployee.BirthDate = emp.Birthdate;
        //    newEmployee.BirthPlace = emp.Birthplace;
        //    newEmployee.Email = emp.Email;            
        //    newEmployee.Person = emp.AXID;
        //    newEmployee.Phone = emp.NoTelp;
        //    newEmployee.Gender = (KESSHCMServices.HcmPersonGender)emp.Gender;
        //    newEmployee.MaritalStatus = (KESSHCMServices.HcmPersonMaritalStatus)emp.MaritalStatus;
        //    newEmployee.Religion = (KESSHCMServices.Religion)emp.Religion;

        //    // Update Bank Account
        //    var ba = bankAccounts.FirstOrDefault();
        //    if (ba != null)
        //        newEmployee.BankAccount = (ba.UpdateRequest != null) ? ba.UpdateRequest.AccountNumber : ba.BankAccount.AccountNumber;

        //    // Update Identifications 
        //    foreach (var identification in identifications) {
        //        var id = (identification.UpdateRequest != null) ? identification.UpdateRequest: identification.Identification;                

        //        switch (id.Type)
        //        {
        //            case "BPJS-NAKER":
        //                newEmployee.BPJS = id.Number;
        //                break;
        //            case "JAMSOSTEK":
        //                newEmployee.Jamsostek = id.Number;
        //                break;
        //            case "KK":
        //                newEmployee.KK = id.Number;
        //                break;
        //            case "NIAK":
        //                newEmployee.Koperasi = id.Number;
        //                break;
        //            case "KTP":
        //                newEmployee.KTP = id.Number;
        //                break;                   
        //            case "Passport":
        //                break;
        //            case "KITAS":
        //                break;
        //            case "Alien/Admission":                        
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    // Update Families
        //    foreach (var family in families)
        //    {
        //        var newFamily = new Family();
        //        if (family.UpdateRequest != null)
        //        {
        //            switch (family.UpdateRequest.Action)
        //            {
        //                case ActionType.Create:
        //                    family.UpdateRequest.AXID = 0;
        //                    newFamily = family.UpdateRequest;
        //                    break;
        //                case ActionType.Update:
        //                    newFamily = family.UpdateRequest;
        //                    break;
        //                case ActionType.Delete:
        //                    continue;
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //        else if (family.Family != null)
        //        {
        //            newFamily = family.Family;
        //        }
        //        else 
        //        {
        //            continue;
        //        }

        //        newFamilies.Add(new KESSHCMServices.HCMPersonContact
        //        {
        //            RecId = newFamily.AXID,
        //            BirthDate = newFamily.BirthDate,
        //            BirthPlace = newFamily.Birthplace,
        //            EmplId = emp.EmployeeID,                    
        //            KTP = newFamily.NIK,
        //            Person = newFamily.AXID,
        //            PersonName = newFamily.Name,
        //            RelationshipType = newFamily.Relationship,
        //            Religion = (KESSHCMServices.Religion)newFamily.Religion,
        //            Gender = (KESSHCMServices.HcmPersonGender)newFamily.Gender,
        //        });
        //    }

        //    long requestID = 0;
        //    var Client = this.GetClient();
        //    var Context = this.GetContext();
        //    //Client.OpenAsync().GetAwaiter();
        //    try
        //    {
        //        requestID = Client.newHCMWorkerRequest(Context, reason, newEmployee, newFamilies.ToArray());
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    finally 
        //    {
        //        if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
        //    }


        //    return requestID.ToString();
        //}                       

        //public UpdateRequestStatus CheckRequest(string requestID) {
        //    UpdateRequestStatus data;
        //    var Client = this.GetClient();
        //    var Context = this.GetContext();
        //    try
        //    {
        //        //Client.OpenAsync().GetAwaiter();
        //        data = (UpdateRequestStatus) Client.getHCMWorkerRequest(Context, Convert.ToInt64(requestID));
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    finally 
        //    {
        //        if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
        //    }            

        //    return data;
        //}

        public List<Certificate> GetCertificates(string employeeID, bool withTypeDescription = false) {
            var certificates = new List<Certificate>();
            var certificateTypes = new Dictionary<string, CertificateType>();
            var tasks = new List<Task<TaskRequest<object>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var result = new List<Certificate>();
                var Client = this.GetClient();
                var Context = this.GetContext();
                try
                {
                    //Client.OpenAsync().GetAwaiter();
                    var data = Client.getHCMPersonCertificateAsync(Context, employeeID).GetAwaiter().GetResult().response;
                    foreach (var d in data)
                    {
                        result.Add(new Certificate
                        {
                            AXID = d.RecId,
                            EmployeeID = d.EmplId,
                            EmployeeName = d.EmplName,
                            TypeID = d.CertificateTypeId,
                            TypeDescription = d.Description,
                            Validity = new DateRange(d.IssueDate, d.ExpirationDate),
                            ReqRenew = d.RequireRenewal == KESSHCMServices.NoYes.Yes,
                            Note = d.Note,
                            Filepath = d.DocumentPath,
                            Purpose = (KESSWRServices.KESSWorkerRequestPurpose)d.Purpose,
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

                return TaskRequest<object>.Create("certificate", result);
            }));

            if (withTypeDescription)
            {
                // Fetch data Type
                tasks.Add(Task.Run(() =>
                {
                    var result = new Dictionary<string, CertificateType>();
                    var Client = this.GetClient();
                    var Context = this.GetContext();

                    try
                    {
                        //Client.OpenAsync().GetAwaiter();
                        var data = Client.getHCMCertificateTypeAsync(Context).GetAwaiter().GetResult().response;
                        foreach (var d in data)
                        {
                            result[d.CertificateTypeId] = new CertificateType
                            {
                                AXID = d.RecId,
                                Description = d.Description,
                                TypeID = d.CertificateTypeId,
                                ReqRenew = d.RequireRenewal == KESSHCMServices.NoYes.Yes,
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

                    return TaskRequest<object>.Create("certificateType", result);
                }));
            }

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result) {
                    if (r.Label == "certificate") 
                    {
                        if (r.Result != null) certificates = (List<Certificate>)r.Result;
                    }
                    else 
                    {
                        if (r.Result != null) certificateTypes = (Dictionary<string, CertificateType>)r.Result; ;
                    }
                }

                // Fix certificate data
                foreach (var c in certificates)
                {
                    CertificateType certificateType;
                    if (certificateTypes.TryGetValue(c.TypeID, out certificateType)) {
                        c.TypeDescription = certificateType.Description;
                    }
                    
                }                
            }

            return certificates;    
        }

        public string UpdateCertificate(Certificate certificate)
        {
            return "";
        }

        public List<Family> GetFamilies(string employeeID)
        {
            var families = new List<Family>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMPersonContactAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                                        
                    families.Add(new Family
                    {
                        AXID = d.RecId,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Birthdate = d.BirthDate,
                        Birthplace = d.BirthPlace,
                        NIK = d.KTP,
                        Name = d.PersonName,
                        FirstName= d.PersonFirstName,
                        LastName = d.PersonLastName,
                        MiddleName=d.PersonMiddleName,
                        Relationship = d.RelationshipTypeId,
                        RelationshipDescription = d.RelationshipTypeDescription,
                        Filepath= d.DocumentPath,
                        Religion = (Religion)d.Religion,
                        Gender = (Gender)d.Gender,
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
                        
            return families;
        }

        public List<Employment> GetEmployments(string employeeID)
        {
            var employments = new List<Employment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMPositionWorkerAssignmentAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    employments.Add(new Employment
                    {
                        PositionID = d.PositionId,
                        AssigmentDate = new DateRange(d.AssigmentStart, d.AssigmentEnd),
                        Description = d.Description,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        PrimaryPosition = d.IsPrimary == KESSHCMServices.NoYes.Yes,
                        Position = d.PositionId,
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
                                  
            return employments;
        }

        public List<Document> GetDocuments(string employeeID)
        {
            var contacts = new List<Document>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMWorkerDocumentAsync(Context, employeeID).GetAwaiter().GetResult().response;

                foreach (var d in data)
                {
                    contacts.Add(new Document
                    {
                        AXID = d.RecId,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Description = d.Description,
                        DocumentType = d.DocumentType,
                        Filepath = d.FilenamePath,
                        Notes = d.Notes,
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
            
            return contacts;
        }

        public string UpdateDocument(Document document)
        {
            return "";
        }

        public List<Training> GetTrainings(string employeeID)
        {
            var trainings = new List<Training>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMPersonCourseAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                    
                    trainings.Add(new Training
                    {
                        TrainingID = d.CourseId,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Schedule = new DateRange(d.StartDate, d.EndDate),
                        Filepath=d.DocumentPath,
                        Location=d.Location,
                        AXID=d.RecId,
                        Description = d.Description,
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
                                  
            return trainings;
        }     

        public List<WarningLetter> GetWarningLetters(string employeeID) 
        {
            var letters = new List<WarningLetter>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMWorkerSPAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    letters.Add(new WarningLetter
                    {
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        Description = d.Description,
                        Schedule = new DateRange(d.DateStart, d.DateEnd),
                        CodeSP = d.KodeSP,
                        Worker = d.Worker,
                        Filepath = "",
                        Type = "",

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
                                  
            return letters;
        }

        public List<Application> GetApplications(string employeeID)
        {
            var applications = new List<Application>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMPersonApplicationAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    applications.Add(new Application
                    {
                        ApplicationID = d.ApplicationId,
                        EmployeeID = d.EmplId,
                        EmployeeName = d.EmplName,
                        JobID = d.JobId,
                        RecruitmentDescription = d.RecruitingDescription,
                        RecruitmentID = d.RecruitingId,                        
                        Schedule = new DateRange(d.RecruitingStartDate, d.RecruitingEndDate),
                        ApplicationStatus = (KESSHRMServices.HRMApplicationStatus)d.Status,                       

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
                                   
            return applications;
        }

        public List<Employee> GetSubordinate(string employeeID)
        {
            var employee = new List<Employee>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMWorkerSubordinateAsync(Context, employeeID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    employee.Add(new Employee
                    {
                        EmployeeID= d.EmplId,
                        EmployeeName= d.EmplName,
                        AXID= d.RecId,

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

            return employee;
        }

        public bool HasSubordinate(string employeeID)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHCMWorkerSubordinateAsync(Context, employeeID).GetAwaiter().GetResult().response;
                return data.Length > 0;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }            
        }

        public List<MedicalRecord> GetMedicalRecords(string employeeID)
        {
            var records = new List<MedicalRecord>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getEmplMedicalRecordAsync(Context, employeeID).GetAwaiter().GetResult().response;

                foreach (var d in data)
                {
                    var r = new MedicalRecord
                    {
                        AXID = d.RecId,
                        EmployeeID = d.EmplId,
                        Description = d.Description,
                        RecordDate = d.DocumentDate,
                        Notes = d.Notes,
                    };

                    r.Documents = new List<FieldAttachment>();
                    foreach (var document in d.DocumentList) {
                        r.Documents.Add(new FieldAttachment { 
                            Filepath= document.DocumentPath,
                            AXID=document.RecId,
                            Notes=document.Notes,
                        });
                    }
                    records.Add(r);
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

            return records;
        }

        public string UpdateDocumentRequest(DocumentRequest documentRequest)
        {
            return "";
        }

        public List<City> GetCities()
        {
            var result = new List<City>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getHCMLogisticsAddressCityAsync(Context).GetAwaiter().GetResult().response;                
                foreach (var city in data)
                {
                    result.Add(new City
                    {
                        AXID = city.RecId,
                        Name = city.Name,
                        Description = city.Description,
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
            
            return result;
        }

        public List<RelationshipType> GetRelationshipType()
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var result = new List<RelationshipType>();
                var data = Client.getHCMRelationshipTypeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data) {
                    result.Add(new RelationshipType {
                        AXID = d.RecId,
                        Description = d.Description,
                        TypeID=d.RelationshipTypeId,
                    });
                }
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
        }

        public List<CertificateType> GetCertificateType()
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var result = new List<CertificateType>();
                var data = Client.getHCMCertificateTypeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    result.Add(new CertificateType
                    {
                        AXID = d.RecId,
                        Description = d.Description,
                        TypeID = d.CertificateTypeId,
                        ReqRenew = d.RequireRenewal == KESSHCMServices.NoYes.Yes,
                    });
                }
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
        }

        public List<string> GetDocumentTypes()
        {
            return new List<string>(new string[]{
                "Passport",
                "KTP",
                "Ijazah",
                "Buku Nikah",
                "BPJS",
                "NPWP",
                "Passport",
                "Visa",
                "Kartu Asuransi",
            });
        }
        public List<string> GetDocumentRequestTypes()
        {
            return new List<string>(new string[]{
                "Surat Keterangan Penghasilan",
                "Surat Pengajuan Biaya Mitra (Koperasi)",
            });
        }       

    }
}
