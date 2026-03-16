using TaxiDispatch.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Modules.Membership;

public class AccountUserLink
{
    [Key]
    public int UserId { get; set; }

    [Required]
    public int AccNo { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;

    [ForeignKey(nameof(AccNo))]
    public virtual Account Account { get; set; } = null!;
}
