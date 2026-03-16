using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Features.Membership;

namespace TaxiDispatch.Features.AdminUsers.CreateUser;

public class CreateAdminUserService
{
    private readonly TaxiDispatchContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly TenantUserService _tenantUserService;

    public CreateAdminUserService(TaxiDispatchContext db, UserManager<AppUser> userManager, TenantUserService tenantUserService)
    {
        _db = db;
        _userManager = userManager;
        _tenantUserService = tenantUserService;
    }

    public async Task<Result<CreatedTenantUserResult>> CreateAsync(CreateAdminUserRequest request, int? createdByUserId = null)
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

        await _userManager.AddToRoleAsync(user, MembershipRoleMapper.ToLegacyIdentityRole(MembershipRole.AdminUser));
        await _tenantUserService.UpsertTenantUserAsync(user.Id, MembershipRole.AdminUser, createdByUserId);

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
            Role = MembershipRole.AdminUser
        });
    }
}
