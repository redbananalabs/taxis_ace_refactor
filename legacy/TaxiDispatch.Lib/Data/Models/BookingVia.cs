using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace TaxiDispatch.Data.Models
{
    public class BookingVia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [MaxLength(100)]
        public string Address { get; set; } = string.Empty;

        [StringLength(9, ErrorMessage = "Postcode should not exceed 9 characters")]
        [MaxLength(9)]
        public string PostCode { get; set; } = string.Empty;

        [Required]
        [DefaultValue(1)]
        public int ViaSequence { get; set; }

        [IgnoreDataMember]
        [SwaggerIgnoreProperty]
        public virtual Booking? Booking { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

