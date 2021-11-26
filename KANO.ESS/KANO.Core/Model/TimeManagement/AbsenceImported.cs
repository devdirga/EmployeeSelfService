using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class AbsenceImported
    {
        public string EmplIdField { get; set; }
        public string EmplNameField { get; set; }        
        public string InOutField { get; set; }
        public DateTime Clock { get; set; }
        public DateTime PresenceDateField;
        public long RecIdField;
    }

    public class AbsenceInOut
    {
        public string EmplIdField { get; set; }
        public string EmplNameField { get; set; }
        public string InOutField { get; set; }
        public int Clock { get; set; }
        public DateTime PresenceDateField;
        public long RecIdField;
        public string TermNo { get; set; }
    }

    public class AbsenceInOutImported
    {
        public string InOut { get; set; }
        public string EntityID { get; set; }
        public string ActivityTypeID { get; set; }
        public string LocationID { get; set; }
        public Double Longitude { get; set; }
        public Double Latitude { get; set; }

    }
}
