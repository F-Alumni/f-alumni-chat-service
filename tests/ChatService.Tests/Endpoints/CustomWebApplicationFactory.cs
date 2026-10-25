using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChatService.Tests.Endpoints;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Deterministic signing key strictly reserved for automated testing (xUnit test fixture).
    /// WARNING: DO NOT USE IN RUNTIME, DEVELOPMENT, OR PRODUCTION ENVIRONMENTS.
    /// Real runtime signing keys must be randomly generated and supplied via User Secrets or environment variables.
    /// </summary>
    public const string TestJwtSecret = "TestFixtureDeterministicKey_AtLeast32BytesLong_PRN232_ASM_2026!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "InMemory");
        builder.UseSetting("Jwt:Key", TestJwtSecret);
    }
}
