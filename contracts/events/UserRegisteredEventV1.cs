using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Domain event payload emitted when a new user registers in User Service.
/// Topic: user.registered.v1
/// Partition Key: UserId
/// </summary>
public class UserRegisteredEventV1
{
    public const string EventTypeName = "user.registered.v1";
    public const string CurrentVersion = "1.0";
    public const string DefaultTopic = "user.registered.v1";

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("verificationCode")]
    public string VerificationCode { get; set; } = string.Empty;

    [JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public UserRegisteredEventV1()
    {
    }

    public UserRegisteredEventV1(Guid userId, string email, string fullName, string verificationCode)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
        VerificationCode = verificationCode;
        RegisteredAt = DateTime.UtcNow;
    }
}
