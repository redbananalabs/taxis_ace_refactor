using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class ProfitabilityDto
    {
        public int InvoiceNumber { get; set; }
        public int AccountNo { get; set; }
        public DateTime Date { get; set; }
        public decimal NetTotal { get; set; }
        public decimal Cost { get; set; }
        public double Profit { get; set; }
        public double Margin { get; set; }
    }
}
