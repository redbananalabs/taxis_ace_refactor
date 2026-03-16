using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Features.Membership;
using TaxiDispatch.Services;

namespace TaxiDispatch.Features.AccountUsers.CreateUser;

public class CreateAccountUserService
{
    private readonly TaxiDispatchContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly TenantUserService _tenantUserService;
    private readonly AceMessagingService _messagingService;

    public CreateAccountUserService(
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

    public async Task<Result<CreatedTenantUserResult>> CreateAsync(CreateAccountUserRequest request, int? createdByUserId = null)
    {
        if (!await _db.Accounts.AnyAsync(x => x.AccNo == request.AccNo))
        {
            return Result.Fail<CreatedTenantUserResult>($"Account {request.AccNo} was not found.");
        }

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

        await _userManager.AddToRoleAsync(user, MembershipRoleMapper.ToLegacyIdentityRole(MembershipRole.AccountUser));
        await _tenantUserService.UpsertTenantUserAsync(user.Id, MembershipRole.AccountUser, createdByUserId);

        await _db.Set<AccountUserLink>().AddAsync(new AccountUserLink
        {
            UserId = user.Id,
            AccNo = request.AccNo
        });

        await _db.UserProfiles.AddAsync(new UserProfile
        {
            UserId = user.Id,
            ShowAllBookings = false,
            ShowHVSBookings = false
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _messagingService.SendAccountRegistrationEmail(
            user.Email,
            user.FullName,
            new TaxiDispatch.DTOs.MessageTemplates.NewUserRegisteredDto
            {
                fullname = user.FullName,
                password = request.Password,
                accno = request.AccNo,
                userid = user.Id,
                username = user.UserName
            });

        return Result.Ok(new CreatedTenantUserResult
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = MembershipRole.AccountUser
        });
    }
}
