using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    public class CloudflareR2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string? _publicBaseUrl;
        private readonly ILogger<CloudflareR2StorageService> _logger;

        public CloudflareR2StorageService(IConfiguration configuration, ILogger<CloudflareR2StorageService> logger)
        {
            _logger = logger;

            var endpoint = configuration["R2:Endpoint"] ?? configuration["R2__Endpoint"];
            var accessKey = configuration["R2:AccessKeyId"] ?? configuration["R2__AccessKeyId"];
            var secret = configuration["R2:SecretAccessKey"] ?? configuration["R2__SecretAccessKey"];
            _bucketName = configuration["R2:BucketName"] ?? configuration["R2__BucketName"] ?? "cover-art";
            _publicBaseUrl = configuration["R2:PublicBaseUrl"] ?? configuration["R2__PublicBaseUrl"];

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("R2 storage is not fully configured. Ensure R2__Endpoint, R2__AccessKeyId and R2__SecretAccessKey are set.");
            }

            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            };

            var credentials = new BasicAWSCredentials(accessKey, secret);
            _s3Client = new AmazonS3Client(credentials, s3Config);
        }

        public async Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType)
        {
            var safeFileName = Path.GetFileName(fileName);
            var key = $"{userId}/{safeFileName}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName ?? _bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType
                };

                // Make the object public by default; buckets can be configured differently if desired
                putRequest.CannedACL = S3CannedACL.PublicRead;

                await _s3Client.PutObjectAsync(putRequest);

                return GetPublicUrl(bucketName ?? _bucketName, userId, safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {File} to R2 bucket {Bucket}", fileName, bucketName ?? _bucketName);
                throw;
            }
        }

        public async Task DeleteFileAsync(string bucketName, string userId, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var key = $"{userId}/{safeFileName}";

            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName ?? _bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {File} from R2 bucket {Bucket}", fileName, bucketName ?? _bucketName);
                throw;
            }
        }

        public string GetPublicUrl(string bucketName, string userId, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var objectPath = $"{userId}/{safeFileName}";

            if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
            {
                return $"{_publicBaseUrl.TrimEnd('/')}/{bucketName}/{objectPath}";
            }

            // Fallback to ServiceURL style: https://{account}.r2.cloudflarestorage.com/{bucket}/{object}
            var serviceUrl = (_s3Client.Config.ServiceURL ?? string.Empty).TrimEnd('/');
            return $"{serviceUrl}/{bucketName}/{objectPath}";
        }
    }
}
