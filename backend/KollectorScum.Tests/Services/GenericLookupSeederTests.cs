using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Services;

public class GenericLookupSeederTests
{
    private readonly Mock<IJsonFileReader> _mockFileReader;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRepository<Country>> _mockCountryRepository;
    private readonly Mock<ILogger<CountrySeeder>> _mockLogger;
    private readonly string _testDataPath = "/test/data";

    public GenericLookupSeederTests()
    {
        _mockFileReader = new Mock<IJsonFileReader>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCountryRepository = new Mock<IRepository<Country>>();
        _mockLogger = new Mock<ILogger<CountrySeeder>>();

        // Setup repository in unit of work
        _mockUnitOfWork.Setup(u => u.Countries).Returns(_mockCountryRepository.Object);
    }

    [Fact]
    public async Task SeedAsync_WithValidDataAndNoExistingRecords_SeedsSuccessfully()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        var container = new CountriesJsonContainer { Countrys = new List<CountryJsonDto> 
        { 
            new() { Name = "United States" },
            new() { Name = "United Kingdom" }
        }};

        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync(container);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);
        _mockCountryRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(2);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(2, result);
        _mockCountryRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Country>>(c => c.Count() == 2)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenFileDoesNotExist_ReturnsZero()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(0, result);
        _mockFileReader.Verify(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()), Times.Never);
        _mockCountryRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenDataAlreadyExists_SkipsSeeding()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(0, result);
        _mockFileReader.Verify(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()), Times.Never);
        _mockCountryRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenContainerIsNull_ReturnsZero()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync((CountriesJsonContainer?)null);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(0, result);
        _mockCountryRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenContainerHasNoDtos_ReturnsZero()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        var container = new CountriesJsonContainer { Countrys = new List<CountryJsonDto>() };

        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync(container);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(0, result);
        _mockCountryRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_MapsCountryDtoToEntityCorrectly()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        var container = new CountriesJsonContainer { Countrys = new List<CountryJsonDto> 
        { 
            new() { Name = "Canada" }
        }};

        Country? capturedCountry = null;
        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync(container);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);
        _mockCountryRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()))
            .Callback<IEnumerable<Country>>(countries => capturedCountry = countries.First())
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await seeder.SeedAsync();

        // Assert
        Assert.NotNull(capturedCountry);
        Assert.Equal("Canada", capturedCountry.Name);
    }

    [Fact]
    public async Task SeedAsync_WhenFileReadThrowsException_ThrowsAndLogsError()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ThrowsAsync(new IOException("File read error"));
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(async () => await seeder.SeedAsync());

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error seeding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenSaveThrowsException_ThrowsAndLogsError()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        var container = new CountriesJsonContainer { Countrys = new List<CountryJsonDto> 
        { 
            new() { Name = "France" }
        }};

        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync(container);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);
        _mockCountryRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await seeder.SeedAsync());

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error seeding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_LogsCorrectCountWhenSuccessful()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);
        var container = new CountriesJsonContainer { Countrys = new List<CountryJsonDto> 
        { 
            new() { Name = "Germany" },
            new() { Name = "Japan" },
            new() { Name = "Australia" }
        }};

        _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileReader.Setup(f => f.ReadJsonFileAsync<CountriesJsonContainer>(It.IsAny<string>()))
            .ReturnsAsync(container);
        _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
            .ReturnsAsync(false);
        _mockCountryRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Country>>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(3);

        // Act
        var result = await seeder.SeedAsync();

        // Assert
        Assert.Equal(3, result);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Seeded 3 Countries")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TableName_ReturnsCorrectValue()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);

        // Act
        var tableName = seeder.TableName;

        // Assert
        Assert.Equal("Countries", tableName);
    }

    [Fact]
    public void FileName_ReturnsCorrectValue()
    {
        // Arrange
        var seeder = new CountrySeeder(_mockFileReader.Object, _mockUnitOfWork.Object, _mockLogger.Object, _testDataPath);

        // Act
        var fileName = seeder.FileName;

        // Assert
        Assert.Equal("countrys.json", fileName);
    }
}
