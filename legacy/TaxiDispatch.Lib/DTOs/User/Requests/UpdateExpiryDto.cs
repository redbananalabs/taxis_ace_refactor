using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User.Requests
{
    public class UpdateExpiryDto : ModelValidator
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public DocumentType DocType { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
