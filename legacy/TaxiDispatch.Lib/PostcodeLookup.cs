using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Net;


namespace TaxiDispatch
{
    public class PostcodeLookup
    {

        private readonly ILogger<PostcodeLookup> _logger;

        // Parameterless constructor - uses a NullLogger so existing callers keep working.
        public PostcodeLookup() : this(NullLogger<PostcodeLookup>.Instance)
        {
        }

        // Constructor for DI - accepts an ILogger<PostcodeLookup>.
        public PostcodeLookup(ILogger<PostcodeLookup> logger)
        {
            _logger = logger ?? NullLogger<PostcodeLookup>.Instance;
        }

        public FindAddressResponse LookupAddress(string postcode, string houseNumber = null)
        {
            _logger.LogDebug("LookupAddress called with postcode='{Postcode}' houseNumber='{HouseNumber}'", postcode, houseNumber);

            if (string.IsNullOrWhiteSpace(postcode))
            {
                _logger.LogWarning("LookupAddress called with null or empty postcode.");
                return null;
            }

            try
            {
                using var wc = new WebClient();
                wc.Proxy = null;

                // Add required headers for findaddress.io
                wc.Headers.Add("x-api-key", "d1d4a349582f632d1d8c3eccd6db6dbc830e5339ebda965d7c6a76d51d62c91b");
                wc.Headers.Add("referer", "abacusonline.net");
                wc.Headers.Add("content-type", "application/json");
                wc.Headers.Add("cache-control", "no-cache");

                _logger.LogDebug("Headers set for findaddress.io request.");

                // Remove spaces from postcode as recommended
                var cleanPostcode = postcode.Replace(" ", "");
                var house = string.IsNullOrEmpty(houseNumber) ? "NULL" : houseNumber.Replace(" ", "");

                var url = $"https://findaddress.io/API/{house}/{cleanPostcode}";
                _logger.LogDebug("Requesting URL: {Url}", url);

                var str = wc.DownloadString(url);

                if (!string.IsNullOrEmpty(str))
                {
                    var res = JsonConvert.DeserializeObject<FindAddressResponse>(str);
                    if (res != null)
                    {
                        _logger.LogInformation("LookupAddress result: Result={Result} CsvAddress={CsvAddress} Latitude={Latitude} Longitude={Longitude} CallsRemaining={CallsRemaining} CreditsRemaining={CreditsRemaining}",
                            res.Result, res.CsvAddress, res.Latitude, res.Longitude, res.CallsRemaining, res.CreditsRemaining);
                    }
                    else
                    {
                        _logger.LogWarning("LookupAddress returned empty or invalid JSON for postcode='{Postcode}'", postcode);
                    }

                    return res;
                }

                _logger.LogDebug("LookupAddress received empty response for postcode='{Postcode}'", postcode);
            }
            catch (WebException wex)
            {
                // 404 not found or other web errors
                _logger.LogWarning(wex, "WebException during LookupAddress for postcode='{Postcode}' house='{HouseNumber}'", postcode, houseNumber);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception during LookupAddress for postcode='{Postcode}'", postcode);
                throw;
            }

            return null;
        }
    }

    public class FindAddressResponse
    {
        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("callsRemaining")]
        public int? CallsRemaining { get; set; }

        [JsonProperty("creditsRemaining")]
        public int? CreditsRemaining { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("expandedAddress")]
        public ExpandedAddress ExpandedAddress { get; set; }

        [JsonProperty("csvAddress")]
        public string CsvAddress { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("errorMsg")]
        public string ErrorMsg { get; set; }

        public bool IsSuccess => Result == "Success";
        public bool IsPartialMatch => Result == "Partial match";
    }

    public class ExpandedAddress
    {
        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("locality")]
        public string Locality { get; set; }

        [JsonProperty("town")]
        public string Town { get; set; }

        [JsonProperty("district")]
        public string District { get; set; }

        [JsonProperty("county")]
        public string County { get; set; }

        [JsonProperty("pCode")]
        public string PCode { get; set; }

        public string FormattedAddress { get { return Street + ", " + Town + ", " + County; } }
        public string FormattedAddressPC { get { return Street + ", " + Town + ", " + County + ", " + PCode; } }
    }
}

