using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaxiDispatch.Features.Membership;
using TaxiDispatch.Modules.Membership.Services;

namespace TaxiDispatch.Modules.Membership.Security;

public class ApiJwtContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtConfig _appSettings;
    private readonly TokenValidationParameters _tokenvps;

    public ApiJwtContextMiddleware(
        RequestDelegate next,
        IOptions<JwtConfig> appSettings,
        TokenValidationParameters tokenValidationParameters)
    {
        _next = next;
        _appSettings = appSettings.Value;
        _tokenvps = tokenValidationParameters;
    }

    public async Task Invoke(HttpContext context, IUsersService userService, TenantUserService tenantUserService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (token != null)
        {
            await AttachUserToContext(context, userService, tenantUserService, token);
        }

        await _next(context);
    }

    private async Task AttachUserToContext(
        HttpContext context,
        IUsersService userService,
        TenantUserService tenantUserService,
        string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, _tokenvps, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

            var user = await userService.FindById(userId);
            var tenantUser = await tenantUserService.GetAsync(userId);

            if (tenantUser != null && !tenantUser.IsActive)
            {
                context.Items["Inactive"] = true;
                return;
            }

            if (user != null && !user.LockoutEnabled)
            {
                context.Items["User"] = user;
                context.Items["LockedOut"] = false;
                context.Items["Inactive"] = false;

                var userClaims = new List<Claim>
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                foreach (var role in jwtToken.Claims.Where(x => x.Type == ClaimTypes.Role || x.Type == "Role"))
                {
                    userClaims.Add(new Claim(ClaimTypes.Role, role.Value));
                }

                var membershipRole = jwtToken.Claims.FirstOrDefault(x => x.Type == "membership_role")?.Value;
                if (!string.IsNullOrWhiteSpace(membershipRole))
                {
                    userClaims.Add(new Claim("membership_role", membershipRole));
                }

                var userIdentity = new ClaimsIdentity(userClaims, "User Identity");
                var userPrincipal = new ClaimsPrincipal(new[] { userIdentity });

                context.User = userPrincipal;
                await context.SignInAsync(userPrincipal);
            }
            else
            {
                context.Items["LockedOut"] = true;
            }
        }
        catch
        {
            // keep request unauthenticated when token validation fails
        }
    }
}
