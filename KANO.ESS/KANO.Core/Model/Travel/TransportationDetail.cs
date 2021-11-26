using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class TransportationDetail
    {
        public string BookingCode { set; get; }
        public DateTime BookingDate { set; get; }
        public long AXID { set; get; }
        public double TicketPrice { set; get; }
        public string TransportationID { set; get; }
        public string TransportationDescription { set; get; }
        public string VendorAccountID { set; get; }
        public List<FieldAttachment> Attachments { get; set; }

    }
}
