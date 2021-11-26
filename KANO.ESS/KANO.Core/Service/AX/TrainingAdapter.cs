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
    public class TrainingAdapter  
    {
        private IConfiguration Configuration;
        private Credential credential;

        public TrainingAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(Configuration);
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

        public List<Training> GetS(DateRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));

            var training = new List<Training>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseAsync(Context, range.Start, range.Finish).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                    
                    training.Add(this.mapFromAX(d));
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

            return training;
        }

        public List<Training> GetBasicS(DateRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));
            var _range = Tools.normalizeFilter(range);
            var training = new List<Training>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseTypeBasicAsync(Context,_range.Start, _range.Finish).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    training.Add(this.mapFromAX(d));
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

            return training;
        }

        public List<Training> GetHistory(string employeeID, bool withRegistrationInfo = false)
        {        
            var trainings = new List<Training>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseByEmplAsync(Context,employeeID).GetAwaiter().GetResult().response;
                var trainingData = data.GroupBy(x => x.CourseId).Select(x => x.First());
                if (withRegistrationInfo)

                {
                    var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
                    Parallel.ForEach(trainingData, options, (currentData) =>
                    {

                        var training = this.mapFromAX(currentData);
                        training.TrainingRegistration = this.GetRegistration(employeeID, currentData.CourseId);
                        trainings.Add(training);
                    });
                }
                else
                {
                    foreach (var d in trainingData)
                    {
                        trainings.Add(this.mapFromAX(d));
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

            return trainings;
        }

        public List<TrainingSubType> GetSubTypes()
        {
            var trainingType = new List<TrainingSubType>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseSubTypeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                           
                    trainingType.Add(new TrainingSubType{
                        SubType=d.CourseSubType,
                        TypeID=d.CourseTypeId,
                        Description=d.Description,
                        AXID=d.RecId,
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

            return trainingType;
        }

        public List<TrainingType> GetTypes()
        {
            var trainingType = new List<TrainingType>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseTypeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                           
                    trainingType.Add(new TrainingType{
                        MinAttendees=d.CourseMinAttendess,
                        TypeGroup=d.CourseTypeGroup,
                        TypeID=d.CourseTypeId,
                        Description=d.Description,
                        NumberOfDays=d.NumberOfDays,
                        AXID=d.RecId,
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

            return trainingType;
        }

        public TrainingRegistration GetRegistration(string employeeID, string trainingID)
        {
            TrainingRegistration registrationStatus = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMStatusParticipantAsync(Context,employeeID,trainingID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {                                                                                                                
                    return new TrainingRegistration{
                        RegistrationStatus=d.CourseAttendeeStatus,
                        TrainingID=d.CourseId,
                        RegistrationDate=d.CourseRegistrationDate,
                        EmployeeID=d.EmplId,
                        AXID=d.RecId,
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

            return registrationStatus;
        }

        public Training Get(string trainingID)
        {
            var training = new Training();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getHRMCourseByIdAsync(Context, trainingID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                   return this.mapFromAX(d);
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

            return training;
        }

        public long Register(TrainingRegistration registration)
        {
            //void benefit = null;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                //var data = Client.getClaimReimbursementByRecIdAsync(Context, AXID).GetAwaiter().GetResult().response;
                //foreach (var d in data)
                //{
                //    benefit = this.mapToAX(d);
                //    break;
                //}
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return Tools.RandomInt();
        }

        private Training mapFromAX( KESSHRMServices.HRMCourse data){
            return new Training{
                TermAndCondition=data.TermCondition,
                Event=data.CourseEvent,
                TrainingID=data.CourseId,
                Location=data.CourseLocationId,
                MaxAttendees=data.CourseMaxAttendess,
                MinAttendees=data.CourseMinAttendess,
                Room=data.CourseRoomId,
                TrainingStatus=data.CourseStatus,
                TypeID=data.CourseTypeRecId.ToString(),
                SubTypeID=data.CourseSubType,
                Vendor=data.CourseVendor,
                Description=data.Description,
                Name=data.Description,
                Schedule=new DateRange(data.StartDateTime, data.EndDateTime),
                RegistrationDeadline=data.LastDateOfSignUp,
                AXID=data.RecId,
            };
        }
        
    }
}
