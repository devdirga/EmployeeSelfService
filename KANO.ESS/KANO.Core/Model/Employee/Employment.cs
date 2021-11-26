using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class Employment:BaseUpdateRequest
    {
        public DateRange AssigmentDate { get; set; }
        public string PositionID { get; set; }
        public bool PrimaryPosition { get; set; }
        public string Position { get; set; }
        public string Description { get; set; }
    }
}
