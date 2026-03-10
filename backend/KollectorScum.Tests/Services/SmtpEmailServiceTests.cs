using System.Net.Http;
using System.Threading.Tasks;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class SmtpEmailServiceTests
    {
        /// <summary>Creates a mock IHttpClientFactory (unused in dev-fallback path).</summary>
        private static Mock<IHttpClientFactory> MockHttpClientFactory()
        {
            var mock = new Mock<IHttpClientFactory>();
            mock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());
            return mock;
        }

        [Fact]
        public async Task SendMagicLinkEmailAsync_WhenSmtpHostNotConfigured_LogsWarningAndReturns()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockEmailSection = new Mock<IConfigurationSection>();

            // Email section with empty SmtpHost (dev fallback)
            mockEmailSection.Setup(x => x["SmtpHost"]).Returns(string.Empty);
            mockEmailSection.Setup(x => x["SmtpPort"]).Returns("587");
            mockEmailSection.Setup(x => x["SmtpUsername"]).Returns(string.Empty);
            mockEmailSection.Setup(x => x["SmtpPassword"]).Returns(string.Empty);
            mockEmailSection.Setup(x => x["FromAddress"]).Returns("noreply@kollector.app");
            mockEmailSection.Setup(x => x["FromName"]).Returns("Kollector Scüm");
            mockEmailSection.Setup(x => x["EnableSsl"]).Returns("true");

            mockConfig.Setup(x => x.GetSection("Email")).Returns(mockEmailSection.Object);

            var mockLogger = new Mock<ILogger<SmtpEmailService>>();

            var service = new SmtpEmailService(mockConfig.Object, mockLogger.Object, MockHttpClientFactory().Object);

            // Act
            var ex = await Record.ExceptionAsync(() => service.SendMagicLinkEmailAsync("user@example.com", "https://app.example.com/auth/magic-link?token=abc"));

            // Assert - no exception thrown and warning was logged
            Assert.Null(ex);

            // Verify via recorded invocations to avoid delegate generic type mismatches
            var found = false;
            foreach (var inv in mockLogger.Invocations)
            {
                if (inv.Method.Name == "Log" && inv.Arguments.Count > 2)
                {
                    var state = inv.Arguments[2];
                    if (state != null && state.ToString().Contains("SMTP host is not configured"))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.True(found, "Expected a warning log containing 'SMTP host is not configured'.");
        }
    }
}
