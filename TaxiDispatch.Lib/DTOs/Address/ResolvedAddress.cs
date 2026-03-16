namespace TaxiDispatch.DTOs.Address
{
    public sealed record ResolvedAddress(
    string DisplayLabel,        // what you show in UI + booking summary
    string? PlaceName,          // "Asda Bedminster"
    string FormattedAddress,    // "East St, Bedminster, Bristol BS3 4JY, UK"
    string? Postcode,
    string? Line1,
    string? Line2,
    string? TownCity,
    string? County,
    double? Lat,
    double? Lng,
    string Source,
    string? GooglePlaceId
);
}

