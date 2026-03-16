using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class CreateGeoFenceDto : ModelValidator
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public List<LatLong> Data { get; set; }

        public int Points { get; set; }
        public string Area { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}

