using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace TaxiDispatch.Configuration
{
    public class DictionaryToJsonConverter : ValueConverter<Dictionary<string, string>, string>
    {
        public DictionaryToJsonConverter() : base(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v))
        {
        }
    }

    public class DictionaryValueComparer : ValueComparer<Dictionary<string, string>>
    {
        public DictionaryValueComparer()
            : base(
                (d1, d2) => JsonConvert.SerializeObject(d1) == JsonConvert.SerializeObject(d2), // Compare JSON strings
                d => d == null ? 0 : JsonConvert.SerializeObject(d).GetHashCode(), // Get hash code of JSON string
                d => d == null ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(d)) // Clone dictionary
            )
        {
        }
    }
}

