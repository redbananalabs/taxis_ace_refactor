using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class ReviewRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Telephone { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();
    }
}

