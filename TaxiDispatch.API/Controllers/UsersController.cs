using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.Domain;
using TaxiDispatch.Features.AdminUsers.CreateUser;
using TaxiDispatch.Features.DispatchUsers.CreateUser;
using TaxiDispatch.Modules.Membership;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;
        private readonly IAuthenticationService _authenticationService;
        private readonly CreateAdminUserService _createAdminUserService;
        private readonly CreateDispatchUserService _createDispatchUserService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUsersService usersService,
            IAuthenticationService authenticationService,
            CreateAdminUserService createAdminUserService,
            CreateDispatchUserService createDispatchUserService,
            ILogger<UsersController> logger)
        {
            _usersService = usersService;
            _authenticationService = authenticationService;
            _createAdminUserService = createAdminUserService;
            _createDispatchUserService = createDispatchUserService;
            _logger = logger;
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpGet("Test")]
        public async Task<IActionResult> Test()
        {
            return Ok("Yes Working");
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost]
        [Route("Register")]
        public async virtual Task<IActionResult> Register([FromBody] RegisterUserRequestDto model)
        {
            if (ModelState.IsValid)
            {
                Result<TaxiDispatch.Features.Membership.CreatedTenantUserResult> createResult;
                switch (model.RoleName?.Trim())
                {
                    case "Admin":
                        createResult = await _createAdminUserService.CreateAsync(new CreateAdminUserRequest
                        {
                            Username = model.Username,
                            Fullname = model.Fullname ?? string.Empty,
                            PhoneNumber = model.PhoneNumber,
                            Email = model.Email,
                            Password = model.Password
                        });
                        break;

                    case "Dispatch":
                    case "DispatchUser":
                    case "User":
                    case null:
                    case "":
                        createResult = await _createDispatchUserService.CreateAsync(new CreateDispatchUserRequest
                        {
                            Username = model.Username,
                            Fullname = model.Fullname ?? string.Empty,
                            PhoneNumber = model.PhoneNumber,
                            Email = model.Email,
                            Password = model.Password
                        });
                        break;

                    default:
                        return BadRequest("Use the role-specific create endpoint for this user type.");
                }

                if (!createResult.Success)
                {
                    _logger.LogInformation("username already in use or registration failed - {Username}", model.Username);
                    return BadRequest(createResult.ErrorMessage);
                }

                var auth = await _authenticationService.GetAPIToken(new AuthenticateRequest
                {
                    UserName = model.Username,
                    Password = model.Password
                });

                _logger.LogInformation($"user created - {model.Username}");

                return Ok(auth);
            }

            return BadRequest("Invalid payload");
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("AddUserToRole")]
        public async Task<IActionResult> AddUserToRole(int userId, string roleName)
        {
            var user = await _usersService.FindById(userId);
            return Ok(await _usersService.AddUserToRole(user, roleName));
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("CheckPassword")]
        public async Task<IActionResult> CheckPassword(int userId, string password)
        {
            var user = await _usersService.FindById(userId);
            return Ok(await _usersService.CheckPassword(user, password));
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("FindByUserId")]
        public async Task<AppUser> FindByUserId(int id)
        {
            return await _usersService.FindById(id);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("FindByUsername")]
        public async Task<AppUser?> FindByUsername(string username)
        {
            return await _usersService.FindByName(username);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("FindByUsernamePassword")]
        public async Task<AppUser?> FindByUsernamePassword(string username, string password)
        {
            return await _usersService.FindByUsernamePassword(username, password);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetRoleFromRoleName")]
        public async Task<AppRole?> GetRoleFromRoleName(string name)
        {
            return await _usersService.GetRoleFromRoleName(name);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetUsersRoles")]
        public async Task<IList<string>> GetUsersRoles(int userId)
        {
            var user = await _usersService.FindById(userId);
            return await _usersService.GetUserRoles(user);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = _usersService.GetAll();
            return Ok(users);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("IsLockedOut")]
        public async Task<bool> IsLockedOut(int userId)
        {
            return await _usersService.IsLockedOut(userId);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("LockoutOnOff")]
        public async Task LockoutOnOff(int userId, bool lockout)
        {
            var user = await _usersService.FindById(userId);
            await _usersService.LockoutOnOff(user, lockout);
        }
    }
}


