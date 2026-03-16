using Newtonsoft.Json;

namespace TaxiDispatch.Shared.Contracts;

public class ResponseDTO
{
    public ResponseDTO()
    {
        Errors = new List<string>();
    }

    [JsonProperty("Success")]
    public bool Success { get; set; }

    [JsonProperty("Errors")]
    public List<string> Errors { get; set; }

    [JsonProperty("Error")]
    public string? Error
    {
        get => Errors.FirstOrDefault();
        set
        {
            Errors ??= new List<string>();
            Errors.Clear();

            if (!string.IsNullOrWhiteSpace(value))
            {
                Errors.Add(value);
            }
        }
    }

    [JsonProperty("Value")]
    public object? Value
    {
        get => Result;
        set => Result = value;
    }

    [JsonProperty("Result")]
    public object? Result { get; set; }
}
