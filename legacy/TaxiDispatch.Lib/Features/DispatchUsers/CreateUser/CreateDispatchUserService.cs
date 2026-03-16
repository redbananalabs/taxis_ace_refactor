using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Features.Membership;

namespace TaxiDispatch.Features.DispatchUsers.CreateUser;

public class CreateDispatchUserService
{
    private readonly TaxiDispatchContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly TenantUserService _tenantUserService;

    public CreateDispatchUserService(TaxiDispatchContext db, UserManager<AppUser> userManager, TenantUserService tenantUserService)
    {
        _db = db;
        _userManager = userManager;
        _tenantUserService = tenantUserService;
    }

    public async Task<Result<CreatedTenantUserResult>> CreateAsync(CreateDispatchUserRequest request, int? createdByUserId = null)
    {
        if (await _userManager.FindByNameAsync(request.Username) != null)
        {
            return Result.Fail<CreatedTenantUserResult>("Username already in use");
        }

        var user = new AppUser
        {
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.Fullname
        };

        await using var tx = await _db.Database.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result.Fail<CreatedTenantUserResult>(string.Join("\r\n", createResult.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(user, MembershipRoleMapper.ToLegacyIdentityRole(MembershipRole.DispatchUser));
        await _tenantUserService.UpsertTenantUserAsync(user.Id, MembershipRole.DispatchUser, createdByUserId);

        await _db.UserProfiles.AddAsync(new UserProfile
        {
            UserId = user.Id,
            LastLogin = null,
            ShowAllBookings = false,
            ShowHVSBookings = false
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Result.Ok(new CreatedTenantUserResult
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = MembershipRole.DispatchUser
        });
    }
}
