using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaxiDispatch.Data;

namespace TaxiDispatch.Modules.Membership.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly JwtConfig _jwtConfig;
    private readonly TaxiDispatchContext _db;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IUsersService _usersService;
    private readonly TokenValidationParameters _tokenvps;

    public AuthenticationService(
        IOptions<JwtConfig> jwtConfig,
        TaxiDispatchContext dbContext,
        ILogger<AuthenticationService> logger,
        IUsersService usersService,
        TokenValidationParameters tokenValidationParameters)
    {
        _jwtConfig = jwtConfig.Value;
        _db = dbContext;
        _logger = logger;
        _usersService = usersService;
        _tokenvps = tokenValidationParameters;
    }

    public async Task<AuthenticateResponse> GetAPIToken(AuthenticateRequest model)
    {
        var user = await _usersService.FindByUsernamePassword(model.UserName, model.Password);
        if (user == null)
        {
            return null;
        }

        var tenantUser = await _db.Set<TenantUser>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (tenantUser != null && !tenantUser.IsActive)
        {
            return null;
        }

        var expiry = DateTime.Now.AddDays(_jwtConfig.ExpiryDays);
        var token = await GenerateJwtToken(user);

        var existing = await _db.AppRefreshTokens.Where(o => o.UserId == user.Id).FirstOrDefaultAsync();
        var refreshToken = string.Empty;

        if (existing == null)
        {
            var refresh = GenerateRefreshToken(user.Id);
            await _db.AppRefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();
            refreshToken = refresh.Token;
        }
        else
        {
            refreshToken = existing.Token;
        }

        return new AuthenticateResponse(user, token, expiry, refreshToken, true, null);
    }

    public async Task<AuthenticateResponse> ValidateRefreshToken(RefreshTokenRequest model)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var tokenInVerification = jwtTokenHandler.ValidateToken(model.Token, _tokenvps, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase);

                if (!result)
                {
                    return null;
                }
            }

            var utcExpiryDate = long.Parse(tokenInVerification.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDate = UnixTimestampToDateTime(utcExpiryDate);

            if (expiryDate > DateTime.UtcNow)
            {
                return new AuthenticateResponse(false, "Token has not expired");
            }

            var storedToken = await _db.AppRefreshTokens.FirstOrDefaultAsync(x => x.Token == model.RefreshToken);

            if (storedToken == null)
            {
                return new AuthenticateResponse(false, "Token does not exist");
            }

            if (storedToken.IsUsed)
            {
                return new AuthenticateResponse(false, "Token has been used");
            }

            if (storedToken.IsRevoked)
            {
                return new AuthenticateResponse(false, "Token has been revoked");
            }

            var jti = tokenInVerification.Claims.First(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (storedToken.JwtId != jti)
            {
                return new AuthenticateResponse(false, "Token does not match");
            }

            storedToken.IsUsed = true;
            _db.AppRefreshTokens.Update(storedToken);

            var dbuser = await _usersService.FindById(storedToken.UserId);
            var tenantUser = await _db.Set<TenantUser>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == dbuser.Id);
            if (tenantUser != null && !tenantUser.IsActive)
            {
                return new AuthenticateResponse(false, "User is inactive");
            }

            var expiry = DateTime.Now.AddDays(_jwtConfig.ExpiryDays);
            var token = await GenerateJwtToken(dbuser);

            var refresh = GenerateRefreshToken(dbuser.Id);
            await _db.AppRefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();

            return new AuthenticateResponse
            {
                RefreshToken = refresh.Token,
                Token = token,
                TokenExpiry = expiry,
                Username = dbuser.UserName,
                Success = true
            };
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogError("security error generating refresh token - expired refresh token", ex);
            return new AuthenticateResponse(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("unhandled error generating refresh token", ex);
            return new AuthenticateResponse(false, ex.Message);
        }
    }

    public async Task<string> GenerateJwtToken(AppUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddDays(_jwtConfig.ExpiryDays);

        var tenantUser = await _db.Set<TenantUser>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id);
        var roles = await _usersService.GetUserRoles(user);
        var role = tenantUser != null
            ? MembershipRoleMapper.ToLegacyIdentityRole(tenantUser.Role)
            : roles.FirstOrDefault() ?? "User";
        var membershipRole = tenantUser?.Role.ToString() ?? MembershipRoleMapper.FromLegacyIdentityRole(role).ToString();

        var claims = new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("Role", role),
            new Claim("membership_role", membershipRole),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _jwtConfig.Issuer,
            _jwtConfig.Issuer,
            claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public AppRefreshToken GenerateRefreshToken(int userId)
    {
        return new AppRefreshToken
        {
            JwtId = userId.ToString(),
            IsUsed = false,
            IsRevoked = false,
            UserId = userId,
            CreatedOn = DateTime.UtcNow,
            ExpiryDate = DateTime.Now.AddDays(_jwtConfig.RefreshExpiryDays),
            Token = RandomString(35) + Guid.NewGuid()
        };
    }

    private static DateTime UnixTimestampToDateTime(long unixTimestamp)
    {
        var datetimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        datetimeVal = datetimeVal.AddSeconds(unixTimestamp).ToLocalTime();
        return datetimeVal;
    }

    private static string RandomString(int length)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return new string(Enumerable.Repeat(chars, length)
            .Select(x => x[random.Next(x.Length)]).ToArray());
    }
}
