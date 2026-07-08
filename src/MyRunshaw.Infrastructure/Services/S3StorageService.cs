using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Storage;

namespace MyRunshaw.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IConfiguration config, ILogger<S3StorageService> logger)
    {
        _logger = logger;
        _bucketName = config["S3:BucketName"] ?? "profiles";
        _publicBaseUrl = config["S3:PublicBaseUrl"]!;

        var s3Config = new AmazonS3Config
        {
            ServiceURL = config["S3:Endpoint"],
            ForcePathStyle = true, // garage requires this
            AuthenticationRegion = "us-east-1"
        };

        _s3Client = new AmazonS3Client(
            config["S3:AccessKey"],
            config["S3:SecretKey"],
            s3Config
        );
    }

    public async Task<string> UploadPublicFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = false
        };

        try
        {
            await _s3Client.PutObjectAsync(request);
            return $"{_publicBaseUrl}{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {FileName} to S3", fileName);
            throw new Exception("Failed to upload image to storage.");
        }
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName
        };

        try
        {
            await _s3Client.DeleteObjectAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {FileName} from S3", fileName);
            throw new Exception("Failed to delete image from storage.");
        }
    }
}