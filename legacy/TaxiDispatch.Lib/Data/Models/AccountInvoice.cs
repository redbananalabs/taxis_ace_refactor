using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TaxiDispatch.Data.Models
{
    public class AccountInvoice
    {
        public AccountInvoice() 
        {

        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Precision(18, 2)]
        public decimal VatTotal { get; set; }

        [Required]
        [Precision(18, 2)]
        public decimal NetTotal { get; set; }

        [Required]
        [Precision(18, 2)]
        public decimal Total { get; set; }

        [Required]
        public int NumberOfJourneys { get; set; }

        [Required]
        public bool Paid { get; set; }
        
        [Required]
        public bool Cancelled { get; set; }
        
        [Required]
        public int AccountNo { get; set; }

        public string? PurchaseOrderNo { get; set; }
        public string? Reference { get; set; }
    }
}

