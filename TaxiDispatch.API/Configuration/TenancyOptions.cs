namespace TaxiDispatch.API.Configuration;

public enum TenancyMode
{
    SingleDatabase,
    PerTenantDatabase
}

public sealed class TenancyOptions
{
    public string Mode { get; set; } = nameof(TenancyMode.SingleDatabase);
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";
    public bool RequireTenantHeader { get; set; }
    public string DefaultTenantId { get; set; } = "default";
    public Dictionary<string, string> TenantConnections { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public TenancyMode GetMode()
    {
        return Enum.TryParse<TenancyMode>(Mode, ignoreCase: true, out var parsedMode)
            ? parsedMode
            : TenancyMode.SingleDatabase;
    }
}
