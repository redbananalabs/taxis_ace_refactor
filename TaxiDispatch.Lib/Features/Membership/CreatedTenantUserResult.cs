namespace TaxiDispatch.Features.Membership;

public sealed class CreatedTenantUserResult
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public MembershipRole Role { get; init; }
}
