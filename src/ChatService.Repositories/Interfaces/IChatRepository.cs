using ChatService.Repositories.Entities;

namespace ChatService.Repositories.Interfaces;

public interface IChatRepository
{
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId, bool includeHidden = false, CancellationToken cancellationToken = default);
    Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task UpdateParticipantAsync(ConversationParticipant participant, CancellationToken cancellationToken = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesPagedAsync(
        Guid conversationId,
        string? searchKeyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> MarkMessagesAsReadAsync(Guid conversationId, Guid readerUserId, CancellationToken cancellationToken = default);
}
