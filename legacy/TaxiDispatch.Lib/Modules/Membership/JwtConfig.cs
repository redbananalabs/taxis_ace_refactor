namespace TaxiDispatch.Modules.Membership;

public partial class JwtConfig
{
    public string Key { get; set; }

    public string Issuer { get; set; }

    public int ExpiryDays { get; set; }

    public int RefreshExpiryDays { get; set; }
}
