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
    public class TravelAdapter
    {
        private readonly Credential credential;

        public TravelAdapter(IConfiguration config)
        {
            credential = Tools.AXConfiguration(config);
        }

        public KESSTEServices.KESSTEEServiceClient GetClient()
        {
            var Client = new KESSTEServices.KESSTEEServiceClient();
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

        public KESSTEServices.CallContext GetContext()
        {
            return new KESSTEServices.CallContext()
            {
                Company = credential.Company
            };
        }

        public KESSWRServices.KESSWRServiceClient GetClientWR()
        {
            var Client = new KESSWRServices.KESSWRServiceClient();
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

        public KESSWRServices.CallContext GetContextWR()
        {
            return new KESSWRServices.CallContext()
            {
                Company = credential.Company
            };
        }

        public  List<Travel> GetReport(string employeeID, DateRange range) {            
            var travels = new List<Travel>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            var data =  Client.getTEExpenseReportAsync(Context, employeeID).GetAwaiter().GetResult().response;
            foreach (var d in data)
            {
                travels.Add(new Travel
                {
                    // not yet
                });
            }
            return travels;
        }

        public List<TravelPurpose> GetTravelPurposes()
        {
            var travelpurpose = new List<TravelPurpose>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getTETrvExpPurposeAsync(Context).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    travelpurpose.Add(new TravelPurpose
                    {
                        AXID = d.RecId,
                        Description = d.Description,
                        IsOverseas = false,
                        PurposeID = d.TrvExpTDId
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

            return travelpurpose;
        }

        public List<Transportation> GetTransportations()
        {
            var transportasions = new List<Transportation>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var data = Client.getTETrvExpTransportasiAsync(Context).GetAwaiter().GetResult().response;
                sw.Stop();
                Console.WriteLine($"Transportation {sw.ElapsedMilliseconds}");
                sw.Start();
                foreach (var d in data)
                {
                    transportasions.Add(new Transportation
                    {
                        AXID = d.RecId,
                        Description = d.Description,
                        TransportationID = d.TransportId
                    });
                }
                sw.Stop();
                Console.WriteLine($"Transportation List {sw.ElapsedMilliseconds}");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return transportasions;
        }

        public List<SPPD> GetSPPD(string SPPDID)
        {
            List<SPPD> sppd = new List<SPPD>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                sppd = this.GetSPPD(SPPDID, Client, Context);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return sppd;
        }

        public List<SPPD> GetSPPD(string SPPDID, KESSTEServices.KESSTEEServiceClient Client, KESSTEServices.CallContext Context, bool noTravel = false)
        {
            List<SPPD> sppd = new List<SPPD>();            
            try
            {
                var tasks = new List<Task<TaskRequest<object>>>();

                if (noTravel)
                {
                    // Fetch data from Transportation
                    tasks.Add(Task.Run(() =>
                    {
                        var data = Client.getTETrvExpTicketTransportDetailAsync(Context, SPPDID).GetAwaiter().GetResult().response;
                        return TaskRequest<object>.Create("Transportation", data);
                    }));
                }

                // Fetch data from SPPD
                tasks.Add(Task.Run(() => {                    
                    var data = Client.getTESPPDAsync(Context, SPPDID).GetAwaiter().GetResult().response;                    
                    return TaskRequest<object>.Create("SPPD", data);
                }));

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
                    var transportationDetails = new List<TransportationDetail>();
                    var SPPDAXs = new List<KESSTEServices.TESPPD>();

                    foreach (var r in t.Result) { 
                        if (r.Label == "Transportation")
                        {
                            foreach (var td in (KESSTEServices.TETicketTransportDetail[])r.Result)
                            {
                                transportationDetails.Add(this.mapFromAX(td));
                            }
                        }
                        else
                        {
                            SPPDAXs = new List<KESSTEServices.TESPPD>((KESSTEServices.TESPPD[])r.Result);
                        }
                    }

                    foreach (var d in SPPDAXs)
                    {
                        sppd.Add(this.mapFromAX(d, transportationDetails));
                    }

                }                                             
            }
            catch (Exception)
            {
                throw;
            }           

            return sppd;
        }

        public List<Travel> GetTravel(string employeeID, DateRange dateRange)
        {
            List<Travel> travel = new List<Travel>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                var rangeFilter = Tools.normalizeFilter(dateRange);
                var data = Client.getTETravelRequestListAsync(Context, employeeID, rangeFilter.Start, rangeFilter.Finish).GetAwaiter().GetResult().response;                

                var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(data, options, (currentData) =>
                {
                    var sppds = new List<SPPD>();
                    if (currentData.TravelReqStatus >= KESSTEServices.KESSTrvExpTravelReqStatus.Verified)
                    {                        
                        sppds = this.GetSPPD(currentData.SPPDId, Client, Context);                     
                    }


                    travel.Add(this.mapFromAX(currentData, sppds));
                });

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return travel;
        }

        public List<Travel> GetTravelAgenda(string employeeID, DateRange dateRange)
        {
            List<Travel> travel = new List<Travel>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var rangeFilter = Tools.normalizeFilter(dateRange);
                var data = Client.getTETravelRequestListAsync(Context, employeeID, rangeFilter.Start, rangeFilter.Finish).GetAwaiter().GetResult().response;

                var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(data, options, (currentData) =>
                {
                    var sppds = new List<SPPD>();
                    if (currentData.TravelReqStatus >= KESSTEServices.KESSTrvExpTravelReqStatus.Verified)
                    {
                        sppds = this.GetSPPD(currentData.SPPDId, Client, Context, true);
                    }


                    travel.Add(this.mapFromAX(currentData, sppds));
                });

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return travel;
        }

        public Travel GetTravelByRecId (long AXID)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            var travel = new Travel();
            try
            {
                var data = Client.getTETravelRequestRecIdAsync(Context, AXID).GetAwaiter().GetResult().response;
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(data, options, (currentData) => {
                    var sppds = this.GetSPPD(currentData.SPPDId, Client, Context);

                    travel = this.mapFromAX(currentData, sppds);
                });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return travel;

        }

        public Travel GetTravelByTravelRequestID(string employeeID, string travelRequestID, bool travelOnly = false)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            var travel = new Travel();
            try
            {
                var data = Client.getTETravelRequestAsync(Context, employeeID, travelRequestID).GetAwaiter().GetResult().response;
                if (travelOnly) {
                    return (data.Length > 0) ? this.mapFromAX(data[0], new List<SPPD>()) : null;
                }

                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(data, options, (currentData) => {
                    var sppds = this.GetSPPD(currentData.SPPDId, Client, Context);

                    travel = this.mapFromAX(currentData, sppds);
                    return;
                });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return travel;

        }

        public List<Travel> GetTravelByStatus(string employeeID, KESSTEServices.KESSTrvExpTravelReqStatus status = KESSTEServices.KESSTrvExpTravelReqStatus.Verified)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            var travel = new List<Travel>();
            try
            {
                var data = Client.getTETravelRequestFilterStatusAsync(Context, employeeID, status).GetAwaiter().GetResult().response;
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(data, options, (currentData) => {
                    var sppds = this.GetSPPD(currentData.SPPDId, Client, Context, true);
                    travel.Add(this.mapFromAX(currentData, sppds));
                });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            travel.Sort((x, y) => x.Schedule.Start.CompareTo(y.Schedule.Start));
            return travel;

        }

        public static List<string> GetTravelStatus()
        {
            return new List<string>(Enum.GetNames(typeof(KESSTEServices.KESSTrvExpTravelReqStatus)));
        }

        public static List<string> GetTravelType()
        {
            return new List<string>( Enum.GetNames(typeof(TravelType)));
        }

        private TransportationDetail mapFromAX(KESSTEServices.TETicketTransportDetail detail)
        {
            var attachments = new List<FieldAttachment>();

            foreach (var document in detail.DocumentList)
            {
                attachments.Add(new FieldAttachment
                {
                    AXID = document.RecId,
                    Filepath = Path.Combine(document.DocumentPath),
                });
            }
            
            return new TransportationDetail
            {
                BookingCode=detail.BookingCode,
                BookingDate=detail.BookingDate,
                AXID=detail.RecId,
                TicketPrice=Decimal.ToDouble(detail.TicketPrice),
                TransportationID=detail.TransportId,
                VendorAccountID=detail.VendAccount,
                Attachments = attachments
            };
        }

        private SPPD mapFromAX(KESSTEServices.TESPPD AXSPPD, List<TransportationDetail> transportationDetails) {
            var attachments = new List<FieldAttachment>();

            foreach (var document in AXSPPD.DocumentList)
            {
                attachments.Add(new FieldAttachment
                {
                    AXID = document.RecId,
                    Filepath = Path.Combine(document.DocumentPath),
                });
            }

            Boolean isThereItenary = false;
            for(int i=0; i< transportationDetails.Count; i++)
            {
                for(int j=0;j< transportationDetails[i].Attachments.Count; j++)
                {
                    isThereItenary = true;
                }
            }
            
            return new SPPD
            {
                SPPDID = AXSPPD.SPPDId,
                Accommodation = AXSPPD.Akomodasi,
                Fuel = AXSPPD.BBM,
                Grade=AXSPPD.GradePosition,
                Position= AXSPPD.Position,
                EmployeeID = AXSPPD.EmplId,
                Laundry = AXSPPD.Laundry,
                Parking = AXSPPD.Parkir,
                AXID = AXSPPD.RecId,
                AXRequestID = AXSPPD.InstanceId,
                Rent = AXSPPD.SewaKendaraan,
                Status = AXSPPD.SPPDStatus,
                Start = AXSPPD.TglBerangkat,
                End = AXSPPD.TglKembali,
                Ticket = AXSPPD.TiketTransport,
                Highway = AXSPPD.Tol,
                EmployeeName=AXSPPD.EmplName,                
                AirportTransportation = AXSPPD.TransportBandara,
                LocalTransportation = AXSPPD.TransportLokal,
                MealAllowance = AXSPPD.UangMakan,
                PocketMoney = AXSPPD.UangSaku,
                Attachments = attachments,     
                TransportationDetails = transportationDetails,
                IsAttachmentExist = isThereItenary,
                SPPDNumber = AXSPPD.SPPDNumber
            };
        }

        private Travel mapFromAX(KESSTEServices.TETravelRequest AXTravel, List<SPPD> SPPDs) {
            var startDate = Helper.secondsToDateTime(Tools.normalize(AXTravel.TglMulai), AXTravel.JamBerangkat);
            var endDate = Helper.secondsToDateTime(Tools.normalize(AXTravel.TglAkhir), AXTravel.JamAkhir);

            var attachments = new List<FieldAttachment>();

            foreach (var document in AXTravel.DocumentList)
            {
                attachments.Add(new FieldAttachment
                {
                    AXID = document.RecId,
                    Filepath = Path.Combine(document.DocumentPath, document.DocuName),
                    Filename = document.DocuName
                });
            }

            if (!string.IsNullOrWhiteSpace(AXTravel.DocumentPath))
            {
                attachments.Add(new FieldAttachment
                {
                    AXID = 0,
                    Filepath = AXTravel.DocumentPath
                });
            }

            return new Travel
            {
                AXID = AXTravel.RecId,
                AXRequestID = AXTravel.InstanceId,
                TravelID = AXTravel.TravelReqId,
                Origin = AXTravel.AsalKeberangkatan,
                Destination = AXTravel.TujuanKeberangkatan,
                Schedule = new DateRange { Start = startDate, Finish = endDate },
                Filepath= AXTravel.DocumentPath,
                TravelPurpose = AXTravel.TujuanDinas,
                Transportation = AXTravel.Transportasi,
                TransactionDate = AXTravel.CreatedDate,
                Description = AXTravel.Note,
                SPPD = SPPDs,
                EmployeeID = AXTravel.EmplId,
                CreatedBy = AXTravel.CreatedBy,
                VerifiedBy = AXTravel.VerifiedBy,
                CanceledBy = AXTravel.CanceledBy,
                ClosedDate = AXTravel.ClosedDate,
                CanceledDate = AXTravel.CanceledDate,
                VerifiedDate = AXTravel.VerifiedDate,
                TravelRequestStatus = AXTravel.TravelReqStatus,
                Note = AXTravel.Note,
                NoteRevision = AXTravel.NoteRevision,
                RevisionBy = AXTravel.RevisionBy,
                RevisionDate = AXTravel.RevisionDate,
                DocumentList  = attachments
            };
        }

        public Travel GetTravelByInstanceID(string employeeID, string instanceID, bool travelOnly = false)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            var travel = new Travel();
            try
            {
                var data = Client.getTETravelRequestByInstanceIdAsync(Context, employeeID, instanceID).GetAwaiter().GetResult().response;
                if (travelOnly)
                {
                    return (data.Length > 0) ? this.mapFromAX(data[0], new List<SPPD>()) : null;
                }

                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(data, options, (currentData) => {
                    var sppds = this.GetSPPD(currentData.SPPDId, Client, Context);

                    travel = this.mapFromAX(currentData, sppds);
                    return;
                });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return travel;

        }

        public Travel GetTravelByAxID(string axID, bool travelOnly = false)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            var travel = new Travel();
            try
            {
                var data = Client.getTETravelRequestRecIdAsync(Context, long.Parse(axID)).GetAwaiter().GetResult().response;
                if (travelOnly)
                {
                    return (data.Length > 0) ? this.mapFromAX(data[0], new List<SPPD>()) : null;
                }

                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(data, options, (currentData) => {
                    var sppds = this.GetSPPD(currentData.SPPDId, Client, Context);

                    travel = this.mapFromAX(currentData, sppds);
                    return;
                });

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return travel;

        }
    }
}
