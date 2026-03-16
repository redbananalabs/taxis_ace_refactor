using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace TaxiDispatch.Data.Models
{
    public class DriverInvoiceStatement
    {
        public DriverInvoiceStatement()
        {
            DateCreated = DateTime.Now.ToUKTime();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StatementId { get; set; }

        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }

        [IgnoreDataMember]
        public int TotalJobCount { get { return AccountJobsTotalCount + CashJobsTotalCount; } }

        [Required]
        public int AccountJobsTotalCount { get; set; }

        [Required]
        public int CashJobsTotalCount { get; set; }

        [Required]
        public int RankJobsTotalCount { get; set; }

        [Required]
        public double EarningsAccount { get; set; }

        [Required]
        public double EarningsCash { get; set; }

        [Required]
        public double EarningsCard { get; set; }

        [Required]
        public double CardFees { get; set; }

        [Required]
        public double EarningsRank { get; set; }

        [Required]
        public double CommissionDue { get; set; }

        [NotMapped]
        public double PaymentDue { get { return EarningsAccount + EarningsCard - CommissionDue; } }

        [Required]
        public double SubTotal { get; set; }

        [Required]
        public bool PaidInFull { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();

        [SwaggerIgnoreProperty]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? DateUpdated { get; set; }

    }
}

