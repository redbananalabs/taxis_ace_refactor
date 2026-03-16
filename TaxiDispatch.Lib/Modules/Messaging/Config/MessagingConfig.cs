namespace TaxiDispatch.Modules.Messaging;

public class MessagingConfig
{
    public SMSConfig Sms { get; set; }

    public EmailConfig Email { get; set; }

    public PushConfig Push { get; set; }

    public TwilioConfig Twilio { get; set; }

    public class SMSConfig
    {
        public string ApiKey { get; set; }

        public string Sender { get; set; }

        public string Testing { get; set; }
    }

    public class EmailConfig
    {
        public string Sender { get; set; }

        public string SenderName { get; set; }

        public string SendGridApiKey { get; set; }
    }

    public class PushConfig
    {
        public string FcmApiKey { get; set; }

        public string VapIdPublicKey { get; set; }

        public string VapIdPrivateKey { get; set; }

        public string VapSubject { get; set; }
    }

    public class TwilioConfig
    {
        public string TWILIO_ACCOUNT_SID { get; set; }

        public string TWILIO_AUTH_TOKEN { get; set; }

        public string TWILIO_FROM_CHANNEL { get; set; }
    }
}
