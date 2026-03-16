using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class CompanyConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string CompanyName { get; set; } // Ace Taxis (Dorset) Ltd
        public string Address1 { get; set; } // 1 Briar Close
        public string Address2 { get; set; } // Gillingham
        public string Address3 { get; set; } // Dorset
        public string? Address4 { get; set; }
        public string Postcode { get; set; } // SP8 4SS

        public string Email { get; set; } // bookings@acetaxisdorset.co.uk
        public string Website { get; set; } // www.acetaxisdorset.co.uk
        public string Phone { get; set; } // 01747

        public string CompanyNumber { get; set; } // 08920947
        public string VATNumber { get; set; } // 325 1273 31

        public double CardTopupRate { get; set; }

        public string RevoluttSecretKey { get; set; }

        public List<string> BrowserFCMs { get; set; }

        public bool AddVatOnCardPayments { get; set; } = false;
    }
}

