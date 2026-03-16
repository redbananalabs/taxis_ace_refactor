using TaxiDispatch.Services;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvailabilityController : ControllerBase
    {
        private readonly AvailabilityService _service;
        private readonly AceMessagingService _messagingService;

        public AvailabilityController(AvailabilityService service, AceMessagingService messaging)
        {
            _service = service;
            _messagingService = messaging;
        }

        [HttpGet]
        [ApiAuthorize]
        [Route("General")]
        public async Task<IActionResult> GetGeneralAvailability(DateTime date)
        {
            var data = await _service.GetGeneralAvailability(date);
            return Ok(data);

        }

        [HttpGet]
        //[ApiAuthorize]
        [Route("Reminder")]
        public async Task<IActionResult> SendReminder(string key)
        {
            if (key == "ace@taxis")
            {
                await _messagingService.SendDriverAvailabilityReminderSMS();
                return Ok();
            }

            return BadRequest("invalid key");
        }
    }
}



