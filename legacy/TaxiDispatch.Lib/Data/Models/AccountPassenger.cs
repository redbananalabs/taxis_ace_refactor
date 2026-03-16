using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class AccountPassenger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AccNo { get; set; }
        public string Description { get; set; }
        public string Passenger { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string? Phone { get; set; }

        [ForeignKey(nameof(AccNo))]
        public virtual Account Account { get; set; }
    }
}

