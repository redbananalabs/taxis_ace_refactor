using TaxiDispatch.DTOs.Address;
using TaxiDispatch.DTOs.LocalPOI;

namespace TaxiDispatch.Interfaces
{
    public interface IAddressLookupService
    {
        Task<IReadOnlyList<AddressSuggestion>> SearchAsync(string q, string sessionToken, CancellationToken ct);
        Task<ResolvedAddress> ResolveGooglePlaceAsync(string placeId, string sessionToken, CancellationToken ct);
        Task<ResolvedAddress> ResolvePOIAsync(string id);
        Task<List<AddressSuggestion>> GetPoisForDispatch(string search);
        Task<List<AddressSuggestion>> GetPoisForWebBooker(string search);
        Task<FindAddressResponse> PostcodeLookup(string postcode, string? houseNumber);
        Task<List<AddressSuggestion>> IdealSearchAddress(string query);
        Task<List<Domain.IdealPostcodes.Address>> IdealPostcodeSearch(string postcode);
        Task<ResolvedAddress> ResolveIdealAddressAsync(string placeId, CancellationToken ct);
    }
}

