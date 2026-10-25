using ChatService.Repositories.Context;
using ChatService.Repositories.Entities;
using ChatService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories.Implementations;

public class ChatRepository : IChatRepository
{
    private readonly ChatServiceDbContext _dbContext;

    public ChatRepository(ChatServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .Where(c => !c.IsGroup &&
                        c.Participants.Any(p => p.UserId == userAId) &&
                        c.Participants.Any(p => p.UserId == userBId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
        Guid userId,
        bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(p => p.UserId == userId && (includeHidden || !p.IsHidden)));

        return await query
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);
    }

    public async Task UpdateParticipantAsync(ConversationParticipant participant, CancellationToken cancellationToken = default)
    {
        _dbContext.ConversationParticipants.Update(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);

        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken);
        if (conversation is not null)
        {
            conversation.LastMessageAtUtc = message.CreatedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesPagedAsync(
        Guid conversationId,
        string? searchKeyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var term = searchKeyword.Trim().ToLowerInvariant();
            query = query.Where(m => m.Content.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> MarkMessagesAsReadAsync(Guid conversationId, Guid readerUserId, CancellationToken cancellationToken = default)
    {
        var unreadMessages = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != readerUserId && m.Status != "Read")
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.Status = "Read";
            msg.ReadAtUtc = now;
        }

        var participant = await _dbContext.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == readerUserId, cancellationToken);

        if (participant is not null)
        {
            participant.LastReadAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unreadMessages.Count;
    }
}
