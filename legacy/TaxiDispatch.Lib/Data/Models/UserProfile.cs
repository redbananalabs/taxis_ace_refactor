using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public partial class UserProfile
    {
        public UserProfile()
        {
            VehicleType = VehicleType.Unknown;
        }

        [PersonalData, StringLength(20)]
        public string? RegNo { get; set; }

        [PersonalData, StringLength(12)]
        public string? ColorCodeRGB { get; set; }

        [PersonalData, StringLength(500)]
        public string? NotificationFCM { get; set; }

        [PersonalData, StringLength(500)]
        public string? ChromeFCM { get; set; }

        // GPS
        [PersonalData]
        [Precision(10, 7)]
        public decimal? Longitude { get; set; }

        [PersonalData]
        [Precision(9, 7)]
        public decimal? Latitude { get; set; }

        [PersonalData]
        [Precision(6, 3)]
        public decimal? Heading { get; set; }

        [PersonalData]
        [Precision(6, 2)]
        public decimal? Speed { get; set; }

        [PersonalData]
        public DateTime? GpsLastUpdated { get; set; }
        // GPS

        [PersonalData, StringLength(20)]
        public string? VehicleMake { get; set; }
        [PersonalData, StringLength(30)]
        public string? VehicleModel { get; set; }
        [PersonalData, StringLength(20)]
        public string? VehicleColour { get; set; }

        public bool ShowAllBookings { get; set; }
        public bool ShowHVSBookings { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? LastLogin { get; set; }

        public int CashCommissionRate { get; set; }

        [Required]
        public VehicleType VehicleType { get; set; }

        [Required]
        public bool NonAce { get; set; }

        public SendMessageOfType CommsPlatform { get; set; } = SendMessageOfType.WhatsApp;

        [Key]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser User { get; set; }

    }
}



