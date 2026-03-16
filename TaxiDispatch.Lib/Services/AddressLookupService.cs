using TaxiDispatch.Data;
using TaxiDispatch.Domain;
using TaxiDispatch.Domain.IdealPostcodes;
using TaxiDispatch.DTOs.Address;
using TaxiDispatch.Interfaces;
using AutoMapper.Configuration.Conventions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Mathematics;
using System.Text;
using System.Text.Json;
using static Dropbox.Api.Files.ThumbnailFormat;
using static System.Net.Mime.MediaTypeNames;

namespace TaxiDispatch.Services
{
    public class AddressLookupService : BaseService<AddressLookupService>, IAddressLookupService
    {
        private readonly string _googlePlacesApiKey;
        private readonly HttpClient _http;
        private readonly LocalPOIService _localPOIService;
        private readonly IdealPostcodesClient _idealPostcodes;

        public AddressLookupService(
            HttpClient http,
            LocalPOIService localPOIService,
            IdealPostcodesClient idealPostcodes,
            IConfiguration config,
            IDbContextFactory<TaxiDispatchContext> factory,
            ILogger<AddressLookupService> logger)
            : base(factory, logger)
        {
            _http = http;
            _localPOIService = localPOIService;
            _idealPostcodes = idealPostcodes;
            _googlePlacesApiKey = config["Google:PlacesApiKey"]
                      ?? throw new InvalidOperationException("Google:PlacesApiKey missing");
        }

        public async Task<List<DTOs.Address.AddressSuggestion>> IdealSearchAddress(string query)
        {
            var list = new List<DTOs.Address.AddressSuggestion>();

            var options = new Domain.IdealPostcodes.AutocompleteOptions
            {
                Limit = 20,
                //BiasLonLat = $"{centerLng},{centerLat}"
                BiasPostcodeOutward = "SP8,SP7", PostcodeArea = "SP,BA,DT"
            };

            var data = await _idealPostcodes.AutocompleteAddressAsync(query, options);

            foreach (var item in data)
            {
                list.Add(new DTOs.Address.AddressSuggestion(
                      Id: $"i:{item.Id}",
                      Label: item.Suggestion,
                      Type: "ideal",
                      SecondaryText: null,
                      Lat: null,
                      Lng: null,
                      Name: null
                  ));
            }

            return list;
        }

        public async Task<List<Domain.IdealPostcodes.Address>> IdealPostcodeSearch(string postcode)
        {
            return await _idealPostcodes.LookupPostcodeAsync(postcode);
        }

        public async Task<ResolvedAddress> ResolveIdealAddressAsync(string placeId, CancellationToken ct)
        { 
            var data = await _idealPostcodes.ResolveAddressAsync(placeId,ct);

            var displayLabel = BuildDisplayLabel(data.BuildingName, data.BuildingNumber + " " + data.Thoroughfare, data.PostTown
                , data.Postcode, "");

            return new ResolvedAddress(
                DisplayLabel: displayLabel,
                PlaceName: displayLabel,
                FormattedAddress: displayLabel ?? "",
                Postcode: data.Postcode,
                Line1: null,
                Line2: null,
                TownCity: null,
                County: null,
                Lat: null,
                Lng: null,
                Source: "ideal",
                GooglePlaceId: placeId
            );
        }

        public async Task<IReadOnlyList<DTOs.Address.AddressSuggestion>> SearchAsync(
            string q, string sessionToken, CancellationToken ct)
        {
            var url = "https://places.googleapis.com/v1/places:autocomplete";

            const double radiusMeters = 5 * 1609.344; // 32186.88
            const double centerLat = 51.0478;
            const double centerLng = -2.2769;

            var body = new
            {
                input = q,
                includedRegionCodes = new[] { "GB" },
                locationBias = new
                {
                    circle = new
                    {
                        center = new { latitude = centerLat, longitude = centerLng },
                        radius = radiusMeters
                    }
                },
                origin = new { latitude = centerLat, longitude = centerLng },
                sessionToken = sessionToken,
                regionCode = "GB",
                languageCode = "en-GB"
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("X-Goog-Api-Key", _googlePlacesApiKey);
            req.Headers.Add("X-Goog-FieldMask",
                "suggestions.placePrediction.placeId,suggestions.placePrediction.text,suggestions.placePrediction.distanceMeters");
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);

            var responseBody = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Google Places error {StatusCode}: {Body}", (int)res.StatusCode, responseBody);
                throw new HttpRequestException($"Google Places returned {(int)res.StatusCode}: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            var list = new List<DTOs.Address.AddressSuggestion>();

            if (doc.RootElement.TryGetProperty("suggestions", out var suggestions)
                && suggestions.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in suggestions.EnumerateArray())
                {
                    if (!item.TryGetProperty("placePrediction", out var pp)) continue;

                    var placeId = pp.TryGetProperty("placeId", out var pid) ? pid.GetString() : null;
                    var text = pp.TryGetProperty("text", out var t) && t.TryGetProperty("text", out var tt)
                        ? tt.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(placeId) || string.IsNullOrWhiteSpace(text))
                        continue;

                    list.Add(new DTOs.Address.AddressSuggestion(
                        Id: $"g:{placeId}",
                        Label: text!,
                        Type: "google",
                        SecondaryText: null,
                        Lat: null,
                        Lng: null,
                        Name: null
                    ));
                }
            }

            return list;
        }

        public async Task<ResolvedAddress> ResolveGooglePlaceAsync(
            string placeId, string sessionToken, CancellationToken ct)
        {
            var url =
                $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}" +
                $"?sessionToken={Uri.EscapeDataString(sessionToken)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("X-Goog-Api-Key", _googlePlacesApiKey);
            req.Headers.Add("X-Goog-FieldMask", "displayName,formattedAddress,addressComponents,location");

            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? placeName = null;
            if (root.TryGetProperty("displayName", out var dn) &&
                dn.ValueKind == JsonValueKind.Object &&
                dn.TryGetProperty("text", out var dnt))
            {
                placeName = dnt.GetString();
            }

