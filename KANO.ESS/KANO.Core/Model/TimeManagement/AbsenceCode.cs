using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class AbsenceCode
    {
        public string DescriptionField { get; set; }
        public string GroupIdField { get; set; }
        public string IdField { get; set; }
        public bool IsEditable { get; set; }
        public bool IsAttachment { get; set; }
        public bool IsOnList { get; set; }
    }
}
