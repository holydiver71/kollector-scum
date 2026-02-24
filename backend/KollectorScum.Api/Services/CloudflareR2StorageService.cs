using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private static readonly HttpClient HttpClient = new HttpClient();
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
                ForcePathStyle = true,
                // Cloudflare R2 recommends using the special "auto" region
                // for signature calculation when talking to the S3-compatible API.
                AuthenticationRegion = "auto"
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
                var effectiveBucket = bucketName ?? _bucketName;
                _logger.LogInformation("Uploading file to R2. Bucket: {Bucket}, Key: {Key}, Content-Length: {Length}", 
                    effectiveBucket, key, fileStream.Length);

                // Use PutObjectAsync directly.
                var putRequest = new PutObjectRequest
                {
                    BucketName = effectiveBucket,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    DisablePayloadSigning = true // Required for R2 to avoid STREAMING-AWS4-HMAC-SHA256-PAYLOAD
                };

                // Ensure we don't close the stream (good practice if stream is reused, though here it's disposed by caller)
                putRequest.AutoCloseStream = false;

                var response = await _s3Client.PutObjectAsync(putRequest);
                
                _logger.LogInformation("R2 Upload Response: Status={Status}, RequestId={RequestId}", 
                    response.HttpStatusCode, response.ResponseMetadata?.RequestId);

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK && 
                    response.HttpStatusCode != System.Net.HttpStatusCode.Created && 
                    response.HttpStatusCode != System.Net.HttpStatusCode.Accepted)
                {
                     _logger.LogWarning("Unexpected R2 status code: {StatusCode}", response.HttpStatusCode);
                }
                
                return GetPublicUrl(effectiveBucket, userId, safeFileName);
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
                // Expose a stable public URL that does NOT embed the internal bucket name.
                // This decouples public paths from bucket naming and makes renames/routing easier.
                return $"{_publicBaseUrl.TrimEnd('/')}/{objectPath}";
            }

            // Fallback to ServiceURL style (includes bucket name) when no explicit public base URL is configured.
            var serviceUrl = (_s3Client.Config.ServiceURL ?? string.Empty).TrimEnd('/');
            return $"{serviceUrl}/{bucketName}/{objectPath}";
        }

        /// <summary>
        /// Downloads a file from R2 and returns its content stream.
        /// Returns null when the object is not found.
        /// </summary>
        public async Task<Stream?> GetFileStreamAsync(string bucketName, string userId, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var key = $"{userId}/{safeFileName}";
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName ?? _bucketName,
                    Key = key,
                };
                var response = await _s3Client.GetObjectAsync(request);
                // Copy to a MemoryStream so the caller owns the data after the S3 response is disposed
                var ms = new MemoryStream();
                await response.ResponseStream.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("R2 object not found: bucket={Bucket}, key={Key}", bucketName, key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from R2: bucket={Bucket}, key={Key}", bucketName, key);
                return null;
            }
        }
    }
}
