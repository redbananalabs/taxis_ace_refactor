using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class VehicleTypeCountDto
    {
        public VehicleType VehicleType { get; set; }
        public string VehicleTypeText { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
