using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User.Requests
{
    public class UserLoginRequestDto
    {
        [Required, MinLength(3), MaxLength(30)]
        public string Username { get; set; }

        [Required, MinLength(3), MaxLength(30)]
        public string Password { get; set; }
    }
}
