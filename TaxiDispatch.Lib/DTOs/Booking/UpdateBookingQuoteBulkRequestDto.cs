using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class UpdateBookingQuoteBulkRequestDto : UpdateBookingQuoteRequestDto
    {
        public int[] BookingIds { get; set; }
    }
}
