namespace ChatService.Services.DTOs.Responses;

/// <summary>
/// Response DTO returning SampleItem data to the client.
/// </summary>
public class SampleItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
