using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TaxiDispatch.Modules.Messaging;
using RabbitMQ.Client;
using SendGrid;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using Twilio.Rest.Api.V2010.Account;

namespace TaxiDispatch.Services
{
    public class AceMessagingService : MessageService, IMessageService
    {
        private readonly TaxiDispatchContext _db;
        private readonly ILogger<AceMessagingService> _logger;
        private AvailabilityService _availabilityService;
        private readonly UINotificationService _notificationService;
        

        public AceMessagingService(
            IOptions<MessagingConfig> config,
            ISendGridClient sendGrid, 
            IDbContextFactory<TaxiDispatchContext> factory, 
            AvailabilityService availabilityService,
            UINotificationService notificationService, ILogger<AceMessagingService> logger) : 
            base(config, sendGrid)
        {
            _db = factory.CreateDbContext();
            _logger = logger;
            _availabilityService = availabilityService;
            _notificationService = notificationService;

        }

        public async Task<SendMessageOfType> GetMessageTypeToSend(SentMessageType type)
        {
            var res = SendMessageOfType.None;
            var obj = await _db.MessagingNotifyConfig.FirstOrDefaultAsync();

            if (obj != null)
            {
                switch (type)
                {
                    case SentMessageType.DriverOnAllocate:
                        res = obj.DriverOnAllocate;
                        break;
                    case SentMessageType.DriverOnUnAllocate:
                        res = obj.DriverOnUnAllocate;
                        break;
                    case SentMessageType.DriverOnAmend:
                        res = obj.DriverOnAmend;
                        break;
                    case SentMessageType.DriverOnCancel:
                        res = obj.DriverOnCancel;
                        break;
                    case SentMessageType.CustomerOnAllocate:
                        res = obj.CustomerOnAllocate;
                        break;
                    case SentMessageType.CustomerOnUnAllocate:
                        res = obj.CustomerOnUnAllocate;
                        break;
                    case SentMessageType.CustomerOnAmend:
                        res = obj.CustomerOnAmend;
                        break;
                    case SentMessageType.CustomerOnCancel:
                        res = obj.CustomerOnCancel;
                        break;
                    case SentMessageType.CustomerOnComplete:
                        res = obj.CustomerOnComplete;
                        break;
                }
            }

            return res;
        }

        #region EMAIL

        public async Task SendEmailRaiseTicket(string subject, string messageBody, Stream stream, string filename)
        {
            var smtp = new SmtpClient("smtp.ionos.co.uk")
            {
                Port = 587,               // or the port your server uses
                Credentials = new NetworkCredential("bookings@acetaxisdorset.co.uk", "@CeTaxis171!@"),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress("bookings@acetaxisdorset.co.uk"),
                Subject = subject,
                Body = messageBody,
                IsBodyHtml = false
            };

            // --- NEW CODE STARTS HERE ---

            if (stream != null && stream.Length > 0 && !string.IsNullOrEmpty(filename))
            {
                // Create the Attachment object from the stream and filename
                // The Attachment class handles the content type automatically if you specify a filename
                Attachment attachment = new Attachment(stream, filename);

                // Add the attachment to the message collection
                message.Attachments.Add(attachment);
            }

            // --- NEW CODE ENDS HERE ---


            message.To.Add("support@TaxiDispatch.raiseaticket.com");

            await smtp.SendMailAsync(message);
        }

        public async Task SendCashBookingAcceptedEmail(string toEmail, string toName, BookingAcceptedEmail dto)
        {
            dto.subject = "Ace Taxis - Booking Accepted";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.CashBookingAccepted, dto);
        }

