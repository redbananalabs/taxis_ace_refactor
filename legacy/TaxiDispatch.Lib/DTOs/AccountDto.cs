using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class AccountDto : ModelValidator
    {
        public int AccNo { get; set; }

        [Required]
        public string ContactName { get; set; }

        [Required]
        public string BusinessName { get; set; }
        [Required]
        public string Address1 { get; set; }
        [Required]
        public string Address2 { get; set; }

        public string Address3 { get; set; } = string.Empty;
        public string Address4 { get; set; } = string.Empty;

        [Required]
        public string Postcode { get; set; }

        [Required]
        public string Telephone { get; set; }

        public string? PurchaseOrderNo { get; set; }
        public string? Reference { get; set; }

        [Required]
        public string Email { get; set; }

        public string BookerEmail { get; set; } = string.Empty;
        public string BookerName { get; set; } = string.Empty;

        public int? AccountTariffId { get; set; }
    }
}
