using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.LocalPOI
{
    public class DeletePOIRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
