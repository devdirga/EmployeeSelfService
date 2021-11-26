using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class MedicalRecord: BaseUpdateRequest
    {
        public DateTime RecordDate { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public List<FieldAttachment> Documents{ get; set; }
    }
}
