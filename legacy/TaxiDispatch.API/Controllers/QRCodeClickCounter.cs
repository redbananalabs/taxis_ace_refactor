using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers
{
    [Route("qrcode/{location}")]
    [ApiController]
    public class QRCodeClickCounter : ControllerBase
    {
        private readonly UrlTrackingService _urlTrackingService;

        public QRCodeClickCounter(UrlTrackingService urlTrackingService)
        {
            _urlTrackingService = urlTrackingService;
        }

        [HttpGet]
        public async Task<IActionResult> RedirectToLongUrl([FromRoute] string location)
        {
            await _urlTrackingService.TrackQrCodeClickAsync(location, HttpContext.RequestAborted);
            return Redirect($"https://acetaxisdorset.co.uk?referer={location}");
        }
    }
}
