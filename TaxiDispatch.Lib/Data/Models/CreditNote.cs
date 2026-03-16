using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class CreditNote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
        public string AccountNo { get; set; }
        
        public int InvoiceNumber { get; set; }
        
        public DateTime InvoiceDate { get; set; }
        
        public string Reason { get; set; }
    }
}

