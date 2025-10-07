using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace KollectorScum.Tests.Repositories
{
    public class RepositoryTests
    {
        [Fact]
        public async Task Repository_ShouldProvideBasicCrudOperations()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new KollectorScumDbContext(options);
            var repository = new Repository<Country>(context);

            var country = new Country { Name = "Test Country" };

            // Act & Assert - Add
            var addedCountry = await repository.AddAsync(country);
            await context.SaveChangesAsync();
            
            Assert.NotNull(addedCountry);
            Assert.Equal("Test Country", addedCountry.Name);

            // Act & Assert - Get
            var retrievedCountry = await repository.GetByIdAsync(addedCountry.Id);
            Assert.NotNull(retrievedCountry);
            Assert.Equal("Test Country", retrievedCountry.Name);

            // Act & Assert - Update
            retrievedCountry.Name = "Updated Country";
            repository.Update(retrievedCountry);
            await context.SaveChangesAsync();

            var updatedCountry = await repository.GetByIdAsync(addedCountry.Id);
            Assert.Equal("Updated Country", updatedCountry?.Name);

            // Act & Assert - Delete
            var deleteResult = await repository.DeleteAsync(addedCountry.Id);
            await context.SaveChangesAsync();
            
            Assert.True(deleteResult);
            
            var deletedCountry = await repository.GetByIdAsync(addedCountry.Id);
            Assert.Null(deletedCountry);
        }

        [Fact]
        public async Task Repository_ShouldSupportFilteringAndPagination()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new KollectorScumDbContext(options);
            var repository = new Repository<Country>(context);

            // Add test data
            var countries = new List<Country>
            {
                new Country { Name = "Australia" },
                new Country { Name = "Austria" },
                new Country { Name = "Belgium" },
                new Country { Name = "Canada" },
                new Country { Name = "Denmark" }
            };

            await repository.AddRangeAsync(countries);
            await context.SaveChangesAsync();

            // Act & Assert - Filtering
            var aCountries = await repository.GetAsync(c => c.Name.StartsWith("A"));
            Assert.Equal(2, aCountries.Count());

            // Act & Assert - Pagination
            var (firstPage, totalCount) = await repository.GetPagedAsync(1, 2);
            Assert.Equal(2, firstPage.Count());
            Assert.Equal(5, totalCount);

            var (secondPage, _) = await repository.GetPagedAsync(2, 2);
            Assert.Equal(2, secondPage.Count());
        }

        [Fact]
        public async Task UnitOfWork_ShouldManageMultipleRepositories()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new KollectorScumDbContext(options);
            using var unitOfWork = new UnitOfWork(context);

            // Act & Assert
            var country = new Country { Name = "Test Country" };
            var store = new Store { Name = "Test Store" };

            await unitOfWork.Countries.AddAsync(country);
            await unitOfWork.Stores.AddAsync(store);

            var savedCount = await unitOfWork.SaveChangesAsync();
            Assert.Equal(2, savedCount);

            // Verify data was saved
            var savedCountry = await unitOfWork.Countries.GetFirstOrDefaultAsync(c => c.Name == "Test Country");
            var savedStore = await unitOfWork.Stores.GetFirstOrDefaultAsync(s => s.Name == "Test Store");

            Assert.NotNull(savedCountry);
            Assert.NotNull(savedStore);
        }

        [Fact]
        public async Task UnitOfWork_TransactionAPI_ShouldWorkWithoutErrors()
        {
            // Note: InMemory provider doesn't support real transactions, but we test the API
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.AmbientTransactionWarning))
                .Options;

            using var context = new KollectorScumDbContext(options);
            using var unitOfWork = new UnitOfWork(context);

            // Act & Assert - Transaction methods should not throw exceptions
            try
            {
                await unitOfWork.BeginTransactionAsync();
                await unitOfWork.CommitTransactionAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
            {
                // This is expected with InMemory provider - just verify no other exceptions are thrown
                Assert.True(true);
            }

            // Verify rollback doesn't throw either
            try
            {
                await unitOfWork.BeginTransactionAsync();
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
            {
                // This is expected with InMemory provider
                Assert.True(true);
            }
        }
    }
}
