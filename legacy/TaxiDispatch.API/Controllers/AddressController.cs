using TaxiDispatch;
using TaxiDispatch.DTOs.Address;
using TaxiDispatch.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly IAddressLookupService _service;

        public AddressController(IAddressLookupService service)
        {
            _service = service;
        }

        [HttpGet("DispatchSearch")]
        public async Task<ActionResult<IReadOnlyList<AddressSuggestion>>> DispatchSearch(
               [FromQuery] string q,
               [FromQuery] string sessionToken,
               CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
                return Ok(Array.Empty<AddressSuggestion>());

            if (string.IsNullOrWhiteSpace(sessionToken))
                return BadRequest("sessionToken required");

            var poiResults = await _service.GetPoisForDispatch(q);         // returns AddressSuggestion list with Type="poi"
            //var googleResults = await _service.SearchAsync(q, sessionToken, ct);
            var idealResults = await _service.IdealSearchAddress(q);

            //var merged = poiResults.Concat(googleResults).Take(20).ToList();
            var merged = poiResults.Concat(idealResults).Take(20).ToList();

            return Ok(merged);
        }

        [HttpGet("WebBookerSearch")]
        public async Task<ActionResult<IReadOnlyList<AddressSuggestion>>> WebBookerSearch(
               [FromQuery] string q,
               [FromQuery] string sessionToken,
               CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
                return Ok(Array.Empty<AddressSuggestion>());

            if (string.IsNullOrWhiteSpace(sessionToken))
                return BadRequest("sessionToken required");

            //var poiResults = await _service.GetPoisForDispatch(q);         // returns AddressSuggestion list with Type="poi"
            var googleResults = await _service.SearchAsync(q, sessionToken, ct);

            // var merged = poiResults.Concat(googleResults).Take(20).ToList();
            return Ok(googleResults);
        }



        [HttpGet("Resolve")]
        public async Task<ActionResult<ResolvedAddress>> Resolve(
            [FromQuery] string id,
            [FromQuery] string sessionToken,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("id required");

            if (id.StartsWith("p:", StringComparison.OrdinalIgnoreCase))
            {
                var poiId = id[2..];
                var poi = await _service.ResolvePOIAsync(poiId);       // returns ResolvedAddress Source="poi"
                return Ok(poi);
            }

            if (id.StartsWith("g:", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(sessionToken))
                    return BadRequest("sessionToken required for google resolution");

                var placeId = id[2..];
                var resolved = await _service.ResolveGooglePlaceAsync(placeId, sessionToken, ct);
                return Ok(resolved);
            }

            if (id.StartsWith("i:", StringComparison.OrdinalIgnoreCase))
            {
                var placeId = id[2..];
                var resolved = await _service.ResolveIdealAddressAsync(placeId, ct);
                return Ok(resolved);
            }


            return BadRequest("Unknown id format");
        }

        [HttpGet("PostcodeLookup")]
        public async Task<ActionResult<FindAddressResponse>> PostcodeLookup([FromQuery] string postcode)
        {
            if (string.IsNullOrWhiteSpace(postcode) || postcode.Length < 3)
                return Ok(Array.Empty<AddressSuggestion>());
            var res = await _service.PostcodeLookup(postcode, "");
            return Ok(res);
        }

        [HttpGet("IdealSearch")]
        public async Task<ActionResult<ResolvedAddress>> Ideal([FromQuery] string query)
        {
            var res = await _service.IdealSearchAddress(query);
            return Ok(res);
        }

        [HttpGet("IdealPostcode")]
        public async Task<ActionResult<ResolvedAddress>> IdealPostcode([FromQuery] string postcode)
        {
            var res = await _service.IdealPostcodeSearch(postcode);
            return Ok(res);
        }

    }
}

