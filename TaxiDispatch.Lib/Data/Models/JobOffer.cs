using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class JobOffer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Guid { get; set; }
        public DateTime BookingDateTime { get; set; }
        public DateTime CreatedOn { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string,string> Data { get; set; }
        public int BookingId { get; set; }
        public int Attempts { get; set; }
    }
}

