namespace TaxiDispatch.Configuration;

public class CallEventsConfig
{
    public string AppId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public string Cluster { get; set; } = "eu";

    public string Channel { get; set; } = "my-channel";

    public string EventName { get; set; } = "my-event";
}
