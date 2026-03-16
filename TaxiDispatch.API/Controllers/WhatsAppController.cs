using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/x-www-form-urlencoded")]
    public class WhatsAppController : TwilioController
    {
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(
            WhatsAppService whatsAppService,
            ILogger<WhatsAppController> logger)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        [HttpPost]
        [Route("RecieveReply")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<TwiMLResult> RecieveReply([FromForm] SmsRequest request)
        {
            var messagingResponse = new MessagingResponse();

            await _whatsAppService.HandleReplyAsync(
                request.From,
                request.Body,
                request.ButtonPayload,
                HttpContext.RequestAborted);

            return TwiML(messagingResponse);
        }

        [HttpGet]
        [Route("Send")]
        public async Task Send(string number, string message)
        {
            _logger.LogInformation("Sending WhatsApp message to {Number}", number);
            await _whatsAppService.SendMessageAsync(number, message);
        }

        public class SmsRequest : TwilioRequest
        {
            public string SmsMessageSid { get; set; } = string.Empty;

            public string SmsSid { get; set; } = string.Empty;

            public int NumMedia { get; set; }

            public string ProfileName { get; set; } = string.Empty;

            public string WaId { get; set; } = string.Empty;

            public string SmsStatus { get; set; } = string.Empty;

            public string Body { get; set; } = string.Empty;

            public string ButtonPayload { get; set; } = string.Empty;

            public new string To { get; set; } = string.Empty;

            public int NumSegments { get; set; }

            public int ReferralNumMedia { get; set; }

            public string MessageSid { get; set; } = string.Empty;

            public new string AccountSid { get; set; } = string.Empty;

            public new string From { get; set; } = string.Empty;

            public string ApiVersion { get; set; } = string.Empty;
        }
    }
}