        public async Task SendAccountBookingCancelledEmail(string toEmail, string toName, BookingCancelledEmail dto)
        {
            dto.subject = "Ace Taxis - Booking Cancelled";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountBookingCancelled, dto);
        }

        public async Task SendAccountBookingAcceptedEmail(string toEmail, string toName, BookingAcceptedEmail dto)
        {
            dto.subject = "Ace Taxis - Booking Accepted";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountBookingAccepted, dto);
        }

        public async Task SendCashBookingRejectedEmail(string toEmail, string toName, BookingRejectedEmail dto)
        {
            dto.subject = "Ace Taxis - Booking Rejected";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.CashBookingRejected, dto);
        }

        public async Task SendAccountBookingRejectedEmail(string toEmail, string toName, BookingRejectedEmail dto)
        {
            dto.subject = "Ace Taxis - Booking Rejected";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountBookingRejected, dto);
        }

        public async Task SendAccountRegistrationEmail(string toEmail, string toName, NewUserRegisteredDto dto)
        {
            dto.subject = "Ace Taxis - Account User Registration.";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountRegistration, dto);
        }

        public async Task SendRegistrationEmail(string toEmail, string toName, NewUserRegisteredDto dto)
        {
            dto.subject = "Ace Taxis - User Registration.";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.Register, dto);
        }

        public async Task SendDriverStatementEmail(string toEmail, string toName, DriverStatementDto dto)
        {
            dto.subject = "Ace Taxis Driver Statement.";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.DriverStatement, dto);
        }

        public async Task SendDriverStatementEmail(string toEmail, string toName, DriverStatementDto dto, string filename, string base64Content)
        {
            dto.subject = "Ace Taxis Driver Statement.";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.DriverStatement, dto, filename, base64Content);
        }

        public async Task SendDriverStatementResendEmail(string toEmail, string toName,  string filename, string base64Content)
        {
            var dto = new DriverStatementDto();
            dto.subject = "Ace Taxis Driver Statement.";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.DriverStatementResend, dto, filename, base64Content);
        }

        public async Task SendAccountInvoiceEmail(string toEmail, string toName, AccountInvoiceTemplateDto dto, string filename, string base64Content)
        {
            dto.subject = "Ace Taxis Invoice";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountInvoice, dto, filename, base64Content);
        }

        public async void SendCustomerQuoteEmail(SendQuoteDto req)
        {
            var obj = new { 
                price = "£" + req.Price.ToString("N2"), 
                datetime = req.Date.ToString("dd/MM/yyyy HH:mm"), 
                passengername = req.Passenger, 
                pickupaddress = req.Pickup,
                destinationaddress = req.Destination,
                subject = "Ace Taxis Quotation"
            };
            await SendEmailTemplate(req.Email, req.Passenger, EmailTemplates.Quotation, obj);
        }

        public async Task SendAccountCreditNoteEmail(string toEmail, string toName, string filename, string base64Content)
        {
            var dto = new CreditNoteTemplateDto();
            dto.subject = "Ace Taxis Credit Note";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.AccountCreditNote, dto, filename, base64Content);
        }

        public async Task SendAccountInvoiceEmailProDisability(string toEmail, string passengerName, string toName, AccountInvoiceTemplateDto dto, string filename, string base64Content)
        {
            var templateid = "d-81e7ebe0e76241448e37b5b3cf0ac8e4";
            var subject = $"Ace Taxis Invoice - {passengerName}";
            dto.subject = subject;
            await SendTransactionalEmail(toEmail, toName, subject, templateid, dto, filename, base64Content);
        }

        public async Task SendPaymentLinkEmail(string toEmail, string toName, PaymentLinkTemplateDto dto)
        {
            dto.subject = "Ace Taxis Payment Link";
            await SendEmailTemplate(toEmail, toName, EmailTemplates.PaymentLink, dto);
        }

        public async Task<bool> SendPaymentReceiptEmail(string toEmail, string toName, string filename, string base64Content)
        {
            var templateid = "d-46dd090bd0a44088ab2b490728ce7b00";
            var dto = new PaymentReceiptDto { customer = toName };
            return await SendTransactionalEmail(toEmail, toName, "Ace Taxis - Payment Receipt", templateid ,dto, filename, base64Content);
        }

        public async Task<bool> SendAccountInvoiceAttachmentsEmail(string toEmail, string toName, Dictionary<string,string> attachments)
        {
            var templateid = "d-7971cadd2ddd4532aa0f8192cfbe26a7";
            var dto = new PaymentReceiptDto { customer = toName };
            return await SendTransactionalEmail(toEmail, toName, "Ace Taxis - Invoice", templateid, dto, attachments);
        }

        private async Task SendEmailTemplate(string toEmail, string toName, EmailTemplates template, object data, string filename = "", string base64Content = "")
        {
            var templateid = string.Empty;
            var subject = string.Empty;

            switch (template)
            {
                case EmailTemplates.Register:
                    templateid = "d-588cd7318d7e40e4b4246e0fca058d59";
                    break;
                case EmailTemplates.DriverStatement:
                    templateid = "d-d2a3ffefe29940369e7426df64169845";
                    break;
                case EmailTemplates.AccountInvoice:
                    templateid = "d-7971cadd2ddd4532aa0f8192cfbe26a7";
                    break;
                case EmailTemplates.PaymentLink:
                    templateid = "d-743a839078a34921929932a5a73ef49a";
                    break;
                case EmailTemplates.PaymentReceipt:
                    templateid = "d-46dd090bd0a44088ab2b490728ce7b00";
                    break;
                case EmailTemplates.AccountRegistration:
                    templateid = "d-6378c357f86244f696a4d63db4239c4c";
                    break;
                case EmailTemplates.AccountBookingAccepted:
                    templateid = "d-b1c28ecd630b4d3dab7866d8137fc946";
                    break;
                case EmailTemplates.AccountBookingRejected:
                    templateid = "d-267592ce42de41e2909b2ad667cdbddd";
                    break;
                case EmailTemplates.AccountBookingCancelled:
                    templateid = "d-312f2ba92b364f84addffcffbe247724";
                    break;
                case EmailTemplates.DriverStatementResend:
                    templateid = "d-877358eb4ca54c62bd24432ce55f6d4b";
                    break;
                case EmailTemplates.AccountCreditNote:
                    templateid = "d-2dbd0524f3ed475c8d28a190ec0d1fa4";
                    break;
                case EmailTemplates.Quotation:
                    templateid = "d-17d734a8a9054d51981ead3eaf904fc0";
                    break;
                case EmailTemplates.CashBookingAccepted:
                    templateid = "d-e50cef9cdd5347d79eda80c8de33188d";
                    break;
                case EmailTemplates.CashBookingRejected:
                    templateid = "d-0b8454822a1e4571bb925126f5e7683e";
                    break;
            }

            if (string.IsNullOrEmpty(filename))
                await SendTransactionalEmail(toEmail, toName, subject, templateid, data);
            else
                await SendTransactionalEmail(toEmail, toName, subject, templateid, data, filename, base64Content);
        }

        #endregion

        #region WHATSAPP
        public async Task SendWhatsAppCancelled(string passengerName, DateTime date, string telephone)
        {
            var sid = "HX21ff0cd5636f6b00988dcf33edb18073";

            var dic = new Dictionary<string, object>()
            {
                { "1",$"{passengerName}" },
                { "2",date.ToString("dd/MM/yy HH:mm") }
            };

            await SendWhatsAppWithRetry(telephone, dic, sid);
        }

        public async Task SendWhatsAppUnAllocated(string passengerName, DateTime date, string telephone)
        {
            var sid = "HXc42c618c273c5f525d3345d98c11c7fd";

            var dic = new Dictionary<string, Object>()
            {
                { "1",$"{passengerName}" },
                { "2",date.ToString("dd/MM/yy HH:mm") }
            };

            await SendWhatsAppWithRetry(telephone, dic, sid);
        }

        public async Task SendWhatsAppAllocatedV3(string telephone, DateTime date, string passenger, int passengerCount,
            string pickup, string drop, string vias, string details, int bookingId, int userId)
        {
            var sid = "HX6806e9e02c16fa365de2f175a66233d8";

            var dic = new Dictionary<string, Object>()
            {
                { "1",date.ToString("dd/MM/yy HH:mm") },
                { "2",$"{passenger}" },
                { "3",$"{passengerCount}" },
                { "4",$"{pickup}" },
                { "5",$"{drop}" },
                { "6",$"{vias}" },
                { "7",$"{details}" },
                { "8",$"{bookingId}" }
            };

            var obj = new DriverAllocation();

            try
            {
                var res = await SendWhatsAppWithRetry(telephone, dic, sid);

                if (res.To == "0000011111")
                    return;

                if (res != null)
                {
                    var status =string.Empty;
                    var str = res.Status.ToString();
                    switch (str)
                    {
                        case "accepted":
                            status = "SENT";
                            break;
                        case "undelivered":
                            status = "UNDELIVERED";
                            break;
                        case "failed":
                            status = "FAILED";
                            break;
                        default:
                            status = str; break;
                    }

                    obj.TwilioResponse = status;
                    obj.UserId = userId;
                    obj.BookingId = bookingId;
                    obj.SentAt = DateTime.Now.ToUKTime();
                    obj.Type = SentMessageType.DriverOnAllocate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send allocate message to {telephone}");

                obj.TwilioResponse = "NOT SENT - ERROR";
                obj.UserId = userId;
                obj.BookingId = bookingId;
                obj.SentAt = DateTime.Now.ToUKTime();
                obj.Type = SentMessageType.DriverOnAllocate;
            }
            finally
            {
                try
                {
                    _db.DriverAllocations.Add(obj);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "unable to insert driver allocation record.");
                }
            }
        }

        public async Task SendWhatsAppBookingAmended(string passengerName, DateTime date, string telephone)
        {
            var sid = "HX39f0bb50e4c814a34da3980fedfa684b";

            var dic = new Dictionary<string, Object>()
            {
                { "1",$"{passengerName}" },
                { "2",date.ToString("dd/MM/yy HH:mm") }
            };

            await SendWhatsAppWithRetry(telephone, dic, sid);

            
        }

        public async override Task<MessageResource?> SendWhatsApp(string toNumber, Dictionary<string, Object> variables, string templateId)
        { 
            var res = await base.SendWhatsApp(toNumber, variables,templateId);

            return res;
        }

        private async Task<MessageResource?> SendWhatsAppWithRetry(string telephone, Dictionary<string, object> dic, string sid)
        {
            var tryCount = 1;
            var sleep = 5000;

            while (true)
            {
                try
                {
                    _logger.LogInformation($"Sending whatsapp message for {telephone}");
                    var res = await SendWhatsApp(telephone, dic, sid);

                    if (res != null)
                    {
                        _logger.LogInformation($"Twilio Response Status: {res.Status.ToString()}");

                        if (!string.IsNullOrEmpty(res.ErrorMessage))
                        {
                            _logger.LogInformation($"Twilio Error Message : {res.ErrorMessage}");
                        }
                    }

                    return res; // success!
                }
                catch (Exception ex)
                {
                    if (--tryCount == 0)
                    {
                        _logger.LogError(ex, @$"SendWhatsAppWithRetry() failed - all retries failed to {telephone}.");

                        // var message = DictionaryToString(dic);
                        // SendSmsMessage(telephone,message);
                        throw ex;
                    }
                    else
                    {
                        _logger.LogError(ex, @$"SendWhatsAppWithRetry() in retry to {telephone}");
                        await Task.Delay(sleep);
                    }
                }
            }
        }
        #endregion

        #region SMS
        private string _noreplyText = "\r\n\r\n(Please do not reply.)";

        public async void SendCustomerQuoteSMS(SendQuoteDto obj)
        {
            var str = string.Empty;

            if (obj.ReturnTime.HasValue)
            {
                str = $"Dear {obj.Passenger},\r\n\r\nYour quote for {obj.Pickup} to {obj.Destination} with {obj.Passengers} passengers is £{obj.Price:N2}.The price for your return journey on {obj.ReturnTime.Value:g} will be £{obj.ReturnPrice:N2}\r\n\r\nAce Taxis.";
            }
            else
            {
                str = $"Dear {obj.Passenger},\r\n\r\nYour quote for {obj.Pickup} to {obj.Destination} with {obj.Passengers} passengers is {obj.Price:N2}.\r\n\r\nAce Taxis.";
            }
            
            await SendSmsAsync(str, obj.Phone);
        }

        public async void SendCustomerOnBookedSMS(string tel, string date, string jobno)
        {
            var id = Convert.ToInt32(jobno);
            var data = await _db.Bookings.Where(o => o.Id == id).Select(o => new { o.PickupDateTime, o.PickupPostCode, o.DestinationPostCode, o.Price }).FirstOrDefaultAsync();
            var str = $"Booking from {data.PickupPostCode} to {data.DestinationPostCode} for {data.PickupDateTime:HH:mm dd/MM/yy} is confirmed on ref: {jobno}, the cost of your journey is £{data.Price:N2}.\r\n\r\nThank you for booking with Ace Taxis.";
            //var str = $"Thank you for booking with Ace Taxis, your journey is confirmed on reference {jobno}.\r\n\r\nAce Taxis.";
            //SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
            await SendSmsAsync(str, tel);
        }

        public void SendCustomerOnAllocateSMS(string tel, string make, string model, string color, string reg, string drivername)
        {
            var name = string.Empty;

            if (drivername.Contains(" "))
            {
                var arr = drivername.Split(" ");
                if (arr.Length > 1)
                {
                    name = arr[0];
                }
            }
            else
            {
                name = drivername;   
            }
            
            var str = $"You booking with Ace Taxis has been allocated, your drivers name is {name} in a {color} {make} {model} registration no {reg}.\r\n\r\nAce Taxis.";
           // SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
        }

        public async void SendCustomerArrivedSMS(string tel, string destination, string make, string model, string color, string reg, string drivername)
        {
            var name = string.Empty;

            if (drivername.Contains(" "))
            {
                var arr = drivername.Split(" ");
                if (arr.Length > 1)
                {
                    name = arr[0];
                }
            }
            else
            {
                name = drivername;
            }

            var str = $"Your Ace Taxi has arrived.\r\n\r\n Your drivers name is {name} in a {color} {make} {model} registration no {reg}.\r\n\r\nAce Taxis.";
            //SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
            await SendSmsAsync(str, tel);
        }

        public void SendCustomerOnBookingAmendSMS(string tel)
        {
            // ignore
        }

        public void SendCustomerOnBookingCancelledSMS(string tel, string date, string pickup)
        {
            var str = $"Your booking for {date} from {pickup} has been cancelled.\r\n\r\nThank you for using Ace Taxis.";
           // SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
        }

        public async void SendCustomerOnBookingCompletedSMS(string tel)
        {
            var data = await _db.ReviewRequests.Where(o => o.Telephone == tel).AnyAsync();

            if (!data)
            {
                var str = "Thank you for using Ace Taxis, 01747 821111.\r\n\r\nIf you have a moment we would really appreciate if you would leave us a review. https://cutt.ly/NefYovLC";
                //SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
                await SendSmsAsync(str, tel);
                
                var now = DateTime.Now.ToUKTime();
                _db.ReviewRequests.Add(new ReviewRequest { DateCreated = now, Telephone = tel });
                await _db.SaveChangesAsync();
            }
        }


        public async Task SendDriverAvailabilityReminderSMS()
        {
            var recipients = await _db.UserProfiles.AsNoTracking().Include(o => o.User)
                .Where(o=>o.NonAce == false && o.IsDeleted == false && o.User.LockoutEnabled == false)
                .Select(o => new { o.UserId, o.User.FullName, o.User.PhoneNumber })
                .ToListAsync();

            foreach (var recipient in recipients)
            {
                var msg = $"REMINDER!!\r\n\r\nHi {recipient.FullName},\r\n\r\nIf you haven't already, please can you make sure you add your availability for the coming week.";
                msg += "\r\n\r\nThank you.\r\n\r\nAce Taxis";

                SendToRabbitMq(new RabbitMessagePacket { Telephone = recipient.PhoneNumber, Message = msg });
            }
        }

        public async void SendPaymentLinkSMS(string telephone,string link)
        {
            var message = $"Thank you for booking with Ace Taxis, please use the link below to make payment for your journey.\r\n\r\n{link}";

            await SendSmsAsync(message, telephone);
           //  SendToRabbitMq(new RabbitMessagePacket { Telephone = telephone, Message = message }); ;
        }

        public async void SendPaymentLinkReminderSMS(string telephone, string link)
        {
            var message = $"Hi,\r\n\r\nThis is a reminder from Ace Taxis that payment for your journey is due ASAP, please use the link below to make payment.\r\n\r\n{link}";

            await SendSmsAsync(message, telephone);
           //  SendToRabbitMq(new RabbitMessagePacket { Telephone = telephone, Message = message }); ;
        }

        public bool SendPaymentReceiptSMS(string telephone, string link)
        {
            var message = $"Thank you for your recent payment to Ace Taxis. The link to your receipt is below.\r\n\r\n{link}";

            return SendSmsAsync(message,telephone).Result;
        }

        public bool SendSmsMessage(string telephone, string message)
        {
            return SendToRabbitMq(new RabbitMessagePacket { Telephone = telephone, Message = message }); ;
        }

        public async Task<string> ShorternUrl(string url)
        {
            var baseUrl = "http://pay.acetaxisdorset.co.uk/u/";
            string shortCode = GenerateShortCode();
            await _db.UrlMappings.AddAsync(new UrlMapping { LongUrl = url, ShortCode = shortCode });
            await _db.SaveChangesAsync();

            return (baseUrl + shortCode);
        }

        [Obsolete]
        public async Task<string> ShorternUrl11(string url)
        {
            var client = new HttpClient();

            var obj = new { domain = "ace.1soft.co.uk", originalURL = url };
            //var obj = new { domain = "gv19.short.gy", originalURL = url };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.short.io/links"),
                Headers =

                    {
                        { "accept", "application/json" },
                        { "Authorization", "sk_E7TqHwWbFvbuzgjB" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(obj))
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(body))
                { 
                    var res = JsonConvert.DeserializeObject<ShortenedUrlReply>(body);

                    return res.SecureShortURL;
                }
            }

            return url;
        }

        private string GenerateShortCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }


        private bool SendToRabbitMq(RabbitMessagePacket packet)
        {
            if (packet.Telephone == "07825350912" || packet.Telephone == "07738825598")
            {
                return true;
            }

            if (packet.Message.Length <= 138)
            {
                packet.Message += _noreplyText;
            }

            try
            {
                var username = "TaxiDispatch";
                var password = "ace";
                var host = "85.234.135.182";
                var port = 5672;
                var queName = "messages";
                var routingKey = "ace-routing-key";

                var factory = new ConnectionFactory();
                factory.Uri = new Uri($"amqp://{username}:{password}@{host}:{port}");
                factory.ClientProvidedName = "TaxiDispatch";

                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();

                channel.ExchangeDeclare("AceExchange", ExchangeType.Direct);
                channel.QueueDeclare(queName, false, false, false);
                channel.QueueBind(queName, "AceExchange", routingKey, null);
                channel.BasicQos(0, 1, false);

                var json = JsonConvert.SerializeObject(packet);
                var bytes = Encoding.UTF8.GetBytes(json);

                channel.BasicPublish("AceExchange", routingKey, null, bytes);
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"error sending sms via rabbit mq");
                return false;
            }
            
            //channel.Dispose();
            //connection.Dispose();
        }

        public async void SendDriverBookingAmendedSMS(string tel, string date, string passenger, bool nonace = false)
        {
            var str = $"Your booking on {date} for {passenger} has been amended.";

            if (nonace)
            { 
                await SendSmsAsync(str, tel);
                _logger.LogInformation($"Sent amended twilio sms to non ace driver {tel}");
            }
            else
            {
                SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
                _logger.LogInformation($"Sent amended sms to non ace driver {tel}");
            }
        }

        public async void SendDriverBookingCancelledSMS(string tel, string date, string passenger, bool nonace = false)
        {
            var str = $"Your booking on {date} for passenger {passenger} has been cancelled.";

            if (nonace)
            {
                await SendSmsAsync(str, tel);
                _logger.LogInformation($"Sent cancelled twilio sms to non ace driver {tel}");
            }
            else
            {
                SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
                _logger.LogInformation($"Sent cancelled sms to non ace driver {tel}");
            }
        }

        public void SendDriverBookingUnallocatedSMS(string tel, string date, string passenger)
        {
            var str = $"Your booking on {date} for passenger {passenger} has been unallocated.";
            SendToRabbitMq(new RabbitMessagePacket { Telephone = tel, Message = str });
        }
        #endregion

        #region BROWSER PUSH
        public async Task SendBrowserNotification(string title, string body)
        {
            var fcms = await _db.Set<UserDeviceRegistration>()
                .AsNoTracking()
                .Where(o => o.DeviceType == UserDeviceType.BrowserPush &&
                            o.IsActive &&
                            !string.IsNullOrEmpty(o.Token))
                .Select(o => o.Token)
                .ToListAsync();

            if (fcms.Count == 0)
            {
                fcms = await _db.UserProfiles.AsNoTracking()
                    .Where(o => !string.IsNullOrEmpty(o.ChromeFCM))
                    .Select(o => o.ChromeFCM)
                    .ToListAsync();
            }

            var push = new PushNotificationRequest();

            if (fcms != null)
            {
                if (fcms.Count > 0)
                {
                    push.registration_ids.AddRange(fcms);

                    push.notification = new NotificationMessageBody();

                    push.notification.title = title;
                    push.notification.body = body;
                    await SendChromeNotification(push);
                }
            }
        }
        #endregion

        public async Task<List<RecievedMessage>?> GetDriverMessages()
        {
            try
            {
                var lst = await _db.DriverMessages.AsNoTracking().Include(o=>o.User)
              .Where(o => o.Read == false)
              .ToListAsync();

                var res = new List<RecievedMessage>();
                foreach (var msg in lst)
                {
                    var obj = new RecievedMessage();
                    obj.Id = msg.Id;
                    obj.Date = msg.DateCreated;
                    obj.UserId = msg.UserId.Value;
                    obj.Message = msg.Message;
                    obj.Name = msg.User.FullName;

                    res.Add(obj);
                }

                return res;
            }
            catch
            {
                return new List<RecievedMessage>();
            }
        }

        public async Task MarkDriverMessageRead(int messageId)
        {
            await _db.DriverMessages.Where(o => o.Id == messageId)
             .ExecuteUpdateAsync(b => b.SetProperty(u => u.Read, true));
        }

        public async Task DeleteFromDriverAllocations(int bookingId)
        {
            await _db.DriverAllocations
                .Where(o => o.BookingId == bookingId)
                .ExecuteDeleteAsync();
        }

        // Converts a Dictionary<string, object> to a string representation
        public static string DictionaryToString(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var kvp in dict)
            {
                sb.Append($"{kvp.Value}, ");
            }
            // Remove trailing comma and space
            if (sb.Length > 2)
                sb.Length -= 2;
            return sb.ToString();
        }


        public class RecievedMessage
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public int UserId { get; set; }
            public string Name { get; set; }
            public string Message { get; set; }
        }

        public class ShortenedUrlReply
        {
            public string OriginalURL { get; set; }
            public int DomainId { get; set; }
            public bool Archived { get; set; }
            public string Source { get; set; }
            public bool Cloaking { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public int OwnerId { get; set; }
            public List<string> Tags { get; set; }
            public string Path { get; set; }
            public string Id { get; set; }
            public string IdString { get; set; }
            public string ShortURL { get; set; }
            public string SecureShortURL { get; set; }
            public bool Duplicate { get; set; }
        }
    }
}





