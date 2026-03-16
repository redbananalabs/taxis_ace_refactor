using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class PayoutByMonthDto
    {
        public string Month { get; set; }
        public decimal TotalPaymentDue { get; set; }
    }
}
