using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.LocalPOI
{
    public class CreatePOIRequest : ModelValidator
    {
        public string? Name { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Postcode { get; set; }

        [Required]
        public LocalPOIType Type { get; set; }
    }
}
