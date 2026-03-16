using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiDispatch.Data;

namespace TaxiDispatch.Modules.Membership.Services;

public class UsersService : IUsersService
{
    private readonly ILogger<UsersService> _logger;
    private readonly TaxiDispatchContext _dB;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UsersService(
        ILogger<UsersService> logger,
        TaxiDispatchContext dB,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _logger = logger;
        _dB = dB;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IdentityResult> Create(AppUser newUser, string password)
    {
        return await _userManager.CreateAsync(newUser, password);
    }

    public async Task<IdentityResult> AddUserToRole(AppUser newUser, string roleName)
    {
        return await _userManager.AddToRoleAsync(newUser, roleName);
    }

    public async Task<bool> CheckPassword(AppUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<AppRole?> GetRoleFromRoleName(string name)
    {
        return await _roleManager.FindByNameAsync(name);
    }

    public async Task RemoveUserFromRole(int userId, string role)
    {
        var userRole = await _dB.UserRoles.Where(o => o.UserId == userId).FirstOrDefaultAsync();
        _dB.UserRoles.Remove(userRole);
        await _dB.SaveChangesAsync();
    }

    public async Task<IList<string>> GetUserRoles(AppUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<AppUser?> FindByName(string username)
    {
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username);
            return user;
        }

        throw new ArgumentException("username cannot be null");
    }

    public async Task<AppUser?> FindByUsernamePassword(string username, string password)
    {
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user != null)
            {
                var isCorrect = await CheckPassword(user, password);
                if (isCorrect)
                {
                    return user;
                }

                return null;
            }
        }

        return null;
    }

    public async Task<AppUser> FindById(int id)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return user;
    }

    public async Task<IEnumerable<AppUser>> GetAll()
    {
        return await _dB.Users.AsNoTracking().ToListAsync();
    }

    public async Task<string?> GetUsername(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            throw new Exception($"user id {id} was not found.");
        }

        return user.UserName;
    }

    public async Task<bool> IsLockedOut(int userId)
    {
        return _dB.Users.Where(o => o.Id == userId).Select(o => o.LockoutEnabled).FirstOrDefault();
    }

    public async Task LockoutOnOff(AppUser user, bool lockout)
    {
        user.LockoutEnabled = lockout;
        user.LockoutEnd = DateTime.UtcNow.AddYears(5);
        _dB.Users.Update(user);
        await _dB.SaveChangesAsync();
    }
}
