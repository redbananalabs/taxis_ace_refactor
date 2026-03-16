namespace TaxiDispatch.API.Configuration;

public interface ITenantConnectionResolver
{
    string ResolveTenantId();
    string ResolveConnectionString();
}
