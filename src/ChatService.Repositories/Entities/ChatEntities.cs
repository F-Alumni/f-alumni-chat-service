namespace ChatService.Repositories.Entities;

/// <summary>
/// Represents a 1-1 or Group conversation owned exclusively by ChatService (FAlumni_Chat_Db).
/// Strictly stores scalar UserIds (NO Cross-Database Foreign Keys).
/// </summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public bool IsGroup { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ConversationParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    /// <summary>
    /// Scalar User ID referencing User Service identity (Zero cross-DB FK).
    /// </summary>
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// CH-09 (Sheet): Allow user to mute notifications for this conversation.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// CH-09 (Sheet): Allow user to hide/archive this conversation.
    /// </summary>
    public bool IsHidden { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastReadAtUtc { get; set; }

    public Conversation? Conversation { get; set; }
}

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    /// <summary>
    /// Scalar Sender User ID (Zero cross-DB FK).
    /// </summary>
    public Guid SenderId { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// CH-07: Optional image or file attachment URL in message.
    /// </summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>
    /// CH-07: Attachment type (e.g. image/png, image/jpeg, application/pdf).
    /// </summary>
    public string? AttachmentType { get; set; }

    /// <summary>
    /// CH-05 (Sheet) / CH-08 (Guideline): Delivery status: Sent, Delivered, Read.
    /// </summary>
    public string Status { get; set; } = "Sent";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }

    public Conversation? Conversation { get; set; }
}
