using ChatService.Repositories.Configurations;
using ChatService.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories.Context;

/// <summary>
/// Service-specific DbContext for ChatService (FAlumni_Chat_Db).
/// Strictly isolated from User, Forum, AI, and Admin databases.
/// </summary>
public class ChatServiceDbContext : DbContext
{
    public ChatServiceDbContext(DbContextOptions<ChatServiceDbContext> options) : base(options)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
    }
}
