using TaxiDispatch.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class DriverExpenseDto : ModelValidator
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public ExpenseCategory Category { get; set; }

        public string? Description { get; set; }

        [Required]
        [Precision(18, 2)]
        public decimal Amount { get; set; }
    }
}
