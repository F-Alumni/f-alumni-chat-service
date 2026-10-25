namespace ChatService.Services.DTOs.Responses;

/// <summary>
/// Guideline CH-04: gRPC Participant Profile Lookup response.
/// Matches smoke-test.ps1 and examples/grpc-demo contract.
/// </summary>
public record ConversationParticipantResponse(
    Guid ConversationId,
    Guid UserId,
    string DisplayName,
    string AvatarUrl,
    string Status,
    string ResolvedVia,
    DateTime RetrievedAtUtc
);

public class ParticipantDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool IsMuted { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? LastReadAt { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class ConversationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsGroup { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public ChatMessageResponse? LastMessage { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
}

public class ChatMessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string Status { get; set; } = "Sent";
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; }
}

public class MarkReadResponse
{
    public Guid ConversationId { get; set; }
    public Guid ReaderUserId { get; set; }
    public int MarkedCount { get; set; }
    public DateTime ReadAtUtc { get; set; }
}
