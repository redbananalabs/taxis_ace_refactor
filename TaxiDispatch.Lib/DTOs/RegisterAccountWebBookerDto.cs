using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class RegisterAccountWebBookerDto : ModelValidator
    {
        [Required]
        public int Accno { get; set; }
        [Required]
        public string BookerName { get; set; }
        [Required]
        public string BookerEmail { get; set; }
        [Required]
        public string BookerPhone { get; set; }
    }
}
