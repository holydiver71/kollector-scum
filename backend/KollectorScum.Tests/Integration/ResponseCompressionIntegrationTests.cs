using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Integration tests for response compression behavior.
    /// </summary>
    public class ResponseCompressionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ResponseCompressionIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RuntimeInfo_WithGzipAcceptEncoding_ReturnsGzipCompressedJson()
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/runtime-info");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("gzip", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress);
            using var decompressedReader = new StreamReader(gzipStream);
            var decompressedJson = await decompressedReader.ReadToEndAsync();

            using var jsonDoc = JsonDocument.Parse(decompressedJson);
            Assert.True(jsonDoc.RootElement.TryGetProperty("environment", out _));
            Assert.True(jsonDoc.RootElement.TryGetProperty("databaseTarget", out _));
        }
    }
}
