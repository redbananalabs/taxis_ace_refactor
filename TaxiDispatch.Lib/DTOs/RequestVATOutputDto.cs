using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class RequestVATOutputDto : ModelValidator
    {
        [Required]
        public DateTime Start { get; set; }
        [Required]
        public DateTime End { get; set; }
    }
}
