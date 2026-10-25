using Microsoft.Extensions.Logging;
using ChatService.Repositories.Entities;
using ChatService.Repositories.Interfaces;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Exceptions;
using ChatService.Services.Interfaces;

namespace ChatService.Services.Implementations;

/// <summary>
/// Business service implementation managing SampleItem domain logic.
/// </summary>
public class SampleItemService : ISampleItemService
{
    private readonly ISampleItemRepository _repository;
    private readonly ILogger<SampleItemService> _logger;

    public SampleItemService(
        ISampleItemRepository repository,
        ILogger<SampleItemService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SampleItemResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all sample items.");
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<PagedResponse<SampleItemResponse>> GetPagedAsync(
        string? search,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("Retrieving paged sample items. Search: '{Search}', Page: {Page}, Size: {Size}", search, pageNumber, pageSize);
        var (items, totalCount) = await _repository.GetPagedAsync(search, pageNumber, pageSize, cancellationToken);
        var responseItems = items.Select(MapToResponse).ToList();

        return new PagedResponse<SampleItemResponse>(responseItems, totalCount, pageNumber, pageSize);
    }

    public async Task<SampleItemResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving sample item by ID: {ItemId}", id);
        var item = await _repository.GetByIdAsync(id, cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Sample item {ItemId} not found.", id);
            throw new NotFoundException(nameof(SampleItem), id);
        }

        return MapToResponse(item);
    }

    public async Task<SampleItemResponse> CreateAsync(CreateSampleItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Creating sample item with name: {Name}", request.Name);

        var entity = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Sample item created successfully with ID: {ItemId}", created.Id);

        return MapToResponse(created);
    }

    public async Task<SampleItemResponse> UpdateAsync(Guid id, UpdateSampleItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Updating sample item with ID: {ItemId}", id);

        var entityToUpdate = new SampleItem
        {
            Id = id,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = request.IsActive
        };

        var updated = await _repository.UpdateAsync(entityToUpdate, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot update: Sample item {ItemId} not found.", id);
            throw new NotFoundException(nameof(SampleItem), id);
        }

        _logger.LogInformation("Sample item {ItemId} updated successfully.", id);
        return MapToResponse(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting sample item with ID: {ItemId}", id);
        var deleted = await _repository.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            _logger.LogWarning("Cannot delete: Sample item {ItemId} not found.", id);
            throw new NotFoundException(nameof(SampleItem), id);
        }

        _logger.LogInformation("Sample item {ItemId} deleted successfully.", id);
    }

    private static SampleItemResponse MapToResponse(SampleItem entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc
    };
}
