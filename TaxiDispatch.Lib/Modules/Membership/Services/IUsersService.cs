using Microsoft.AspNetCore.Identity;

namespace TaxiDispatch.Modules.Membership.Services;

public interface IUsersService
{
    Task<IdentityResult> AddUserToRole(AppUser newUser, string roleName);

    Task<bool> CheckPassword(AppUser user, string password);

    Task<IdentityResult> Create(AppUser newUser, string password);

    Task<AppUser> FindById(int id);

    Task<AppUser?> FindByName(string username);

    Task<AppUser?> FindByUsernamePassword(string username, string password);

    Task<IEnumerable<AppUser>> GetAll();

    Task<AppRole?> GetRoleFromRoleName(string name);

    Task<string?> GetUsername(int id);

    Task<IList<string>> GetUserRoles(AppUser user);

    Task<bool> IsLockedOut(int userId);

    Task LockoutOnOff(AppUser user, bool lockout);
}
