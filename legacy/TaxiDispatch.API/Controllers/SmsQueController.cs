using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmsQueController : ControllerBase
    {
        private readonly SmsQueueService _smsQueueService;

        public SmsQueController(SmsQueueService smsQueueService)
        {
            _smsQueueService = smsQueueService;
        }

        [HttpGet]
        [Route("Get")]
        public async Task<ActionResult> GetMessages()
        {
            try
            {
                var message = await _smsQueueService.GetNextMessageAsync(HttpContext.RequestAborted);

                if (message == null)
                {
                    return NotFound();
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("SendText")]
        public async Task SendTextMessage(string message, string telephone)
        {
            await _smsQueueService.SendTextMessageAsync(
                message,
                telephone,
                User?.Identity?.Name,
                HttpContext.RequestAborted);
        }
    }
}
