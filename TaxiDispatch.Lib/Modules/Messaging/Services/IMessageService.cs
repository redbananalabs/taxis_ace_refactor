namespace TaxiDispatch.Modules.Messaging;

public interface IMessageService
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);

    Task SendEmailAsync(string email, string subject, string htmlMessage, string[] attachments = null);

    Task<bool> SendSmsAsync(string number, string message);

    Task SendTransactionalEmail(string toEmail, string toName, string subject, string templateId, object data, string bcc);
}
