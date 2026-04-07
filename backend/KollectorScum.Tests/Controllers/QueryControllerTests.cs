using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for QueryController covering SEC-01: authentication enforcement
    /// and tenant-scoped SQL execution.
    /// </summary>
    public class QueryControllerTests
    {
        // ---------- ApplyTenantScoping (static, no infrastructure needed) ----------

        [Fact]
        public void ApplyTenantScoping_PlainSelect_PrefixesTenantCtes()
        {
            // Arrange
            const string sql = "SELECT \"Title\" FROM \"MusicReleases\" LIMIT 10";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: CTE block is prepended and original SQL appears at the end
            Assert.StartsWith("WITH", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("public.\"MusicReleases\" WHERE \"UserId\" = @userId", result);
            Assert.EndsWith(sql, result);
        }

        [Fact]
        public void ApplyTenantScoping_AllAllowedTables_AreIncludedInCtes()
        {
            // Arrange
            const string sql = "SELECT \"Id\" FROM \"MusicReleases\" LIMIT 1";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: all user-owned tables shadowed
            foreach (var table in new[]
                { "MusicReleases", "Artists", "Labels", "Countries", "Formats", "Genres", "Packagings", "Stores" })
            {
                Assert.Contains($"public.\"{table}\" WHERE \"UserId\" = @userId", result);
            }
        }

        [Fact]
        public void ApplyTenantScoping_NowPlayings_ScopedViaMusicReleasesFk()
        {
            // Arrange
            const string sql = "SELECT * FROM \"NowPlayings\" LIMIT 10";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: NowPlayings CTE joins through scoped MusicReleases
            Assert.Contains("public.\"NowPlayings\" np", result);
            Assert.Contains("mr.\"UserId\" = @userId", result);
        }

        [Fact]
        public void ApplyTenantScoping_SelectWithExistingWith_MergesCtes()
        {
            // Arrange — simulate an LLM-generated query that already has a CTE
            var sql = "WITH ranked AS (SELECT \"Title\", ROW_NUMBER() OVER (ORDER BY \"DateAdded\" DESC) AS rn FROM \"MusicReleases\")\nSELECT \"Title\" FROM ranked WHERE rn <= 10 LIMIT 10";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: original CTE is preserved, tenant CTEs come first
            Assert.StartsWith("WITH", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("public.\"MusicReleases\" WHERE \"UserId\" = @userId", result);
            Assert.Contains("ranked AS", result);
        }

        [Fact]
        public void ApplyTenantScoping_UserId_ParameterPlaceholderPresent()
        {
            // Arrange
            const string sql = "SELECT COUNT(*) FROM \"MusicReleases\" LIMIT 1";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: @userId appears for parameterisation (prevents hard-coded GUID injection)
            Assert.Contains("@userId", result);
        }

        [Fact]
        public void ApplyTenantScoping_SelectWithLeadingWhitespace_HandledCorrectly()
        {
            // Arrange
            const string sql = "   \n  SELECT \"Title\" FROM \"MusicReleases\" LIMIT 5";

            // Act
            var result = QueryController.ApplyTenantScoping(sql);

            // Assert: still produces valid output (TrimStart is applied)
            Assert.StartsWith("WITH", result, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- [Authorize] attribute enforcement ----------

        [Fact]
        public void QueryController_Class_HasAuthorizeAttribute()
        {
            // Arrange
            var controllerType = typeof(QueryController);

            // Act
            var attribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        // ---------- Ask endpoint — null userId returns 401 ----------

        [Fact]
        public async Task Ask_WhenUserContextReturnsNullUserId_ReturnsUnauthorized()
        {
            // Arrange
            var mockLlm = new Mock<IQueryLLMService>();
            var mockValidation = new Mock<ISqlValidationService>();
            var mockUserContext = new Mock<IUserContext>();
            var mockLogger = new Mock<ILogger<QueryController>>();

            // IUserContext returns null — simulates a broken/missing claim
            mockUserContext.Setup(c => c.GetActingUserId()).Returns((Guid?)null);

            // DbContext is not needed for this path (returns 401 before DB is touched)
            var controller = new QueryController(
                mockLlm.Object,
                mockValidation.Object,
                dbContext: null!,
                mockUserContext.Object,
                mockLogger.Object);

            var request = new NaturalLanguageQueryDto { Question = "How many releases do I have?" };

            // Act
            var result = await controller.Ask(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<QueryResponseDto>>(result);
            Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        }

        // ---------- Ask endpoint — SQL validation failure returns 400 ----------

        [Fact]
        public async Task Ask_WhenGeneratedSqlFailsValidation_ReturnsBadRequest()
        {
            // Arrange
            var mockLlm = new Mock<IQueryLLMService>();
            var mockValidation = new Mock<ISqlValidationService>();
            var mockUserContext = new Mock<IUserContext>();
            var mockLogger = new Mock<ILogger<QueryController>>();

            var userId = Guid.NewGuid();
            mockUserContext.Setup(c => c.GetActingUserId()).Returns(userId);
            mockLlm.Setup(l => l.GenerateSqlFromNaturalLanguageAsync(It.IsAny<string>()))
                   .ReturnsAsync("DROP TABLE \"MusicReleases\"");
            mockValidation.Setup(v => v.Sanitize(It.IsAny<string>()))
                          .Returns<string>(s => s);
            mockValidation.Setup(v => v.Validate(It.IsAny<string>()))
                          .Returns(SqlValidationResult.Failure("Only SELECT queries are allowed"));

            var controller = new QueryController(
                mockLlm.Object,
                mockValidation.Object,
                dbContext: null!,
                mockUserContext.Object,
                mockLogger.Object);

            var request = new NaturalLanguageQueryDto { Question = "Drop everything" };

            // Act
            var result = await controller.Ask(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<QueryResponseDto>>(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = Assert.IsType<QueryResponseDto>(badRequest.Value);
            Assert.False(response.Success);
        }

        // ---------- Ask endpoint — LLM throws returns 400 ----------

        [Fact]
        public async Task Ask_WhenLlmThrowsInvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var mockLlm = new Mock<IQueryLLMService>();
            var mockValidation = new Mock<ISqlValidationService>();
            var mockUserContext = new Mock<IUserContext>();
            var mockLogger = new Mock<ILogger<QueryController>>();

            var userId = Guid.NewGuid();
            mockUserContext.Setup(c => c.GetActingUserId()).Returns(userId);
            mockLlm.Setup(l => l.GenerateSqlFromNaturalLanguageAsync(It.IsAny<string>()))
                   .ThrowsAsync(new InvalidOperationException("LLM unavailable"));

            var controller = new QueryController(
                mockLlm.Object,
                mockValidation.Object,
                dbContext: null!,
                mockUserContext.Object,
                mockLogger.Object);

            var request = new NaturalLanguageQueryDto { Question = "What is my collection size?" };

            // Act
            var result = await controller.Ask(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<QueryResponseDto>>(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = Assert.IsType<QueryResponseDto>(badRequest.Value);
            Assert.False(response.Success);
        }
    }
}
