using Microsoft.Extensions.Options;

namespace TaxiDispatch.API.Configuration;

public sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    private const string DefaultTenantHeader = "X-Tenant-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<TenancyOptions> _tenancyOptions;
    private readonly ILogger<TenantConnectionResolver> _logger;
    private readonly string _defaultConnectionString;

    public TenantConnectionResolver(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TenancyOptions> tenancyOptions,
        IConfiguration configuration,
        ILogger<TenantConnectionResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenancyOptions = tenancyOptions;
        _logger = logger;
        _defaultConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing");
    }

    public string ResolveTenantId()
    {
        var options = _tenancyOptions.Value;
        var headerName = string.IsNullOrWhiteSpace(options.TenantHeaderName)
            ? DefaultTenantHeader
            : options.TenantHeaderName;

        var headerValue = _httpContextAccessor.HttpContext?
            .Request
            .Headers[headerName]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.Trim();
        }

        if (options.GetMode() == TenancyMode.PerTenantDatabase && options.RequireTenantHeader)
        {
            throw new InvalidOperationException($"Tenant header '{headerName}' is required.");
        }

        return options.DefaultTenantId;
    }

    public string ResolveConnectionString()
    {
        var options = _tenancyOptions.Value;
        var mode = options.GetMode();

        if (mode == TenancyMode.SingleDatabase)
        {
            return _defaultConnectionString;
        }

        var tenantId = ResolveTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return _defaultConnectionString;
        }

        if (options.TenantConnections.TryGetValue(tenantId, out var connectionString) &&
            !string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (tenantId.Equals(options.DefaultTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return _defaultConnectionString;
        }

        if (options.RequireTenantHeader)
        {
            throw new InvalidOperationException(
                $"No tenant connection mapping found for tenant '{tenantId}'.");
        }

        _logger.LogWarning(
            "No tenant connection mapping for '{TenantId}'. Falling back to DefaultConnection.",
            tenantId);

        return _defaultConnectionString;
    }
}
