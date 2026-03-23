using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: R2Lister <userPrefix>");
            return 2;
        }

        var prefix = args[0].Trim().Trim('/');

        var endpoint = Environment.GetEnvironmentVariable("R2__Endpoint") ?? Environment.GetEnvironmentVariable("R2:Endpoint");
        var accessKey = Environment.GetEnvironmentVariable("R2__AccessKeyId") ?? Environment.GetEnvironmentVariable("R2:AccessKeyId");
        var secret = Environment.GetEnvironmentVariable("R2__SecretAccessKey") ?? Environment.GetEnvironmentVariable("R2:SecretAccessKey");
        var bucket = Environment.GetEnvironmentVariable("R2__BucketName") ?? Environment.GetEnvironmentVariable("R2:BucketName") ?? "cover-art-staging";

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            Console.Error.WriteLine("R2 environment variables not set. Set R2__Endpoint, R2__AccessKeyId and R2__SecretAccessKey in the environment.");
            return 3;
        }

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        var creds = new BasicAWSCredentials(accessKey, secret);
        using var client = new AmazonS3Client(creds, s3Config);

        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                MaxKeys = 1000
            };

            Console.WriteLine($"Listing objects in bucket '{bucket}' with prefix '{prefix}'...");

            ListObjectsV2Response response;
            do
            {
                response = await client.ListObjectsV2Async(request);
                foreach (var s3Object in response.S3Objects)
                {
                    Console.WriteLine(s3Object.Key);
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return 0;
        }
        catch (AmazonS3Exception ex)
        {
            Console.Error.WriteLine($"S3 error: {ex.Message}");
            return 4;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 5;
        }
    }
}
