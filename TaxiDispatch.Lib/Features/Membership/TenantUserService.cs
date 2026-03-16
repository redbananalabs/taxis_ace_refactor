using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxiDispatch.Data;

namespace TaxiDispatch.Features.Membership;

public class TenantUserService
{
    private static readonly MembershipRole[] SupportedRoles =
    [
        MembershipRole.AdminUser,
        MembershipRole.DispatchUser,
        MembershipRole.DriverUser,
        MembershipRole.AccountUser
    ];

    private readonly TaxiDispatchContext _db;
    private readonly RoleManager<AppRole> _roleManager;

    public TenantUserService(TaxiDispatchContext db, RoleManager<AppRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task EnsureCompatibilityRolesAsync()
    {
        foreach (var role in SupportedRoles)
        {
            var roleName = MembershipRoleMapper.ToLegacyIdentityRole(role);
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new AppRole(roleName));
            }
        }
    }

    public async Task<TenantUser> UpsertTenantUserAsync(int userId, MembershipRole role, int? createdByUserId, bool isActive = true, bool isDeleted = false)
    {
        var existing = await _db.Set<TenantUser>().FirstOrDefaultAsync(x => x.UserId == userId);
        var now = DateTime.UtcNow;

        if (existing == null)
        {
            existing = new TenantUser
            {
                UserId = userId,
                Role = role,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreatedUtc = now,
                CreatedByUserId = createdByUserId,
                UpdatedUtc = now
            };

            await _db.Set<TenantUser>().AddAsync(existing);
        }
        else
        {
            existing.Role = role;
            existing.IsActive = isActive;
            existing.IsDeleted = isDeleted;
            existing.UpdatedUtc = now;
        }

        return existing;
    }

    public async Task<TenantUser?> GetAsync(int userId)
    {
        return await _db.Set<TenantUser>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task UpdateLastLoginUtcAsync(int userId)
    {
        await _db.Set<TenantUser>()
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.LastLoginUtc, DateTime.UtcNow)
                .SetProperty(p => p.UpdatedUtc, DateTime.UtcNow));
    }
}
