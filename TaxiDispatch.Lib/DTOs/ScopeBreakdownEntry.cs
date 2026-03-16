using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class ScopeBreakdownEntry
    {
        public string PeriodLabel { get; set; } = string.Empty; // e.g., "2025-04-01", "Week 14", "2025-Q1"
        public BookingScope Scope { get; set; }
        public string ScopeText { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsComparison { get; set; } = false;
    }
}
