using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Modules.Messaging;
using TaxiDispatch.Modules.Messaging.Services;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagingController : ControllerBase
    {
        private readonly AceMessagingService _messageService;
        private readonly IPushNotificationService _pushNotificationService;

        public MessagingController(
            AceMessagingService messageService,
            IPushNotificationService pushNotificationService)
        {
            _messageService = messageService;
            _pushNotificationService = pushNotificationService;
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("SendTextMessage")]
        public async Task<IActionResult> SendTextMessage(SendTextMessageRequestDto request)
        {
            if (ModelState.IsValid)
            {
                await _messageService.SendSmsAsync(request.Message, request.Telephone);
                return Ok();
            }

            return BadRequest(ModelState);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("SendPushNotification")]
        public async Task<IActionResult> SendPushNotification(PushNotificationRequest request)
        {
            if (ModelState.IsValid)
            {
                await _pushNotificationService.SendAndroidNotification(request);
                return Ok();
            }

            return BadRequest(ModelState);
        }
    }
}


