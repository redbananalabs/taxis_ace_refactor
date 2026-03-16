using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TaxiDispatch.Domain;

namespace TaxiDispatch.Data.Models
{
    public class DriverExpense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public ExpenseCategory Category { get; set; }

        public string? Description { get; set; }

        [Required]
        [Precision(18,2)]
        public decimal Amount { get; set; }
        
    }
}

