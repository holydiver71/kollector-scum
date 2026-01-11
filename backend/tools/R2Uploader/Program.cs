using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: R2Uploader <sourceFolder> <userId>");
            return 2;
        }

        var sourceFolder = args[0];
        var userId = args[1];

        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine($"Source folder not found: {sourceFolder}");
            return 3;
        }

        var endpoint = Environment.GetEnvironmentVariable("R2__Endpoint") ?? Environment.GetEnvironmentVariable("R2:Endpoint");
        var accessKey = Environment.GetEnvironmentVariable("R2__AccessKeyId") ?? Environment.GetEnvironmentVariable("R2:AccessKeyId");
        var secret = Environment.GetEnvironmentVariable("R2__SecretAccessKey") ?? Environment.GetEnvironmentVariable("R2:SecretAccessKey");
        var bucket = Environment.GetEnvironmentVariable("R2__BucketName") ?? Environment.GetEnvironmentVariable("R2:BucketName") ?? "cover-art";

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            Console.WriteLine("R2 environment variables not set. Set R2__Endpoint, R2__AccessKeyId and R2__SecretAccessKey.");
            return 4;
        }

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        };

        var creds = new BasicAWSCredentials(accessKey, secret);
        using var client = new AmazonS3Client(creds, s3Config);

        var files = Directory.GetFiles(sourceFolder);
        Console.WriteLine($"Found {files.Length} files in {sourceFolder}");

        var uploaded = 0;
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var key = $"{userId}/{fileName}";
            try
            {
                // Read into memory to ensure the SDK knows the content length and avoids chunked signing
                using var fileStream = File.OpenRead(file);
                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                ms.Position = 0;

                var put = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = ms,
                    ContentType = GetContentType(fileName),
                    CannedACL = S3CannedACL.PublicRead
                };

                Console.WriteLine($"Uploading {fileName} -> {bucket}/{key}");
                await client.PutObjectAsync(put);
                uploaded++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to upload {fileName}: {ex.Message}");
            }
        }

        Console.WriteLine($"Uploaded {uploaded}/{files.Length} files to {bucket} under {userId}/");
        return 0;
    }

    static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }
}
