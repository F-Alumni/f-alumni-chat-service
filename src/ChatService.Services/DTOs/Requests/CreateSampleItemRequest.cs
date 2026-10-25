using System.ComponentModel.DataAnnotations;

namespace ChatService.Services.DTOs.Requests;

/// <summary>
/// Request payload to create a new SampleItem.
/// </summary>
public class CreateSampleItemRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;
}
