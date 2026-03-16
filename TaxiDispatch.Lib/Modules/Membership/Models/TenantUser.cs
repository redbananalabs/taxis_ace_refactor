using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Modules.Membership;

public class TenantUser
{
    [Key]
    public int UserId { get; set; }

    [Required]
    public MembershipRole Role { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsDeleted { get; set; }

    public DateTime? LastLoginUtc { get; set; }

    [Required]
    public DateTime CreatedUtc { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime? UpdatedUtc { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
