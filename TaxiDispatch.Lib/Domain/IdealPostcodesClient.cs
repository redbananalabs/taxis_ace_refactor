using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TaxiDispatch.Domain.IdealPostcodes
{

    /// <summary>
    /// C# client for the Ideal Postcodes API (https://docs.ideal-postcodes.co.uk/docs/api)
    ///
    /// NOTE: There is no official NuGet package for C# from Ideal Postcodes.
    /// They provide SDKs only for JavaScript/Node.js and Ruby.
    /// This class wraps the REST API directly.
    ///
    /// Usage:
    ///   var client = new IdealPostcodesClient("your_api_key");
    ///   var suggestions = await client.AutocompleteAddressAsync("10 Downing Street");
    ///   var address = await client.ResolveAddressAsync(suggestions[0].Id);
    /// </summary>
    public class IdealPostcodesClient : IDisposable
    {
        private const string BaseUrl = "https://api.ideal-postcodes.co.uk";

        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <param name="apiKey">Your Ideal Postcodes API key.</param>
        /// <param name="httpClient">Optional custom HttpClient (e.g. from IHttpClientFactory).</param>
        public IdealPostcodesClient(string apiKey, HttpClient? httpClient = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _http = httpClient ?? new HttpClient();
            // Do NOT set BaseAddress — we build fully qualified URLs in every method
            // to avoid the common pitfall where a leading slash strips the base path.
        }

        // -------------------------------------------------------------------------
        // Address Autocomplete (2-step flow)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Step 1 — Get address suggestions as the user types.
        /// Rate limited at 3000 requests per 5 minutes. Does NOT decrement lookup balance.
        /// </summary>
        /// <param name="query">The partial address string to search for.</param>
        /// <param name="options">Optional query filters (postcode, country, etc.).</param>
        public async Task<List<AddressSuggestion>> AutocompleteAddressAsync(
            string query,
            AutocompleteOptions? options = null,
            CancellationToken ct = default)
        {
            var url = BuildUrl("/v1/autocomplete/addresses", new Dictionary<string, string?>
            {
                ["api_key"] = _apiKey,
                ["q"] = query,
                ["limit"] = options?.Limit?.ToString(),
                // General
                ["context"] = options?.Context,
                ["datasets"] = options?.Datasets,
                // Filters
                ["postcode"] = options?.Postcode,
                ["postcode_outward"] = options?.PostcodeOutward,
                ["postcode_area"] = options?.PostcodeArea,
                ["postcode_sector"] = options?.PostcodeSector,
                ["post_town"] = options?.PostTown,
                ["locality"] = options?.Locality,
                ["country_iso"] = options?.CountryIso,
                ["su_organisation_indicator"] = options?.SuOrganisationIndicator,
                ["is_residential"] = options?.IsResidential?.ToString().ToLowerInvariant(),
                ["box"] = options?.Box,
                // Bias
                ["bias_postcode_outward"] = options?.BiasPostcodeOutward,
                ["bias_postcode_area"] = options?.BiasPostcodeArea,
                ["bias_postcode_sector"] = options?.BiasPostcodeSector,
                ["bias_post_town"] = options?.BiasPostTown,
                ["bias_locality"] = options?.BiasLocality,
                ["bias_lonlat"] = options?.BiasLonLat,
                ["bias_ip"] = options?.BiasIp?.ToString().ToLowerInvariant(),
            });

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AutocompleteResult>>(_jsonOptions, ct);
            return result?.Result?.Hits ?? new List<AddressSuggestion>();
        }

        /// <summary>
        /// Step 2 — Resolve a suggestion to a full address using the suggestion's Id.
        /// This DOES decrement your lookup balance (~2p per call).
        /// </summary>
        /// <param name="addressId">The suggestion Id returned from AutocompleteAddressAsync.</param>
        /// <param name="countryIso">ISO 3-letter country code, e.g. "gbr". Defaults to "gbr".</param>
        public async Task<Address?> ResolveAddressAsync(
            string addressId,
            CancellationToken ct = default)
        {
            string countryIso = "gbr";

            var url = BuildUrl(
                $"/v1/autocomplete/addresses/{Uri.EscapeDataString(addressId)}/{Uri.EscapeDataString(countryIso)}",
                new Dictionary<string, string?> { ["api_key"] = _apiKey });

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Address>>(_jsonOptions, ct);
            return result?.Result;
        }

        // -------------------------------------------------------------------------
        // Postcode Lookup
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all addresses at a given postcode. Decrements lookup balance.
        /// Use postcode "ID1 1QD" for free test lookups.
        /// </summary>
        public async Task<List<Address>> LookupPostcodeAsync(
            string postcode,
            CancellationToken ct = default)
        {
            var url = BuildUrl(
                $"/v1/postcodes/{Uri.EscapeDataString(postcode.Trim())}",
                new Dictionary<string, string?> { ["api_key"] = _apiKey });

            var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<Address>();

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<Address>>>(_jsonOptions, ct);
            return result?.Result ?? new List<Address>();
        }

        // -------------------------------------------------------------------------
        // Address Search (full-text)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Searches for addresses matching a free-text query. Returns up to <paramref name="limit"/> results.
        /// Decrements lookup balance.
        /// </summary>
        public async Task<AddressSearchResult> SearchAddressesAsync(
            string query,
            int limit = 10,
            int page = 0,
            CancellationToken ct = default)
        {
            var url = BuildUrl("/v1/addresses", new Dictionary<string, string?>
            {
                ["api_key"] = _apiKey,
                ["q"] = query,
                ["limit"] = limit.ToString(),
                ["page"] = page.ToString(),
            });

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AddressSearchResult>>(_jsonOptions, ct);
            return result?.Result ?? new AddressSearchResult();
        }

        // -------------------------------------------------------------------------
        // Retrieve by UDPRN
        // -------------------------------------------------------------------------

        /// <summary>
        /// Retrieves a specific address by its Royal Mail UDPRN. Decrements lookup balance.
        /// Use UDPRN 0 for free test lookups.
        /// </summary>
        public async Task<Address?> GetAddressByUdprnAsync(int udprn, CancellationToken ct = default)
        {
            var url = BuildUrl(
                $"/v1/udprn/{udprn}",
                new Dictionary<string, string?> { ["api_key"] = _apiKey });

            var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Address>>(_jsonOptions, ct);
            return result?.Result;
        }

        // -------------------------------------------------------------------------
        // Key validation (free)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Checks whether your API key is valid and usable (free, does not decrement balance).
        /// </summary>
        public async Task<bool> CheckKeyAsync(CancellationToken ct = default)
        {
            var url = BuildUrl(
                $"/v1/keys/{Uri.EscapeDataString(_apiKey)}",
                new Dictionary<string, string?> { ["api_key"] = _apiKey });

            var response = await _http.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds a fully qualified URL. Using full URLs (not relative paths on a BaseAddress)
        /// avoids the common bug where a leading slash causes HttpClient to drop the base path.
        /// </summary>
        private static string BuildUrl(string path, Dictionary<string, string?> parameters)
        {
            var sb = new StringBuilder(BaseUrl);
            sb.Append(path);

            bool first = true;
            foreach (var (key, value) in parameters)
            {
                if (string.IsNullOrEmpty(value)) continue;
                sb.Append(first ? '?' : '&');
                sb.Append(Uri.EscapeDataString(key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
                first = false;
            }

            return sb.ToString();
        }

        public void Dispose() => _http.Dispose();
    }

    // -------------------------------------------------------------------------
    // DTOs
    // -------------------------------------------------------------------------

    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public T? Result { get; set; }
    }

    public class AutocompleteResult
    {
        public List<AddressSuggestion> Hits { get; set; } = new();
    }

    /// <summary>
    /// A lightweight suggestion returned from the autocomplete endpoint.
    /// Pass <see cref="Id"/> to <see cref="IdealPostcodesClient.ResolveAddressAsync"/> to get the full address.
    /// </summary>
    public class AddressSuggestion
    {
        public string Id { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? CountryIso { get; set; }
    }

    /// <summary>
    /// A fully resolved UK address.
    /// </summary>
    public class Address
    {
        public string? Postcode { get; set; }
        public string? PostcodeInward { get; set; }
        public string? PostcodeOutward { get; set; }
        public string? PostTown { get; set; }
        public string? DependantLocality { get; set; }
        public string? DoubleDependantLocality { get; set; }
        public string? Thoroughfare { get; set; }
        public string? DependantThoroughfare { get; set; }
        public string? BuildingNumber { get; set; }
        public string? BuildingName { get; set; }
        public string? SubBuildingName { get; set; }
        public string? PoBox { get; set; }
        public string? DepartmentName { get; set; }
        public string? OrganisationName { get; set; }
        public int? Udprn { get; set; }
        public string? PostcodeType { get; set; }
        public string? SuOrganisationIndicator { get; set; }
        public string? DeliveryPointSuffix { get; set; }
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }
        public string? Premise { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public int? Eastings { get; set; }
        public int? Northings { get; set; }
        public string? Country { get; set; }
        public string? TraditionalCounty { get; set; }
        public string? AdministrativeCounty { get; set; }
        public string? PostalCounty { get; set; }
        public string? County { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }

        /// <summary>
        /// UPRN is returned as a string by the API (e.g. "100023336956").
        /// </summary>
        public string? Uprn { get; set; }

        public string? Id { get; set; }
        public string? CountryIso { get; set; }
        public string? CountryIso2 { get; set; }
        public string? CountyCode { get; set; }
        public string? Language { get; set; }

        /// <summary>
        /// UMPRN is returned as an empty string "" when not present, or a number when present.
        /// Using a custom converter to handle both cases.
        /// </summary>
        [JsonConverter(typeof(EmptyStringToNullableLongConverter))]
        public long? Umprn { get; set; }

        public string? Dataset { get; set; }
    }

    public class AddressSearchResult
    {
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Page { get; set; }
        public List<Address> Hits { get; set; } = new();
    }

    /// <summary>
    /// Handles the Ideal Postcodes quirk where UMPRN (and similar fields) is returned as
    /// an empty string "" when absent, but as a number when present.
    /// e.g.  "umprn": ""        -> null
    ///       "umprn": 50906058  -> 50906058
    /// </summary>
    public class EmptyStringToNullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (long.TryParse(s, out var parsed)) return parsed;
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetInt64();

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            throw new JsonException($"Cannot convert token type {reader.TokenType} to long?");
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Query parameters for the /autocomplete/addresses endpoint.
    ///
    /// FILTERS (hard-restrict results — no match = empty set):
    ///   Max 10 filter terms total. Combine with AND logic.
    ///   Multiple terms per filter are comma-separated, e.g. PostcodeOutward = "E1,E2,E3"
    ///
    /// BIAS (soft-boost matching results, unmatched still appear lower):
    ///   Max 5 bias terms total. Prefix is bias_ on the wire.
    /// </summary>
    public class AutocompleteOptions
    {
        // -------------------------------------------------------------------------
        // General
        // -------------------------------------------------------------------------

        /// <summary>Maximum number of suggestions to return (default 10).</summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Additional countries to search beyond the default.
        /// Comma-separated ISO-3 codes, e.g. "gbr,irl".
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Restrict results to specific datasets within a country.
        /// e.g. "nyb" for Not Yet Built (new builds only), "mr" for Multiple Residence.
        /// Comma-separated.
        /// </summary>
        public string? Datasets { get; set; }

        // -------------------------------------------------------------------------
        // Filters  (wire name = property name, snake_cased)
        // -------------------------------------------------------------------------

        /// <summary>Filter to a specific full postcode, e.g. "sw1a2aa". Supports multiple comma-separated values.</summary>
        public string? Postcode { get; set; }

        /// <summary>Filter to one or more outward (district) codes, e.g. "E1,E2,E3".</summary>
        public string? PostcodeOutward { get; set; }

        /// <summary>Filter to one or more postcode areas, e.g. "SW,SE,EC".</summary>
        public string? PostcodeArea { get; set; }

        /// <summary>Filter to one or more postcode sectors, e.g. "SW1A 2,SW1A 1".</summary>
        public string? PostcodeSector { get; set; }

        /// <summary>Filter to a specific post town / town, e.g. "London". Supports multiple comma-separated values.</summary>
        public string? PostTown { get; set; }

        /// <summary>Filter to a specific locality / dependent locality.</summary>
        public string? Locality { get; set; }

        /// <summary>Filter by ISO 3-letter country code, e.g. "gbr". Supports multiple comma-separated values.</summary>
        public string? CountryIso { get; set; }

        /// <summary>
        /// Filter to only small user organisation addresses.
        /// Pass "Y" to include only SU organisation addresses.
        /// </summary>
        public string? SuOrganisationIndicator { get; set; }

        /// <summary>
        /// Filter to only residential addresses. Pass "true" to filter.
        /// Corresponds to is_residential query param.
        /// </summary>
        public bool? IsResidential { get; set; }

        /// <summary>
        /// Geospatial bounding box filter. Restricts results to addresses within the box.
        /// Format: "west_lng,north_lat,east_lng,south_lat" e.g. "-0.2,51.6,0.0,51.4"
        /// </summary>
        public string? Box { get; set; }

        // -------------------------------------------------------------------------
        // Bias  (wire name = "bias_" + snake_case property name)
        // -------------------------------------------------------------------------

        /// <summary>Bias towards one or more postcode outward codes, e.g. "SP8" or "SP8,SP7".</summary>
        public string? BiasPostcodeOutward { get; set; }

        /// <summary>Bias towards one or more postcode areas, e.g. "SW,SE".</summary>
        public string? BiasPostcodeArea { get; set; }

        /// <summary>Bias towards one or more postcode sectors, e.g. "SW1A 2".</summary>
        public string? BiasPostcodeSector { get; set; }

        /// <summary>Bias towards a specific post town, e.g. "London".</summary>
        public string? BiasPostTown { get; set; }

        /// <summary>Bias towards a specific locality.</summary>
        public string? BiasLocality { get; set; }

        /// <summary>
        /// Bias towards a geospatial point + radius.
        /// Format: "longitude,latitude,radius_metres" e.g. "-0.118092,51.509865,5000"
        /// Max radius is 50000 metres.
        /// </summary>
        public string? BiasLonLat { get; set; }

        /// <summary>
        /// Bias results towards the approximate geolocation of the caller's IP address.
        /// Set to true to enable.
        /// </summary>
        public bool? BiasIp { get; set; }
    }
}
