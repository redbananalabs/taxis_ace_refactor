using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class GetPriceRequestDto : ModelValidator
    {
        [Required]
        public string PickupPostcode { get; set; }
        
        public List<string>? ViaPostcodes { get; set; }

        [Required]
        public string DestinationPostcode { get; set; }

        [Required]
        public DateTime PickupDateTime { get; set; }

        [Required]
        public int Passengers { get; set; }

        [Required]
        public bool PriceFromBase { get; set; }

        public int AccountNo { get; set; }
    }

    /// <summary>
    /// Used from back end admin panel
    /// </summary>
}
