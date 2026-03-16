using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Modules.Membership;

public partial class AppRefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; }

    public string JwtId { get; set; }

    public bool IsUsed { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ExpiryDate { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; }
}
