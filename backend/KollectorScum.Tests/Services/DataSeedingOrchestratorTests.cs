using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Services;

public class DataSeedingOrchestratorTests
{
    private readonly Mock<ILookupSeeder<Country, CountryJsonDto>> _mockCountrySeeder;
    private readonly Mock<ILookupSeeder<Store, StoreJsonDto>> _mockStoreSeeder;
    private readonly Mock<ILookupSeeder<Format, FormatJsonDto>> _mockFormatSeeder;
    private readonly Mock<ILookupSeeder<Genre, GenreJsonDto>> _mockGenreSeeder;
    private readonly Mock<ILookupSeeder<Label, LabelJsonDto>> _mockLabelSeeder;
    private readonly Mock<ILookupSeeder<Artist, ArtistJsonDto>> _mockArtistSeeder;
    private readonly Mock<ILookupSeeder<Packaging, PackagingJsonDto>> _mockPackagingSeeder;
    private readonly Mock<ILogger<DataSeedingOrchestrator>> _mockLogger;

    public DataSeedingOrchestratorTests()
    {
        _mockCountrySeeder = new Mock<ILookupSeeder<Country, CountryJsonDto>>();
        _mockStoreSeeder = new Mock<ILookupSeeder<Store, StoreJsonDto>>();
        _mockFormatSeeder = new Mock<ILookupSeeder<Format, FormatJsonDto>>();
        _mockGenreSeeder = new Mock<ILookupSeeder<Genre, GenreJsonDto>>();
        _mockLabelSeeder = new Mock<ILookupSeeder<Label, LabelJsonDto>>();
        _mockArtistSeeder = new Mock<ILookupSeeder<Artist, ArtistJsonDto>>();
        _mockPackagingSeeder = new Mock<ILookupSeeder<Packaging, PackagingJsonDto>>();
        _mockLogger = new Mock<ILogger<DataSeedingOrchestrator>>();

        // Setup table names for logging verification
        _mockCountrySeeder.Setup(s => s.TableName).Returns("Countries");
        _mockStoreSeeder.Setup(s => s.TableName).Returns("Stores");
        _mockFormatSeeder.Setup(s => s.TableName).Returns("Formats");
        _mockGenreSeeder.Setup(s => s.TableName).Returns("Genres");
        _mockLabelSeeder.Setup(s => s.TableName).Returns("Labels");
        _mockArtistSeeder.Setup(s => s.TableName).Returns("Artists");
        _mockPackagingSeeder.Setup(s => s.TableName).Returns("Packagings");
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_CallsAllSeeders()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        SetupAllSeeders(10, 5, 8, 12, 15, 20, 6);

        // Act
        await orchestrator.SeedAllLookupDataAsync();

        // Assert
        _mockCountrySeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockStoreSeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockFormatSeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockGenreSeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockLabelSeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockArtistSeeder.Verify(s => s.SeedAsync(), Times.Once);
        _mockPackagingSeeder.Verify(s => s.SeedAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_ReturnsTotalCountFromAllSeeders()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        SetupAllSeeders(10, 5, 8, 12, 15, 20, 6);

        // Act
        var result = await orchestrator.SeedAllLookupDataAsync();

        // Assert
        Assert.Equal(76, result); // 10 + 5 + 8 + 12 + 15 + 20 + 6
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_CallsSeedersInCorrectOrder()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var callOrder = new List<string>();

        _mockCountrySeeder.Setup(s => s.SeedAsync()).ReturnsAsync(10).Callback(() => callOrder.Add("Country"));
        _mockStoreSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(5).Callback(() => callOrder.Add("Store"));
        _mockFormatSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(8).Callback(() => callOrder.Add("Format"));
        _mockGenreSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(12).Callback(() => callOrder.Add("Genre"));
        _mockLabelSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(15).Callback(() => callOrder.Add("Label"));
        _mockArtistSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(20).Callback(() => callOrder.Add("Artist"));
        _mockPackagingSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(6).Callback(() => callOrder.Add("Packaging"));

        // Act
        await orchestrator.SeedAllLookupDataAsync();

        // Assert
        Assert.Equal(new[] { "Country", "Store", "Format", "Genre", "Label", "Artist", "Packaging" }, callOrder);
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_WhenAllSeedersReturnZero_ReturnsZero()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        SetupAllSeeders(0, 0, 0, 0, 0, 0, 0);

        // Act
        var result = await orchestrator.SeedAllLookupDataAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_WhenSeederThrowsException_ThrowsAndLogsError()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        _mockCountrySeeder.Setup(s => s.SeedAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await orchestrator.SeedAllLookupDataAsync());

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred during lookup data seeding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_LogsStartAndCompletionMessages()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        SetupAllSeeders(10, 5, 8, 12, 15, 20, 6);

        // Act
        await orchestrator.SeedAllLookupDataAsync();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting lookup data seeding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Lookup data seeding completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAllLookupDataAsync_DoesNotLogIndividualSeederCompletion()
    {
        // Arrange
        // The orchestrator doesn't log individual seeder completion - each seeder logs its own completion
        var orchestrator = CreateOrchestrator();
        SetupAllSeeders(10, 5, 8, 12, 15, 20, 6);

        // Act
        await orchestrator.SeedAllLookupDataAsync();

        // Assert - Verify orchestrator only logs start and completion, not individual seeders
        _mockLogger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2)); // Only start and completion messages
    }

    private DataSeedingOrchestrator CreateOrchestrator()
    {
        return new DataSeedingOrchestrator(
            _mockCountrySeeder.Object,
            _mockStoreSeeder.Object,
            _mockFormatSeeder.Object,
            _mockGenreSeeder.Object,
            _mockLabelSeeder.Object,
            _mockArtistSeeder.Object,
            _mockPackagingSeeder.Object,
            _mockLogger.Object
        );
    }

    private void SetupAllSeeders(int countryCount, int storeCount, int formatCount, int genreCount, int labelCount, int artistCount, int packagingCount)
    {
        _mockCountrySeeder.Setup(s => s.SeedAsync()).ReturnsAsync(countryCount);
        _mockStoreSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(storeCount);
        _mockFormatSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(formatCount);
        _mockGenreSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(genreCount);
        _mockLabelSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(labelCount);
        _mockArtistSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(artistCount);
        _mockPackagingSeeder.Setup(s => s.SeedAsync()).ReturnsAsync(packagingCount);
    }
}
