using System.Security.Claims;
using ChatService.Api.Hubs;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Api.Controllers;

[ApiController]
[Route("api/v1/chats")]
public class ChatsController : ControllerBase
{
    private readonly IChatAppService _chatAppService;
    private readonly IUserProfileLookupService _profileLookupService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatsController> _logger;

    private static readonly Guid DefaultDemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ChatsController(
        IChatAppService chatAppService,
        IUserProfileLookupService profileLookupService,
        IHubContext<ChatHub> hubContext,
        IConfiguration configuration,
        ILogger<ChatsController> logger)
    {
        _chatAppService = chatAppService;
        _profileLookupService = profileLookupService;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Guideline CH-04: Looks up participant summary profile for a conversation by synchronously calling User Service via gRPC.
    /// Used by smoke-test.ps1 and API Gateway (/api/chat/* -> /api/v1/chats/*).
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/participants/{userId:guid}")]
    [ProducesResponseType(typeof(ConversationParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetParticipantProfile(
        [FromRoute] Guid conversationId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds = _configuration.GetValue("GrpcServices:UserProfile:TimeoutSeconds", 3);

        try
        {
            var response = await _profileLookupService.GetParticipantSummaryAsync(
                conversationId,
                userId,
                strictGrpc: true,
                cancellationToken);

            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.LogWarning("Participant profile not found via gRPC: {Detail}", ex.Status.Detail);
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Participant Profile Not Found",
                detail: ex.Status.Detail,
                type: "https://httpstatuses.com/404"
            );
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            _logger.LogError(ex, "User gRPC Service is unavailable at target endpoint.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "User Profile Service Unavailable",
                detail: "The upstream User Service gRPC endpoint is unreachable or currently unavailable.",
                type: "https://httpstatuses.com/503"
            );
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
        {
            _logger.LogError("gRPC call to User Service exceeded deadline of {Timeout}s.", timeoutSeconds);
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "User Profile Service Timeout",
                detail: $"The upstream User Service failed to respond within the {timeoutSeconds}s deadline.",
                type: "https://httpstatuses.com/504"
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Unexpected gRPC error: StatusCode={StatusCode}, Detail={Detail}",
                ex.StatusCode, ex.Status.Detail);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Upstream Communication Error",
                detail: ex.Status.Detail,
                type: "https://httpstatuses.com/500"
            );
        }
    }

    /// <summary>
    /// CH-01: Start a 1-1 or Group conversation with another alumni.
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = userId ?? ResolveCurrentUserId();
            var conversation = await _chatAppService.CreateOrGetConversationAsync(currentUserId, request, cancellationToken);
            return CreatedAtAction(
                nameof(GetConversationById),
                new { conversationId = conversation.Id },
                conversation);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Conversation Request",
                detail: ex.Message,
                type: "https://httpstatuses.com/400");
        }
    }

    /// <summary>
    /// CH-03: Get list of recent conversations for the current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(PagedResponse<ConversationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] Guid? userId = null,
        [FromQuery] bool includeHidden = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = userId ?? ResolveCurrentUserId();
        var result = await _chatAppService.GetUserConversationsAsync(currentUserId, includeHidden, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get details of a specific conversation by ID.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationById(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatAppService.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conversation Not Found",
                detail: $"Conversation with ID '{conversationId}' was not found.",
                type: "https://httpstatuses.com/404");
        }

        return Ok(conversation);
    }

    /// <summary>
    /// CH-04 & CH-08: Get paginated message history in a conversation, with optional keyword search (?search=...).
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(PagedResponse<ChatMessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _chatAppService.GetMessagesAsync(conversationId, search, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conversation Not Found",
                detail: ex.Message,
                type: "https://httpstatuses.com/404");
        }
    }

    /// <summary>
    /// CH-02, CH-07, CH-10: Send a text or image/attachment message in a conversation,
    /// broadcast in real time via SignalR Hub, and publish Kafka event for push notification.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var senderUserId = request.SenderUserId ?? ResolveCurrentUserId();
            var message = await _chatAppService.SendMessageAsync(conversationId, senderUserId, request, cancellationToken);

            await _hubContext.Clients
                .Group(ChatHub.GetConversationGroupName(conversationId))
                .SendAsync("ReceiveMessage", message, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, message);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Message",
                detail: ex.Message,
                type: "https://httpstatuses.com/400");
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conversation Not Found",
                detail: ex.Message,
                type: "https://httpstatuses.com/404");
        }
    }

    /// <summary>
    /// CH-05: Mark unread messages in a conversation as Read and broadcast read receipt via SignalR.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/read")]
    [ProducesResponseType(typeof(MarkReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkConversationRead(
        [FromRoute] Guid conversationId,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var readerUserId = userId ?? ResolveCurrentUserId();
            var result = await _chatAppService.MarkConversationReadAsync(conversationId, readerUserId, cancellationToken);

            await _hubContext.Clients
                .Group(ChatHub.GetConversationGroupName(conversationId))
                .SendAsync("MessagesRead", result, cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conversation Not Found",
                detail: ex.Message,
                type: "https://httpstatuses.com/404");
        }
    }

    /// <summary>
    /// CH-09: Mute notifications or hide conversation from active chat list.
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}/settings")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConversationSettings(
        [FromRoute] Guid conversationId,
        [FromBody] UpdateConversationSettingsRequest request,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = userId ?? ResolveCurrentUserId();
        var updated = await _chatAppService.UpdateConversationSettingsAsync(
            conversationId, currentUserId, request, cancellationToken);

        if (updated is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Participant Not Found",
                detail: $"User '{currentUserId}' is not a participant in conversation '{conversationId}'.",
                type: "https://httpstatuses.com/404");
        }

        return Ok(updated);
    }

    private Guid ResolveCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (Guid.TryParse(claim, out var parsed))
        {
            return parsed;
        }

        if (Request.Headers.TryGetValue("X-User-Id", out var headerVal) &&
            Guid.TryParse(headerVal.FirstOrDefault(), out var headerGuid))
        {
            return headerGuid;
        }

        return DefaultDemoUserId;
    }
}
