namespace ChatService.Repositories.Entities;

/// <summary>
/// Sample entity representing a resource owned by this service.
/// </summary>
public class SampleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
