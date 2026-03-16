using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class RevenueByMonthDto
    {
        public string Month { get; set; }
        public decimal NetTotal { get; set; }
    }
}
