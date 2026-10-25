using System.ComponentModel.DataAnnotations;

namespace ChatService.Services.DTOs.Requests;

/// <summary>
/// Request payload to update an existing SampleItem resource.
/// </summary>
public class UpdateSampleItemRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
