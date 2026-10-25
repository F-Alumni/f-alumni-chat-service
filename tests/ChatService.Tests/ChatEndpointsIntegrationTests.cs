using System.Net;
using System.Net.Http.Json;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;

namespace ChatService.Tests.Endpoints;

public class ChatEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ChatEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ping_And_Health_ReturnOk()
    {
        var client = _factory.CreateClient();

        var pingRes = await client.GetAsync("/ping");
        Assert.Equal(HttpStatusCode.OK, pingRes.StatusCode);

        var apiPingRes = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, apiPingRes.StatusCode);

        var healthRes = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthRes.StatusCode);
    }

    [Fact]
    public async Task GetParticipantProfile_ValidSeededUser_Returns200WithGrpcMetadata()
    {
        var client = _factory.CreateClient();
        var conversationId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{UserA}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(body);
        Assert.Equal(UserA, body.UserId);
        Assert.Equal("Nguyen Van A", body.DisplayName);
        Assert.Equal("gRPC (HTTP/2 Protobuf)", body.ResolvedVia);
    }

    [Fact]
    public async Task GetParticipantProfile_UnknownUser_Returns404ProblemDetails()
    {
        var client = _factory.CreateClient();
        var conversationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var unknownId = Guid.Empty;

        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{unknownId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullChatLifecycle_Create_SendMessage_Search_Read_And_Mute_WorksEndToEnd()
    {
        var client = _factory.CreateClient();

        // 1. CH-01: Create 1-1 Conversation between UserA and UserC
        var createReq = new CreateConversationRequest
        {
            TargetUserId = UserC,
            IsGroup = false
        };

        var createRes = await client.PostAsJsonAsync($"/api/v1/chats/conversations?userId={UserA}", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var conversation = await createRes.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal(2, conversation.Participants.Count);

        // 2. CH-02 & CH-07: Send message with text and image attachment
        var sendReq = new SendMessageRequest
        {
            SenderUserId = UserA,
            Content = "Xin chào anh Lê Hoàng C, em muốn hỏi về cơ hội thực tập AI tại FPT Software!",
            AttachmentUrl = "https://cdn.falumni.local/chat/cv-nguyenvana.png",
            AttachmentType = "Image"
        };

        var sendRes = await client.PostAsJsonAsync($"/api/v1/chats/conversations/{conversation.Id}/messages", sendReq);
        Assert.Equal(HttpStatusCode.Created, sendRes.StatusCode);

        var sentMsg = await sendRes.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(sentMsg);
        Assert.Equal("Sent", sentMsg.Status);
        Assert.Equal("https://cdn.falumni.local/chat/cv-nguyenvana.png", sentMsg.AttachmentUrl);

        // 3. CH-04 & CH-08: Search messages in conversation
        var searchRes = await client.GetAsync($"/api/v1/chats/conversations/{conversation.Id}/messages?search=FPT%20Software");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);

        var pagedMessages = await searchRes.Content.ReadFromJsonAsync<PagedResponse<ChatMessageResponse>>();
        Assert.NotNull(pagedMessages);
        Assert.Single(pagedMessages.Items);

        // 4. CH-05: Mark conversation as read by UserC
        var readRes = await client.PostAsync($"/api/v1/chats/conversations/{conversation.Id}/read?userId={UserC}", null);
        Assert.Equal(HttpStatusCode.OK, readRes.StatusCode);

        var readBody = await readRes.Content.ReadFromJsonAsync<MarkReadResponse>();
        Assert.NotNull(readBody);
        Assert.Equal(1, readBody.MarkedCount);

        // 5. CH-09: Mute conversation notifications for UserC
        var settingsReq = new UpdateConversationSettingsRequest
        {
            IsMuted = true,
            IsHidden = false
        };

        var settingsRes = await client.PutAsJsonAsync(
            $"/api/v1/chats/conversations/{conversation.Id}/settings?userId={UserC}",
            settingsReq);
        Assert.Equal(HttpStatusCode.OK, settingsRes.StatusCode);

        var updatedParticipant = await settingsRes.Content.ReadFromJsonAsync<ParticipantDto>();
        Assert.NotNull(updatedParticipant);
        Assert.True(updatedParticipant.IsMuted);

        // 6. CH-03: Get user conversations list
        var listRes = await client.GetAsync($"/api/v1/chats/conversations?userId={UserA}");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var listBody = await listRes.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>();
        Assert.NotNull(listBody);
        Assert.NotEmpty(listBody.Items);
    }
}
