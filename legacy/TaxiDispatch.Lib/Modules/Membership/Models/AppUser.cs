using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Membership;

public partial class AppUser : IdentityUser<int>
{
    [MinLength(3), MaxLength(50)]
    public string? FullName { get; set; }
}
