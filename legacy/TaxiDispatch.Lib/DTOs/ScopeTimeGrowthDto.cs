using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class ScopeTimeGrowthDto
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public BookingScope Scope { get; set; }
        public string ScopeText { get; set; } = string.Empty;
        public int CurrentYearCount { get; set; }
        public int PreviousYearCount { get; set; }
        public double PercentageGrowth { get; set; }
    }
}
