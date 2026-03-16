#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class DriverStatementDto 
    {
        public int userid { get; set; }
        public string fullname { get; set; }
        public string reg { get; set; }
        public int statementid { get; set; }
        public string period { get; set; }

        public List<transaction> transactions {  get; set; } 

        public string commstotal { get; set; }
        public string nettotal { get; set; }

        public string subject { get; set; }
        public class transaction
        {
            public string date { get; set; }
            public int bookingid { get; set; }
            public string comms { get; set; }
            public string net { get; set; }
        }

    }
}
