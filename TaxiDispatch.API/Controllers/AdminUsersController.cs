using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Features.AdminUsers.CreateUser;
using TaxiDispatch.Shared.Contracts;

namespace TaxiDispatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminUsersController : ControllerBase
{
    private readonly CreateAdminUserService _service;

    public AdminUsersController(CreateAdminUserService service)
    {
        _service = service;
    }

    [ApiAuthorize]
    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request)
    {
        var createdByUserId = GetCurrentUserId();
        var result = await _service.CreateAsync(request, createdByUserId);
        if (!result.Success)
        {
            return BadRequest(new ResponseDTO { Success = false, Error = result.ErrorMessage });
        }

        return Ok(new ResponseDTO { Success = true, Value = result.Value });
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirst("id")?.Value;
        return int.TryParse(raw, out var userId) ? userId : null;
    }
}
