namespace TaxiDispatch.Integrations.Aws;

public class AWSConfig
{
    public string BucketName { get; set; }

    public string LiveKey { get; set; }

    public string LiveSecret { get; set; }

    public string DevKey { get; set; }

    public string DevSecret { get; set; }

    public bool LiveMode { get; set; }
}
