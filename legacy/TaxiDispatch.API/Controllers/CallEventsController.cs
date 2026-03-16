using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallEventsController : ControllerBase
    {
        private readonly CallEventsService _callEventsService;

        public CallEventsController(CallEventsService callEventsService)
        {
            _callEventsService = callEventsService;
        }

        [HttpGet]
        [Route("CallNotification")]
        public async Task<IActionResult> CallNotification(string caller_id, string recipient_id)
        {
            if (caller_id == "(anonymous)" || recipient_id == "(anonymous)")
            {
                return Ok();
            }

            var payload = await _callEventsService.PublishCallNotificationAsync(caller_id, HttpContext.RequestAborted);
            return Ok(payload);
        }

        [HttpGet]
        [Route("LookupNumber")]
        public async Task<IActionResult> LookupByNumber(string number)
        {
            if (number.StartsWith("044"))
            {
                number = "0" + number[3..];
            }

            await _callEventsService.PublishCallNotificationAsync(number, HttpContext.RequestAborted);
            return Ok(null);
        }

        [HttpGet]
        [Route("LookupEmail")]
        public IActionResult LookupByEmail(string email)
        {
            return Ok(email);
        }

        [HttpGet]
        [Route("CallerLookup")]
        public async Task<IActionResult> CallNotificationLookup(string caller_id)
        {
            var payload = await _callEventsService.BuildCallerLookupAsync(caller_id, HttpContext.RequestAborted);
            return Ok(payload);
        }
    }
}
