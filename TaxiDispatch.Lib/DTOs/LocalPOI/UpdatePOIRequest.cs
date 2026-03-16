using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.LocalPOI
{
    public class UpdatePOIRequest : CreatePOIRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
