using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Integration tests for the add-release related endpoints using a mocked Discogs service.
    /// </summary>
    public class AddReleaseIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AddReleaseIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SearchEndpoint_ReturnsOkAndResults_WhenDiscogsServiceReturnsData()
        {
            // Arrange
            var mockDiscogs = new Mock<IDiscogsService>();
            var sample = new List<DiscogsSearchResultDto>
            {
                new DiscogsSearchResultDto { Id = "1", Title = "Album", Artist = "Artist", CatalogNumber = "CAT1" }
            };

            mockDiscogs
                .Setup(s => s.SearchByCatalogNumberAsync("CAT1", null, null, null))
                .ReturnsAsync(sample);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the IDiscogsService registration with our mock
                    services.AddSingleton(mockDiscogs.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/discogs/search?catalogNumber=CAT1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<DiscogsSearchResultDto>>();
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("CAT1", results[0].CatalogNumber);
        }
    }
}
