using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class SPPD
    {
        public string SPPDID { get; set; }
        [JsonIgnore]
        public Decimal Accommodation { get; set; }
        public Decimal Fuel { get; set; }
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Grade { get; set; }
        public string Position { get; set; }
        public Decimal Laundry { get; set; }
        public Decimal Parking { get; set; }
        public long AXID { get; set; }
        public string AXRequestID { get; set; }
        public Decimal Rent { get; set; }
        public KESSTEServices.KESSTrvExpSPPDStatus Status { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public Decimal Ticket { get; set; }
        public Decimal Highway { get; set; }
        public Decimal AirportTransportation { get; set; }
        public Decimal LocalTransportation { get; set; }
        public Decimal MealAllowance { get; set; }
        public Decimal PocketMoney { get; set; }
        public List<TransportationDetail> TransportationDetails { get; set; }
        public List<FieldAttachment> Attachments { get; set; }
        public Boolean IsAttachmentExist { get; set; }
        public string SPPDNumber { get; set; }
    }
}
