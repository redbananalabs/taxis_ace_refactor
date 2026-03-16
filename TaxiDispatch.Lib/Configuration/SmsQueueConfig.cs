namespace TaxiDispatch.Configuration;

public class SmsQueueConfig
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string QueueName { get; set; } = "messages";

    public string RoutingKey { get; set; } = string.Empty;

    public string Exchange { get; set; } = "AceExchange";

    public string ClientProvidedName { get; set; } = "TaxiDispatch";
}
