using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ATestController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }
    }
}

