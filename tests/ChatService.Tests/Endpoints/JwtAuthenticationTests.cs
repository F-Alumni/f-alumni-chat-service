using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using Xunit;

namespace ChatService.Tests.Endpoints;

public class JwtAuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public JwtAuthenticationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateTestJwt(string userId, string role, TimeSpan? lifetime = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Email, $"{userId}@fpt.edu.vn"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1)),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_MissingToken_Returns401Unauthorized()
    {
        // Act (No Authorization header)
        var response = await _client.GetAsync("/api/v1/sample-items/admin-only");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_InvalidToken_Returns401Unauthorized()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sample-items/admin-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token.value");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_UserRoleToken_Returns403Forbidden()
    {
        // Arrange: Valid token with role USER (insufficient role)
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sample-items/admin-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_AdminRoleToken_Returns200Ok()
    {
        // Arrange: Valid token with role ADMIN
        var adminToken = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sample-items/admin-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSampleItem_MissingToken_Returns401Unauthorized()
    {
        // Arrange
        var payload = new CreateSampleItemRequest
        {
            Name = "Unauthorized Item",
            Description = "Should fail without token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sample-items", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSampleItem_ValidUserToken_Returns201Created()
    {
        // Arrange
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        var payload = new CreateSampleItemRequest
        {
            Name = "Authorized Item " + Guid.NewGuid().ToString()[..6],
            Description = "Created by authenticated user"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sample-items");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Content = JsonContent.Create(payload);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SampleItemResponse>();
        Assert.NotNull(created);
        Assert.Equal(payload.Name, created.Name);
    }

    [Fact]
    public async Task DeleteSampleItem_UserRoleToken_Returns403Forbidden()
    {
        // Arrange: User has role USER, but DELETE requires ADMIN
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/sample-items/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserProfile_ValidToken_ReturnsExtractedClaims()
    {
        // Arrange
        var userId = "student-12345";
        var token = GenerateTestJwt(userId, "USER");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sample-items/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(json);
        Assert.Equal(userId, json["userId"]);
        Assert.Equal("USER", json["role"]);
    }

    [Fact]
    public async Task PutEndpoint_MissingToken_Returns401Unauthorized()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var updateRequest = new UpdateSampleItemRequest { Name = "Updated Name" };

        // Act (No Authorization header)
        var response = await _client.PutAsJsonAsync($"/api/v1/sample-items/{itemId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutEndpoint_InvalidToken_Returns401Unauthorized()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var updateRequest = new UpdateSampleItemRequest { Name = "Updated Name" };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/sample-items/{itemId}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_MissingToken_Returns401Unauthorized()
    {
        // Arrange
        var itemId = Guid.NewGuid();

        // Act (No Authorization header)
        var response = await _client.DeleteAsync($"/api/v1/sample-items/{itemId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_InvalidToken_Returns401Unauthorized()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/sample-items/{itemId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_UserRoleWithoutAdmin_Returns403Forbidden()
    {
        // Arrange: Valid token with role USER (Delete requires ADMIN role)
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/sample-items/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_AdminRole_AllowsAccess()
    {
        // Arrange: Valid token with role ADMIN
        var adminToken = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/sample-items/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: Admin role is authorized (passes 401/403 check), returns 404 for random nonexistent ID
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Startup_MissingJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange: Factory configured with empty/missing Jwt:Key
        using var factoryWithoutKey = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("DatabaseProvider", "InMemory");
                builder.UseSetting("Jwt:Key", "");
            });

        // Act & Assert: Host startup fails fast with actionable exception
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factoryWithoutKey.CreateClient();
        });

        Assert.Contains("JWT signing key ('Jwt:Key') is missing", ex.Message);
    }

    [Fact]
    public void Startup_KeyShorterThan32Bytes_ThrowsInvalidOperationException()
    {
        // Arrange: Key with fewer than 32 bytes (256 bits)
        using var factoryWithShortKey = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("DatabaseProvider", "InMemory");
                builder.UseSetting("Jwt:Key", "too-short-secret-key");
            });

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factoryWithShortKey.CreateClient();
        });

        Assert.Contains("too short", ex.Message);
    }
}
