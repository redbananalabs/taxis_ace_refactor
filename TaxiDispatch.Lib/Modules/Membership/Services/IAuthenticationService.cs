namespace TaxiDispatch.Modules.Membership.Services;

public interface IAuthenticationService
{
    Task<AuthenticateResponse> GetAPIToken(AuthenticateRequest model);

    Task<string> GenerateJwtToken(AppUser user);

    AppRefreshToken GenerateRefreshToken(int userId);

    Task<AuthenticateResponse> ValidateRefreshToken(RefreshTokenRequest model);
}
