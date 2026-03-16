using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class PeriodAverageDurationDto
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public double AverageDurationMinutes { get; set; }
        public int TotalBookings { get; set; }
    }
}
