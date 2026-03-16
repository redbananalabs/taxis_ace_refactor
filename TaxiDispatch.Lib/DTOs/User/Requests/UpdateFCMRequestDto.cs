using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User.Requests
{
    public class UpdateFCMRequestDto
    {
        [Required]
        public string fcm { get; set; }
    }
}

