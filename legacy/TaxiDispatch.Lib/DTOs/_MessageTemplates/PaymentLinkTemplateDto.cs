#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class PaymentLinkTemplateDto
    {
        public string customer { get; set; }
        public string link { get; set; }
        public string subject { get; set; }
    }
}