            var formatted = root.TryGetProperty("formattedAddress", out var fa) ? fa.GetString() : "";

            string? postcode = null, streetNumber = null, route = null, town = null, county = null;

            if (root.TryGetProperty("addressComponents", out var comps) && comps.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in comps.EnumerateArray())
                {
                    var longText = c.TryGetProperty("longText", out var lt) ? lt.GetString() : null;

                    if (!c.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
                        continue;

                    var typeSet = types.EnumerateArray()
                        .Select(t => t.GetString())
                        .Where(x => x != null)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (typeSet.Contains("postal_code")) postcode = longText;
                    if (typeSet.Contains("street_number")) streetNumber = longText;
                    if (typeSet.Contains("route")) route = longText;
                    if (typeSet.Contains("postal_town")) town = longText;
                    if (typeSet.Contains("administrative_area_level_2")) county = longText;
                }
            }

            string? line1 = null;
            if (!string.IsNullOrWhiteSpace(streetNumber) && !string.IsNullOrWhiteSpace(route))
                line1 = $"{streetNumber} {route}";
            else if (!string.IsNullOrWhiteSpace(route))
                line1 = route;
            else if (!string.IsNullOrWhiteSpace(streetNumber))
                line1 = streetNumber;

            double? lat = null, lng = null;
            if (root.TryGetProperty("location", out var loc))
            {
                if (loc.TryGetProperty("latitude", out var la) && la.TryGetDouble(out var lad)) lat = lad;
                if (loc.TryGetProperty("longitude", out var lo) && lo.TryGetDouble(out var lod)) lng = lod;
            }

            var displayLabel = BuildDisplayLabel(placeName, line1, town, postcode, formatted);

            return new ResolvedAddress(
                DisplayLabel: displayLabel,
                PlaceName: placeName,
                FormattedAddress: formatted ?? "",
                Postcode: postcode,
                Line1: line1,
                Line2: null,
                TownCity: town,
                County: county,
                Lat: lat,
                Lng: lng,
                Source: "google",
                GooglePlaceId: placeId
            );
        }

        private static string BuildDisplayLabel(
            string? placeName, string? line1, string? town, string? postcode, string? formattedFallback)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(placeName)) parts.Add(placeName);
            if (!string.IsNullOrWhiteSpace(line1)) parts.Add(line1);

            var townPost = string.Join(", ", new[] { town, postcode }.Where(x => !string.IsNullOrWhiteSpace(x)));
            //if (!string.IsNullOrWhiteSpace(townPost)) parts.Add(townPost);
            if (!string.IsNullOrWhiteSpace(town)) parts.Add(town);

            var label = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(label) ? (formattedFallback ?? "") : label;
        }

        public async Task<ResolvedAddress> ResolveAsync(string id, string sessionToken, CancellationToken ct)
        {
            if (id.StartsWith("p:", StringComparison.OrdinalIgnoreCase))
                return await ResolvePOIAsync(id.Substring(2));

            if (id.StartsWith("g:", StringComparison.OrdinalIgnoreCase))
                return await ResolveGooglePlaceAsync(id.Substring(2), sessionToken, ct);

            throw new InvalidOperationException($"Unknown address ID format: '{id}'");
        }

        public async Task<ResolvedAddress> ResolvePOIAsync(string poiId)
        {
            var id = Convert.ToInt32(poiId);
            var data = await _localPOIService.GetLocalPOIById(id);

            if (data == null) throw new InvalidOperationException("POI not found");

            return new ResolvedAddress(
                DisplayLabel: data.Address,
                PlaceName: data.Name,
                FormattedAddress: data.Address,
                Postcode: data.Postcode,
                Line1: data.Address,
                Line2: null,
                TownCity: null,
                County: null,
                Lat: 0,
                Lng: 0,
                Source: "poi",
                GooglePlaceId: null
            );
        }

        public async Task<List<DTOs.Address.AddressSuggestion>> GetPoisForDispatch(string search)
        {
            var data = await _localPOIService.GetLocalPOI(search);
            var lst = new List<DTOs.Address.AddressSuggestion>();

            data.Select(o => new DTOs.Address.AddressSuggestion(
                Id: $"p:{o.Id}",
                Label: o.Address,
                Type: "poi",
                SecondaryText: o.Postcode,
                Lat: 0,
                Lng: 0,
                Name: o.Name
            )).ToList().ForEach(lst.Add);

            return lst;
        }

        public async Task<List<DTOs.Address.AddressSuggestion>> GetPoisForWebBooker(string search)
        {
            var pois = await _dB.LocalPOIs
                .Where(o => (o.Type != LocalPOIType.House
                             && o.Type != LocalPOIType.Airport
                             && o.Type != LocalPOIType.Ferry_Port)
                            && o.Address.StartsWith(search) || o.Postcode.StartsWith(search))
                .ToListAsync();

            var lst = new List<DTOs.Address.AddressSuggestion>();

            pois.Select(o => new DTOs.Address.AddressSuggestion(
                Id: $"p:{o.Id}",
                Label: o.Address,
                Type: "poi",
                SecondaryText: o.Postcode,
                Lat: 0,
                Lng: 0,
                Name: o.Name
            )).ToList().ForEach(lst.Add);

            return lst;
        }

        public async Task<FindAddressResponse> PostcodeLookup(string postcode, string? houseNumber)
        {
            var pc = new PostcodeLookup();
            return pc.LookupAddress(postcode, houseNumber);
        }
    }
}


