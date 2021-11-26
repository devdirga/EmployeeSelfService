using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class WarningLetter:BaseDocumentVerification
    {
        public DateRange Schedule { get; set; }
        public string Worker { get; set; }
        public string Description { get; set; }
        public string CodeSP { get; set; }
        public string Type { get; set; }
    }
}
