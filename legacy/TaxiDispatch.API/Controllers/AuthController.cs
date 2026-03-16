using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Modules.Membership;

namespace TaxiDispatch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("Authenticate")]
        public async Task<IActionResult> AuthenticateUser([FromBody] AuthenticateRequest model)
        {
            var response = await _authService.GetAPIToken(model);

            if (response == null)
            {
                return BadRequest(new { message = "Username/password is incorrect or username not found." });
            }

            return Ok(response);
        }

        [HttpGet("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromQuery] RefreshTokenRequest model)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"validating refresh token {model.RefreshToken}");

                var result = await _authService.ValidateRefreshToken(model);

                if (result == null)
                {
                    return BadRequest("Invalid token");
                }

                _logger.LogInformation($"refresh token validation success: {result.Success}");

                if (result.Success)
                {
                    _logger.LogInformation($"returning token {result.RefreshToken}");
                    return Ok(result);
                }

                _logger.LogInformation($"validation reason error: {result.Errors.FirstOrDefault()}");
                return BadRequest(result.Errors.FirstOrDefault());
            }

            return BadRequest("invalid payload");
        }
    }
}

