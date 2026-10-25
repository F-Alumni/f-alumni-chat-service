using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using Xunit;

namespace ChatService.Tests.Endpoints;

public class SampleItemsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public SampleItemsEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        // Configure default client with Admin token for authenticated operations
        var adminToken = GenerateTestJwt("test-admin-id", "ADMIN");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    }

    private static string GenerateTestJwt(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task GetAllEndpoint_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/sample-items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<SampleItemResponse>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task PostEndpoint_ValidPayload_ReturnsCreatedAndResource()
    {
        // Arrange
        var request = new CreateSampleItemRequest
        {
            Name = "Integration Test Item " + Guid.NewGuid().ToString()[..8],
            Description = "Created via WebApplicationFactory test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sample-items", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(created);
        Assert.Equal(request.Name, created.Name);
        Assert.NotNull(response.Headers.Location);

        // Cleanup
        var deleteResponse = await _client.DeleteAsync($"/api/v1/sample-items/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetPagedEndpoint_ReturnsPagedEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/sample-items/paged?pageNumber=1&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<SampleItemResponse>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.PageNumber);
        Assert.Equal(5, pagedResult.PageSize);
    }

    [Fact]
    public async Task GetByIdEndpoint_NonExistingId_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/sample-items/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutEndpoint_ValidPayload_ReturnsOkAndUpdatedResource()
    {
        // Arrange: Create a new resource first
        var createRequest = new CreateSampleItemRequest
        {
            Name = "Item To Update " + Guid.NewGuid().ToString()[..8],
            Description = "Original description"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/sample-items", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(created);

        var updateRequest = new UpdateSampleItemRequest
        {
            Name = "Successfully Updated Name",
            Description = "Updated Description Content",
            IsActive = false
        };

        // Act
        var putResponse = await _client.PutAsJsonAsync($"/api/v1/sample-items/{created.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Successfully Updated Name", updated.Name);
        Assert.Equal("Updated Description Content", updated.Description);
        Assert.False(updated.IsActive);

        // Verify with GET
        var getResponse = await _client.GetAsync($"/api/v1/sample-items/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal("Successfully Updated Name", retrieved.Name);
        Assert.False(retrieved.IsActive);

        // Cleanup
        await _client.DeleteAsync($"/api/v1/sample-items/{created.Id}");
    }

    [Fact]
    public async Task PutEndpoint_NonExistingId_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateSampleItemRequest
        {
            Name = "Valid Name",
            Description = "Valid Description",
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/sample-items/{nonExistingId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutEndpoint_InvalidPayload_ReturnsBadRequest()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var invalidRequest = new UpdateSampleItemRequest
        {
            Name = "", // Required, min 2 chars
            Description = "Invalid empty name"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/sample-items/{itemId}", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_ExistingId_ReturnsNoContent()
    {
        // Arrange: Create a resource to delete
        var createRequest = new CreateSampleItemRequest
        {
            Name = "Item For Delete Test " + Guid.NewGuid().ToString()[..8],
            Description = "To be deleted"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/sample-items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(created);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/sample-items/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify subsequent GET returns 404
        var getResponse = await _client.GetAsync($"/api/v1/sample-items/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_NonExistingId_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/sample-items/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
