using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class PickupPostcodeDto
    {
        public string PickupPostCode { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
