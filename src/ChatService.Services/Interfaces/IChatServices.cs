using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;

namespace ChatService.Services.Interfaces;

public interface IUserProfileLookupService
{
    /// <summary>
    /// Calls User Service via gRPC (HTTP/2 Protobuf) to retrieve user summary profile.
    /// Throws RpcException if strictGrpc=true and the upstream gRPC call fails or returns NotFound.
    /// </summary>
    Task<ConversationParticipantResponse> GetParticipantSummaryAsync(
        Guid conversationId,
        Guid userId,
        bool strictGrpc = true,
        CancellationToken cancellationToken = default);
}

public interface IChatKafkaPublisher
{
    /// <summary>
    /// CH-10: Publishes chat.message_sent.v1 event to Kafka for offline push notifications.
    /// </summary>
    Task PublishMessageSentAsync(ChatMessageResponse message, IEnumerable<Guid> recipientUserIds, CancellationToken cancellationToken = default);
}

public interface IChatAppService
{
    Task<ConversationResponse> CreateOrGetConversationAsync(Guid creatorUserId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    Task<ConversationResponse?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<PagedResponse<ConversationResponse>> GetUserConversationsAsync(Guid userId, bool includeHidden = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ChatMessageResponse> SendMessageAsync(Guid conversationId, Guid senderUserId, SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, string? search = null, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default);
    Task<MarkReadResponse> MarkConversationReadAsync(Guid conversationId, Guid readerUserId, CancellationToken cancellationToken = default);
    Task<ParticipantDto?> UpdateConversationSettingsAsync(Guid conversationId, Guid userId, UpdateConversationSettingsRequest request, CancellationToken cancellationToken = default);
}
