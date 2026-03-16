#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class AccountInvoiceTemplateDto
    {
        public string customer { get; set; }
        public string invno { get; set; }
        public string subject { get; set; }
    }
}
