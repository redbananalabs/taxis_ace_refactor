using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [Route("/u/{shortCode}")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly UrlTrackingService _urlTrackingService;

        public RedirectController(UrlTrackingService urlTrackingService)
        {
            _urlTrackingService = urlTrackingService;
        }

        [HttpGet]
        public async Task<IActionResult> RedirectToLongUrl([FromRoute] string shortCode)
        {
            var longUrl = await _urlTrackingService.ResolveAndTrackLongUrlAsync(shortCode, HttpContext.RequestAborted);

            if (string.IsNullOrWhiteSpace(longUrl))
            {
                return NotFound();
            }

            return Redirect(longUrl);
        }
    }
}
