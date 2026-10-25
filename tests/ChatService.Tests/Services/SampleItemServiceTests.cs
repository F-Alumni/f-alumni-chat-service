using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ChatService.Repositories.Context;
using ChatService.Repositories.Entities;
using ChatService.Repositories.Implementations;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.Exceptions;
using ChatService.Services.Implementations;
using Xunit;

namespace ChatService.Tests.Services;

public class SampleItemServiceTests : IDisposable
{
    private readonly ChatServiceDbContext _dbContext;
    private readonly SampleItemService _service;
    private readonly Guid _seedId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SampleItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChatServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ChatServiceDbContext(options);

        // Seed initial test data
        _dbContext.SampleItems.Add(new SampleItem
        {
            Id = _seedId,
            Name = "Initial Sample Item",
            Description = "Seeded item for unit testing",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var repository = new SampleItemRepository(_dbContext);
        _service = new SampleItemService(repository, NullLogger<SampleItemService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsInitialSeedItems()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, x => x.Name == "Initial Sample Item");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResponseWithMetadata()
    {
        // Act
        var result = await _service.GetPagedAsync(search: "Initial", pageNumber: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Single(result.Items);
        Assert.Equal("Initial Sample Item", result.Items[0].Name);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsSampleItem()
    {
        // Arrange
        var request = new CreateSampleItemRequest
        {
            Name = "New Feature Item",
            Description = "Unit test description"
        };

        // Act
        var created = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("New Feature Item", created.Name);
        Assert.Equal("Unit test description", created.Description);
        Assert.True(created.IsActive);

        // Verify it was persisted in the DbContext
        var retrieved = await _service.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsExpectedItem()
    {
        // Act
        var result = await _service.GetByIdAsync(_seedId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_seedId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(nonExistingId));
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsItem()
    {
        // Arrange
        var updateRequest = new UpdateSampleItemRequest
        {
            Name = "Updated Item Name",
            Description = "Updated Description",
            IsActive = false
        };

        // Act
        var updated = await _service.UpdateAsync(_seedId, updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(_seedId, updated.Id);
        Assert.Equal("Updated Item Name", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
        Assert.False(updated.IsActive);

        // Verify persistence
        var retrieved = await _service.GetByIdAsync(_seedId);
        Assert.Equal("Updated Item Name", retrieved.Name);
        Assert.False(retrieved.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateSampleItemRequest
        {
            Name = "NonExistent Item",
            Description = "Should fail",
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(nonExistingId, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_seedId, null!));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesItem()
    {
        // Arrange
        var createRequest = new CreateSampleItemRequest
        {
            Name = "Item To Delete",
            Description = "Will be deleted"
        };
        var created = await _service.CreateAsync(createRequest);

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(nonExistingId));
    }
}
