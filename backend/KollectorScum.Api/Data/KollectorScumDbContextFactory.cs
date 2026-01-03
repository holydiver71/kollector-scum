using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KollectorScum.Api.Data;

/// <summary>
/// Design-time factory for creating <see cref="KollectorScumDbContext"/> instances.
/// This is used by EF Core tooling (e.g., dotnet-ef) to apply migrations without
/// requiring the full web host to start.
/// </summary>
public sealed class KollectorScumDbContextFactory : IDesignTimeDbContextFactory<KollectorScumDbContext>
{
    /// <summary>
    /// Creates a <see cref="KollectorScumDbContext"/> for EF Core design-time operations.
    /// </summary>
    /// <param name="args">Tooling arguments (unused).</param>
    /// <returns>A configured <see cref="KollectorScumDbContext"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is missing.</exception>
    public KollectorScumDbContext CreateDbContext(string[] args)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing connection string 'DefaultConnection'. Set ConnectionStrings__DefaultConnection (recommended for CI/CD) " +
                "or configure ConnectionStrings:DefaultConnection in appsettings.json.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<KollectorScumDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new KollectorScumDbContext(optionsBuilder.Options);
    }
}
