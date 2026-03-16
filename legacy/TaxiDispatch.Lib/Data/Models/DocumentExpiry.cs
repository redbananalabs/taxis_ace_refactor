using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TaxiDispatch.Domain;

namespace TaxiDispatch.Data.Models
{
    public class DocumentExpiry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DocumentType DocumentType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastUpdatedOn { get; set; }

        [NotMapped]
        public string Fullname { get; set; }

        [NotMapped]
        public string ColorCode { get; set; }
    }
}

