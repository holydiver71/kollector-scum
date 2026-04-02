using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="DiscogsHttpClient"/> focusing on rate-limit handling,
    /// 429 retry behaviour, and header tracking.
    /// </summary>
    public class DiscogsHttpClientTests
    {
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Minimal <see cref="HttpMessageHandler"/> that returns a pre-configured
        /// sequence of <see cref="HttpResponseMessage"/> objects in order.
        /// </summary>
        private sealed class SequentialHttpHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public SequentialHttpHandler(IEnumerable<HttpResponseMessage> responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (_responses.Count == 0)
                    throw new InvalidOperationException("No more responses queued.");

                return Task.FromResult(_responses.Dequeue());
            }
        }

        private static HttpResponseMessage OkResponse(string body = "{}", int remaining = 55, int total = 60)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
            response.Headers.Add("X-Discogs-Ratelimit", total.ToString());
            response.Headers.Add("X-Discogs-Ratelimit-Remaining", remaining.ToString());
            return response;
        }

        private static HttpResponseMessage TooManyRequestsResponse(int retryAfterSeconds = 1)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds));
            response.Headers.Add("X-Discogs-Ratelimit", "60");
            response.Headers.Add("X-Discogs-Ratelimit-Remaining", "0");
            return response;
        }

        private static DiscogsHttpClient CreateClient(params HttpResponseMessage[] responses)
        {
            var handler = new SequentialHttpHandler(responses);
            var httpClient = new HttpClient(handler);
            var settings = Options.Create(new DiscogsSettings
            {
                BaseUrl = "https://api.discogs.com",
                TimeoutSeconds = 30,
                UserAgent = "KollectorScumTests/1.0"
            });
            var logger = new Mock<ILogger<DiscogsHttpClient>>().Object;
            return new DiscogsHttpClient(httpClient, settings, logger);
        }

        // -----------------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------------

        [Fact]
        public async Task GetReleaseDetailsAsync_SuccessResponse_ReturnsBody()
        {
            // Arrange
            var client = CreateClient(OkResponse("{\"id\":1}"));

            // Act
            var result = await client.GetReleaseDetailsAsync("1");

            // Assert
            Assert.Equal("{\"id\":1}", result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_Non200_ReturnsNull()
        {
            // Arrange
            var client = CreateClient(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var result = await client.GetReleaseDetailsAsync("999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_429ThenOk_RetriesAndReturnsBody()
        {
            // Arrange — first call returns 429 with Retry-After: 0s, second succeeds.
            var client = CreateClient(
                TooManyRequestsResponse(retryAfterSeconds: 0),
                OkResponse("{\"id\":2}"));

            // Act
            var result = await client.GetReleaseDetailsAsync("2");

            // Assert — should succeed on the retry
            Assert.Equal("{\"id\":2}", result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_Repeated429_ExceedsRetries_ReturnsNull()
        {
            // Arrange — all 4 attempts (initial + 3 retries) return 429.
            var client = CreateClient(
                TooManyRequestsResponse(0),
                TooManyRequestsResponse(0),
                TooManyRequestsResponse(0),
                TooManyRequestsResponse(0));

            // Act
            var result = await client.GetReleaseDetailsAsync("3");

            // Assert — after MaxRetryAttempts + 1 calls, returns null gracefully
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserCollectionAsync_SuccessResponse_ReturnsBody()
        {
            // Arrange
            var client = CreateClient(OkResponse("{\"releases\":[]}"));

            // Act
            var result = await client.GetUserCollectionAsync("testuser", 1, 100);

            // Assert
            Assert.Equal("{\"releases\":[]}", result);
        }

        [Fact]
        public async Task SearchReleasesAsync_SuccessResponse_ReturnsBody()
        {
            // Arrange
            var client = CreateClient(OkResponse("{\"results\":[]}"));

            // Act
            var result = await client.SearchReleasesAsync("CATNO-001");

            // Assert
            Assert.Equal("{\"results\":[]}", result);
        }
    }
}
