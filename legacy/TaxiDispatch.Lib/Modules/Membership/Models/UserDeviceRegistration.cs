using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Modules.Membership;

public class UserDeviceRegistration
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public UserDeviceType DeviceType { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedUtc { get; set; }

    public DateTime? UpdatedUtc { get; set; }

    [StringLength(100)]
    public string? DeviceName { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
