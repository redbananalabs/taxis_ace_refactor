using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User.Requests
{
    public class UserUpdateDetailsRequestDto
    {
        [Required, MinLength(2), MaxLength(30)]
        public string FullName { get; set; }

        public string RegNo { get; set; }
    }
}
