using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.Data.Models
{
    public class COARecord
    {
        public COARecord() 
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AccountNo { get; set; }

        public DateTime JourneyDateTime { get; set; }
        public string PassengerName { get; set; }
        public DateTime COADateTime { get; set; }
        public string PickupAddress { get; set; }
    }
}

