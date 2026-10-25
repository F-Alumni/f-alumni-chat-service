using System.Text.Json;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatService.Services.Implementations;

public record ChatMessageSentEventPayload(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string ContentPreview,
    bool HasAttachment,
    IReadOnlyList<Guid> RecipientUserIds,
    DateTime SentAtUtc
);

public class ChatKafkaPublisher : IChatKafkaPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatKafkaPublisher> _logger;
    private const string MessageSentTopic = "chat.message_sent.v1";

    public ChatKafkaPublisher(IConfiguration configuration, ILogger<ChatKafkaPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishMessageSentAsync(
        ChatMessageResponse message,
        IEnumerable<Guid> recipientUserIds,
        CancellationToken cancellationToken = default)
    {
        var payload = new ChatMessageSentEventPayload(
            MessageId: message.Id,
            ConversationId: message.ConversationId,
            SenderUserId: message.SenderUserId,
            SenderDisplayName: message.SenderDisplayName,
            ContentPreview: message.Content.Length > 100 ? message.Content[..100] + "..." : message.Content,
            HasAttachment: !string.IsNullOrWhiteSpace(message.AttachmentUrl),
            RecipientUserIds: recipientUserIds.ToList(),
            SentAtUtc: message.SentAt
        );

        var envelope = new EventEnvelope<ChatMessageSentEventPayload>(
            eventType: MessageSentTopic,
            version: "1.0",
            producer: "f-alumni-chat-service",
            data: payload,
            correlationId: message.ConversationId.ToString()
        );

        var json = JsonSerializer.Serialize(envelope);
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogInformation(
                "[Kafka Simulated] Published {EventType} for ConversationId={ConversationId}, MessageId={MessageId}: {Payload}",
                MessageSentTopic, message.ConversationId, message.Id, json);
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                MessageTimeoutMs = 3000
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
            await producer.ProduceAsync(
                MessageSentTopic,
                new Message<string, string>
                {
                    Key = message.ConversationId.ToString(),
                    Value = json
                },
                cancellationToken);

            _logger.LogInformation("Published Kafka event {EventType} for MessageId={MessageId}", MessageSentTopic, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka broker unreachable for {EventType}; continuing without failing chat request.", MessageSentTopic);
        }
    }
}
