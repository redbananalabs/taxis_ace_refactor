#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class NewUserRegisteredDto
    {
        public int userid { get; set; }
        public int accno { get; set; }
        public string fullname { get; set; }
        public string reg { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string subject { get; set; }
    }
}
