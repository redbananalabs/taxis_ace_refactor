using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;

namespace TaxiDispatch.Integrations.Aws;

public class AmazonAWSService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AWSConfig _awsConfig;

    private bool _configured;

    public AmazonAWSService(IAmazonS3 s3Client, AWSConfig config)
    {
        _s3Client = s3Client;
        _awsConfig = config;
        RegisterCredentials();
    }

    private void RegisterCredentials()
    {
        var options = new CredentialProfileOptions
        {
            AccessKey = _awsConfig.LiveMode ? _awsConfig.LiveKey : _awsConfig.DevKey,
            SecretKey = _awsConfig.LiveMode ? _awsConfig.LiveSecret : _awsConfig.DevSecret
        };
        var profile = new CredentialProfile("basic_profile", options) { Region = RegionEndpoint.EUWest2 };
        var netSDKFile = new NetSDKCredentialsFile();

        netSDKFile.RegisterProfile(profile);
        _configured = true;
    }

    public async Task CreateBucket(string bucketName)
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
        if (bucketExists)
        {
            throw new Exception($"Bucket {bucketName} already exists.");
        }

        await _s3Client.PutBucketAsync(bucketName);
    }

    public async Task<IEnumerable<string>> GetAllBuckets()
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        var data = await _s3Client.ListBucketsAsync();
        return data.Buckets.Select(b => b.BucketName);
    }

    public async Task DeleteBucket(string bucketName)
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        await _s3Client.DeleteBucketAsync(bucketName);
    }

    public async Task Upload(string file, string folder, string bucketName)
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

        if (!bucketExists)
        {
            throw new Exception($"Bucket {bucketName} does not exist.");
        }

        var keyName = $"{folder}/{Path.GetFileName(file)}";
        var ext = Path.GetExtension(file);
        var contype = ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            ".png" => "image/png",
            _ => string.Empty
        };

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = keyName,
            FilePath = file,
            ContentType = contype
        };
        request.Metadata.Add("x-amz-meta-title", Path.GetFileName(file));
        var res = await _s3Client.PutObjectAsync(request);
    }

    public async Task DeleteFile(string bucketName, string key)
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
        if (!bucketExists)
        {
            throw new Exception($"Bucket {bucketName} does not exist");
        }

        await _s3Client.DeleteObjectAsync(bucketName, key);
    }

    public async Task<GetObjectResponse> GetFileByKey(string bucketName, string key)
    {
        if (!_configured)
        {
            RegisterCredentials();
        }

        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
        if (!bucketExists)
        {
            throw new Exception($"Bucket {bucketName} does not exist.");
        }

        var s3Object = await _s3Client.GetObjectAsync(bucketName, key);
        return s3Object;
    }
}
