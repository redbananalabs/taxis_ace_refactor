using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class CreditJourneysRequestDto
    {
        public int InvoiceNumber { get; set; }
        public string Reason { get; set; }
        public int[] BookingIds { get; set; }
    }
}
