using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Api;

/// <summary>The real API wired to the test Postgres, with JWT settings from <see cref="TestJwt"/>.</summary>
public class ApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? settings = null
) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting, unlike ConfigureAppConfiguration, is visible to Program.cs while it builds the services.
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Jwt:Secret", TestJwt.Options.Secret);
        builder.UseSetting("Jwt:Issuer", TestJwt.Options.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Options.Audience);
        // Most tests share one client IP; the rate limit tests lower this themselves.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:InviteCode:PermitLimit", "10000");

        if (settings is not null)
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
    }
}
