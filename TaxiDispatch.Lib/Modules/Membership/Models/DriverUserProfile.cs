using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Modules.Membership;

public class DriverUserProfile
{
    [Key]
    public int UserId { get; set; }

    [StringLength(20)]
    public string? RegNo { get; set; }

    [StringLength(12)]
    public string? ColorCodeRgb { get; set; }

    [Precision(10, 7)]
    public decimal? Longitude { get; set; }

    [Precision(9, 7)]
    public decimal? Latitude { get; set; }

    [Precision(6, 3)]
    public decimal? Heading { get; set; }

    [Precision(6, 2)]
    public decimal? Speed { get; set; }

    public DateTime? GpsLastUpdatedUtc { get; set; }

    [StringLength(20)]
    public string? VehicleMake { get; set; }

    [StringLength(30)]
    public string? VehicleModel { get; set; }

    [StringLength(20)]
    public string? VehicleColour { get; set; }

    public bool ShowAllBookings { get; set; }

    public bool ShowHvsBookings { get; set; }

    public int CashCommissionRate { get; set; }

    [Required]
    public VehicleType VehicleType { get; set; } = VehicleType.Unknown;

    [Required]
    public bool NonAce { get; set; }

    public SendMessageOfType CommsPlatform { get; set; } = SendMessageOfType.WhatsApp;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
