using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class Installment
    {
        public string LoanID { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public double Instalment { get; set; }
        public double Balance { get; set; }
        public DateRange LoanSchedule { get; set; }
    }
}
