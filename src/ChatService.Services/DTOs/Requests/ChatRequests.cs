using System.ComponentModel.DataAnnotations;

namespace ChatService.Services.DTOs.Requests;

/// <summary>
/// CH-01: Start a 1-1 or Group conversation with another alumni.
/// </summary>
public class CreateConversationRequest
{
    public string? Title { get; set; }
    public bool IsGroup { get; set; }

    /// <summary>
    /// Optional single recipient for 1-1 chat convenience.
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// List of participant User IDs to include in the conversation.
    /// </summary>
    public List<Guid> ParticipantUserIds { get; set; } = new();
}

/// <summary>
/// CH-02 & CH-07: Send a text or image/attachment message in a conversation.
/// </summary>
public class SendMessageRequest
{
    [MaxLength(4000)]
    public string? Content { get; set; }

    /// <summary>
    /// Optional image or file attachment URL (CH-07).
    /// </summary>
    [MaxLength(1024)]
    public string? AttachmentUrl { get; set; }

    /// <summary>
    /// Optional attachment type: "Image", "File", etc. (CH-07).
    /// </summary>
    [MaxLength(64)]
    public string? AttachmentType { get; set; }

    /// <summary>
    /// Optional sender UserId override when testing without JWT or via internal call.
    /// </summary>
    public Guid? SenderUserId { get; set; }
}

/// <summary>
/// CH-09: Mute notifications or hide conversation from active chat list.
/// </summary>
public class UpdateConversationSettingsRequest
{
    public bool? IsMuted { get; set; }
    public bool? IsHidden { get; set; }
}
