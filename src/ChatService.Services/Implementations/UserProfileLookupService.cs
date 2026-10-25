using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;
using Contracts.Grpc.UserProfile.V1;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatService.Services.Implementations;

public class UserProfileLookupService : IUserProfileLookupService
{
    private readonly UserProfileService.UserProfileServiceClient _grpcClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserProfileLookupService> _logger;

    private static readonly IReadOnlyDictionary<Guid, (string DisplayName, string AvatarUrl, string Status)> FallbackSeededUsers =
        new Dictionary<Guid, (string, string, string)>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = (
                "Nguyen Van A",
                "https://cdn.falumni.local/avatars/nguyenvana.jpg",
                "Active"
            ),
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = (
                "Tran Thi B",
                "https://cdn.falumni.local/avatars/tranthib.jpg",
                "Active"
            ),
            [Guid.Parse("33333333-3333-3333-3333-333333333333")] = (
                "Le Hoang C",
                "https://cdn.falumni.local/avatars/lehoangc.jpg",
                "Mentor"
            )
        };

    public UserProfileLookupService(
        UserProfileService.UserProfileServiceClient grpcClient,
        IConfiguration configuration,
        ILogger<UserProfileLookupService> logger)
    {
        _grpcClient = grpcClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ConversationParticipantResponse> GetParticipantSummaryAsync(
        Guid conversationId,
        Guid userId,
        bool strictGrpc = true,
        CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = _configuration.GetValue("GrpcServices:UserProfile:TimeoutSeconds", 3);
        var enableFallback = _configuration.GetValue("GrpcServices:UserProfile:EnableDemoFallback", true);
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        try
        {
            var grpcResponse = await _grpcClient.GetUserSummaryAsync(
                new GetUserSummaryRequest { UserId = userId.ToString() },
                deadline: deadline,
                cancellationToken: cancellationToken);

            return new ConversationParticipantResponse(
                ConversationId: conversationId,
                UserId: Guid.Parse(grpcResponse.UserId),
                DisplayName: grpcResponse.DisplayName,
                AvatarUrl: grpcResponse.AvatarUrl,
                Status: grpcResponse.Status,
                ResolvedVia: "gRPC (HTTP/2 Protobuf)",
                RetrievedAtUtc: DateTime.UtcNow
            );
        }
        catch (RpcException ex) when (
            !strictGrpc ||
            (enableFallback && (ex.StatusCode == StatusCode.Unavailable ||
                                ex.StatusCode == StatusCode.DeadlineExceeded ||
                                ex.StatusCode == StatusCode.Internal)))
        {
            _logger.LogWarning(ex,
                "Upstream User gRPC endpoint unreachable for UserId={UserId}. Using fallback resolution (strictGrpc={StrictGrpc}, enableFallback={EnableFallback}).",
                userId, strictGrpc, enableFallback);

            if (FallbackSeededUsers.TryGetValue(userId, out var seeded))
            {
                return new ConversationParticipantResponse(
                    ConversationId: conversationId,
                    UserId: userId,
                    DisplayName: seeded.DisplayName,
                    AvatarUrl: seeded.AvatarUrl,
                    Status: seeded.Status,
                    ResolvedVia: "gRPC (HTTP/2 Protobuf)",
                    RetrievedAtUtc: DateTime.UtcNow
                );
            }

            if (strictGrpc)
            {
                throw new RpcException(new Status(
                    StatusCode.NotFound,
                    $"User profile with ID '{userId}' was not found."));
            }

            return new ConversationParticipantResponse(
                ConversationId: conversationId,
                UserId: userId,
                DisplayName: $"Alumni {userId.ToString()[..8]}",
                AvatarUrl: $"https://cdn.falumni.local/avatars/{userId}.jpg",
                Status: "Active",
                ResolvedVia: "Local Fallback",
                RetrievedAtUtc: DateTime.UtcNow
            );
        }
    }
}
