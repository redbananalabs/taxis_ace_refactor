using TaxiDispatch.Shared.Contracts;

namespace TaxiDispatch.Modules.Membership;

public class AuthenticateResponse : ResponseDTO
{
    public int UserId { get; set; }

    public string Username { get; set; }

    public string Token { get; set; }

    public DateTime TokenExpiry { get; set; }

    public string RefreshToken { get; set; }

    public AuthenticateResponse()
    {
        Errors = new List<string>();
    }

    public AuthenticateResponse(bool success, string error)
    {
        Errors = new List<string> { error };
    }

    public AuthenticateResponse(
        AppUser user,
        string token,
        DateTime tokenExpiry,
        string refreshToken,
        bool success,
        string[] errors)
    {
        Errors = new List<string>();

        UserId = user.Id;
        Username = user.UserName;
        Token = token;
        TokenExpiry = tokenExpiry;
        RefreshToken = refreshToken;
        Success = success;

        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }
}
