using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ChatService.Repositories.Entities;
using ChatService.Repositories.Implementations;
using ChatService.Tests.Infrastructure;
using Xunit;

namespace ChatService.Tests.Database;

/// <summary>
/// Integration tests verifying EF Core migrations, data persistence, and CRUD operations
/// against a real Microsoft SQL Server relational database instance.
/// 
/// Strictly isolated to 'FptAlumni_Guideline_Test_Db'. Never runs against domain databases.
/// </summary>
[Trait("Category", "SqlServerIntegration")]
public class SampleItemSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly ConcurrentBag<Guid> _createdIds = new();

    public async Task InitializeAsync()
    {
        // Only run initialization if SQL Server is configured and available
        var (isEligible, _) = SqlServerTestEnvironment.EvaluateEligibility();
        if (isEligible)
        {
            await SqlServerTestEnvironment.EnsureDatabaseMigratedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        // Safe isolation cleanup: only delete records created by this test run
        if (_createdIds.IsEmpty) return;

        try
        {
            await using var dbContext = SqlServerTestEnvironment.CreateDbContext();
            var idsToDelete = _createdIds.Distinct().ToList();
            var itemsToDelete = await dbContext.SampleItems
                .Where(x => idsToDelete.Contains(x.Id))
                .ToListAsync();

            if (itemsToDelete.Count > 0)
            {
                dbContext.SampleItems.RemoveRange(itemsToDelete);
                await dbContext.SaveChangesAsync();
            }
        }
        catch
        {
            // Suppress cleanup exceptions during teardown
        }
    }

    [SqlServerFact]
    public async Task Insert_SampleItem_PersistsToRealSqlServer()
    {
        // Arrange
        await using var dbContext = SqlServerTestEnvironment.CreateDbContext();
        var repository = new SampleItemRepository(dbContext);

        var newItem = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = $"SqlTest_Insert_{Guid.NewGuid():N}"[..30],
            Description = "Integration test verifying insert on real SQL Server",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _createdIds.Add(newItem.Id);

        // Act
        var created = await repository.CreateAsync(newItem);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(newItem.Id, created.Id);

        // Direct verification on DbContext
        var existsInDb = await dbContext.SampleItems
            .AsNoTracking()
            .AnyAsync(x => x.Id == newItem.Id);
        Assert.True(existsInDb, "Record should be physically persisted to SQL Server database.");
    }

    [SqlServerFact]
    public async Task Read_SampleItem_WithSeparateDbContextInstance_ReturnsPersistedData()
    {
        // Arrange: Create record using instance 1
        var itemId = Guid.NewGuid();
        _createdIds.Add(itemId);

        var expectedName = $"SqlTest_Read_{Guid.NewGuid():N}"[..30];
        var expectedDesc = "Verifying read across separate DbContext instances";

        await using (var writeContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var writeRepo = new SampleItemRepository(writeContext);
            await writeRepo.CreateAsync(new SampleItem
            {
                Id = itemId,
                Name = expectedName,
                Description = expectedDesc,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Act: Read record using instance 2 (completely separate DbContext instance and connection)
        SampleItem? retrievedItem;
        await using (var readContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var readRepo = new SampleItemRepository(readContext);
            retrievedItem = await readRepo.GetByIdAsync(itemId);
        }

        // Assert
        Assert.NotNull(retrievedItem);
        Assert.Equal(itemId, retrievedItem.Id);
        Assert.Equal(expectedName, retrievedItem.Name);
        Assert.Equal(expectedDesc, retrievedItem.Description);
        Assert.True(retrievedItem.IsActive);
    }

    [SqlServerFact]
    public async Task Update_SampleItem_PersistsChangesToRealSqlServer()
    {
        // Arrange: Create record using instance 1
        var itemId = Guid.NewGuid();
        _createdIds.Add(itemId);

        await using (var seedContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var seedRepo = new SampleItemRepository(seedContext);
            await seedRepo.CreateAsync(new SampleItem
            {
                Id = itemId,
                Name = "Original Name",
                Description = "Original Description",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var updatedName = $"Updated_{Guid.NewGuid():N}"[..25];
        var updatedDesc = "Updated Description on SQL Server";

        // Act: Perform update using instance 2
        await using (var updateContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var updateRepo = new SampleItemRepository(updateContext);
            var toUpdate = new SampleItem
            {
                Id = itemId,
                Name = updatedName,
                Description = updatedDesc,
                IsActive = false
            };
            var result = await updateRepo.UpdateAsync(toUpdate);
            Assert.NotNull(result);
        }

        // Assert: Read via instance 3 to verify true relational persistence
        await using (var verifyContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var verifyRepo = new SampleItemRepository(verifyContext);
            var persisted = await verifyRepo.GetByIdAsync(itemId);

            Assert.NotNull(persisted);
            Assert.Equal(updatedName, persisted.Name);
            Assert.Equal(updatedDesc, persisted.Description);
            Assert.False(persisted.IsActive);
        }
    }

    [SqlServerFact]
    public async Task Delete_SampleItem_RemovesFromRealSqlServer()
    {
        // Arrange: Create record using instance 1
        var itemId = Guid.NewGuid();
        _createdIds.Add(itemId);

        await using (var seedContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var seedRepo = new SampleItemRepository(seedContext);
            await seedRepo.CreateAsync(new SampleItem
            {
                Id = itemId,
                Name = "To Be Deleted",
                Description = "Delete Verification",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Act: Delete using instance 2
        bool deleteResult;
        await using (var deleteContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var deleteRepo = new SampleItemRepository(deleteContext);
            deleteResult = await deleteRepo.DeleteAsync(itemId);
        }

        // Assert: Verify removal using instance 3
        Assert.True(deleteResult);

        await using (var verifyContext = SqlServerTestEnvironment.CreateDbContext())
        {
            var verifyRepo = new SampleItemRepository(verifyContext);
            var deletedItem = await verifyRepo.GetByIdAsync(itemId);
            Assert.Null(deletedItem);
        }
    }

    [SqlServerFact]
    public async Task GetByIdAsync_UnknownId_ReturnsNullWithoutException()
    {
        // Arrange
        await using var dbContext = SqlServerTestEnvironment.CreateDbContext();
        var repository = new SampleItemRepository(dbContext);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [SqlServerFact]
    public async Task GetPagedAsync_WithSortingAndPagination_ExecutesCorrectlyOnSqlServer()
    {
        // Arrange: Insert 3 unique items with deterministic timestamps and a search token
        var searchToken = $"PageTest_{Guid.NewGuid():N}"[..18];
        var baseTime = DateTime.UtcNow.AddMinutes(-10);

        var item1 = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = $"{searchToken}_A",
            Description = "First Item",
            CreatedAtUtc = baseTime.AddMinutes(1)
        };
        var item2 = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = $"{searchToken}_B",
            Description = "Second Item",
            CreatedAtUtc = baseTime.AddMinutes(2)
        };
        var item3 = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = $"{searchToken}_C",
            Description = "Third Item",
            CreatedAtUtc = baseTime.AddMinutes(3)
        };

        _createdIds.Add(item1.Id);
        _createdIds.Add(item2.Id);
        _createdIds.Add(item3.Id);

        await using (var seedContext = SqlServerTestEnvironment.CreateDbContext())
        {
            await seedContext.SampleItems.AddRangeAsync(item1, item2, item3);
            await seedContext.SaveChangesAsync();
        }

        // Act: Query page 1 (size 2) ordered by CreatedAtUtc DESC
        await using var queryContext = SqlServerTestEnvironment.CreateDbContext();
        var repository = new SampleItemRepository(queryContext);

        var (page1Items, totalCount) = await repository.GetPagedAsync(
            search: searchToken,
            pageNumber: 1,
            pageSize: 2);

        var (page2Items, totalCount2) = await repository.GetPagedAsync(
            search: searchToken,
            pageNumber: 2,
            pageSize: 2);

        // Assert:
        // TotalCount should be 3
        Assert.Equal(3, totalCount);
        Assert.Equal(3, totalCount2);

        // Page 1 should contain 2 items ordered DESC by CreatedAtUtc (Item C then Item B)
        Assert.Equal(2, page1Items.Count);
        Assert.Equal(item3.Id, page1Items[0].Id);
        Assert.Equal(item2.Id, page1Items[1].Id);

        // Page 2 should contain the remaining 1 item (Item A)
        Assert.Single(page2Items);
        Assert.Equal(item1.Id, page2Items[0].Id);
    }

    [SqlServerFact]
    public async Task RelationalConstraints_NameExceedingMaxLength_ThrowsDbUpdateException()
    {
        // Demonstrates relational schema constraint enforcement:
        // In EF Core InMemory, HasMaxLength(100) is silently ignored.
        // In real SQL Server, nvarchar(100) causes a truncation exception (DbUpdateException).
        await using var dbContext = SqlServerTestEnvironment.CreateDbContext();
        var repository = new SampleItemRepository(dbContext);

        var invalidItem = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = new string('X', 150), // Exceeds nvarchar(100)
            Description = "Constraint violation test",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _createdIds.Add(invalidItem.Id);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await repository.CreateAsync(invalidItem);
        });
    }
}
