using ChatService.Repositories.Context;
using ChatService.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Api.Data;

public static class ChatDatabaseSeeder
{
    public static readonly Guid DemoConversationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid UserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatServiceDbContext>();

        try
        {
            if (db.Database.IsRelational())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
            }

            if (await db.Conversations.AnyAsync(c => c.Id == DemoConversationId))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var conversation = new Conversation
            {
                Id = DemoConversationId,
                Title = "Nguyen Van A, Tran Thi B",
                IsGroup = false,
                CreatedByUserId = UserAId,
                CreatedAtUtc = now.AddMinutes(-30),
                LastMessageAtUtc = now.AddMinutes(-5),
                Participants = new List<ConversationParticipant>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = DemoConversationId,
                        UserId = UserAId,
                        DisplayName = "Nguyen Van A",
                        AvatarUrl = "https://cdn.falumni.local/avatars/nguyenvana.jpg",
                        JoinedAtUtc = now.AddMinutes(-30)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = DemoConversationId,
                        UserId = UserBId,
                        DisplayName = "Tran Thi B",
                        AvatarUrl = "https://cdn.falumni.local/avatars/tranthib.jpg",
                        JoinedAtUtc = now.AddMinutes(-30)
                    }
                },
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = DemoConversationId,
                        SenderId = UserAId,
                        Content = "Chào B! Lâu quá không gặp từ đợt tốt nghiệp FPT khóa K16.",
                        Status = "Read",
                        ReadAtUtc = now.AddMinutes(-10),
                        CreatedAtUtc = now.AddMinutes(-15)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = DemoConversationId,
                        SenderId = UserBId,
                        Content = "Chào A! Hiện tại mình đang làm Product Manager ở VNG, hôm nào cafe nhé!",
                        Status = "Sent",
                        CreatedAtUtc = now.AddMinutes(-5)
                    }
                }
            };

            db.Conversations.Add(conversation);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded demo conversation {ConversationId} in ChatServiceDbContext.", DemoConversationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database seeding skipped or encountered non-fatal issue.");
        }
    }
}
