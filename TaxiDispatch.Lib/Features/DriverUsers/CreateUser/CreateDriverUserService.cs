using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Features.Membership;
using TaxiDispatch.Services;

namespace TaxiDispatch.Features.DriverUsers.CreateUser;

public class CreateDriverUserService
{
    private readonly TaxiDispatchContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly TenantUserService _tenantUserService;
    private readonly AceMessagingService _messagingService;

    public CreateDriverUserService(
        TaxiDispatchContext db,
        UserManager<AppUser> userManager,
        TenantUserService tenantUserService,
        AceMessagingService messagingService)
    {
        _db = db;
        _userManager = userManager;
        _tenantUserService = tenantUserService;
        _messagingService = messagingService;
    }

    public async Task<Result<CreatedTenantUserResult>> CreateAsync(CreateDriverUserRequest request, int? createdByUserId = null)
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

        await _userManager.AddToRoleAsync(user, MembershipRoleMapper.ToLegacyIdentityRole(MembershipRole.DriverUser));
        await _tenantUserService.UpsertTenantUserAsync(user.Id, MembershipRole.DriverUser, createdByUserId);

        await _db.Set<DriverUserProfile>().AddAsync(new DriverUserProfile
        {
            UserId = user.Id,
            RegNo = request.RegistrationNo,
            ColorCodeRgb = request.ColorCode,
            ShowAllBookings = request.ShowAllBookings,
            ShowHvsBookings = request.ShowHvsBookings,
            VehicleColour = request.VehicleColor,
            VehicleMake = request.VehicleMake,
            VehicleModel = request.VehicleModel,
            VehicleType = request.VehicleType,
            CashCommissionRate = request.CashCommissionRate,
            CommsPlatform = request.Comms,
            NonAce = request.NonAce
        });

        await _db.UserProfiles.AddAsync(new UserProfile
        {
            UserId = user.Id,
            ColorCodeRGB = request.ColorCode,
            RegNo = request.RegistrationNo,
            ShowAllBookings = request.ShowAllBookings,
            ShowHVSBookings = request.ShowHvsBookings,
            VehicleColour = request.VehicleColor,
            VehicleMake = request.VehicleMake,
            VehicleModel = request.VehicleModel,
            VehicleType = request.VehicleType,
            CashCommissionRate = request.CashCommissionRate,
            CommsPlatform = request.Comms,
            NonAce = request.NonAce
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _messagingService.SendRegistrationEmail(
            user.Email,
            user.FullName,
            new TaxiDispatch.DTOs.MessageTemplates.NewUserRegisteredDto
            {
                fullname = user.FullName,
                password = request.Password,
                reg = request.RegistrationNo,
                userid = user.Id,
                username = user.UserName
            });

        return Result.Ok(new CreatedTenantUserResult
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = MembershipRole.DriverUser
        });
    }
}
