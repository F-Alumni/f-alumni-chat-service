using Microsoft.EntityFrameworkCore;
using ChatService.Repositories.Context;
using ChatService.Repositories.Entities;
using ChatService.Repositories.Interfaces;

namespace ChatService.Repositories.Implementations;

/// <summary>
/// EF Core repository implementation querying and persisting SampleItem entities.
/// </summary>
public class SampleItemRepository : ISampleItemRepository
{
    private readonly ChatServiceDbContext _dbContext;

    public SampleItemRepository(ChatServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SampleItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SampleItems
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<SampleItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SampleItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || x.Description.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SampleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SampleItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<SampleItem> CreateAsync(SampleItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.SampleItems.AddAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<SampleItem?> UpdateAsync(SampleItem item, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SampleItems.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.IsActive = item.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SampleItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.SampleItems.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
