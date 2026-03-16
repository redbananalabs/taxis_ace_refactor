namespace TaxiDispatch.DTOs.Address
{
    public sealed record AddressSuggestion(
    string Id,              // "g:<placeId>" or "p:<poiId>"
    string Label,           // display text
    string Type,            // "google" | "poi"
    string? SecondaryText,  // optional
    double? Lat,
    double? Lng,
    string? Name
);
}

