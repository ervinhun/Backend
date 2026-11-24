using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

public class ReverseProxyTests(PostgresFixture database) : DatabaseTest(database)
{
    private const int Limit = 2;
    private const string Proxy = "10.1.2.3";

    private async Task<HttpStatusCode> StatusAfterOtherClientsUsedTheLimitAsync(
        string connectsFrom, Dictionary<string, string?> settings, string header = "X-Forwarded-For")
    {
        settings["RateLimiting:Auth:PermitLimit"] = Limit.ToString();
        await using var factory = new ApiFactory(Database.ConnectionString, settings);
        using var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter>(new ConnectFrom(IPAddress.Parse(connectsFrom)))))
            .CreateClient();

        async Task<HttpStatusCode> LoginAsync(string clientIp)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = "nobody@example.com", password = "correct-horse" }),
            };
            request.Headers.Add(header, clientIp);
            return (await client.SendAsync(request)).StatusCode;
        }

        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync("203.0.113.1"));

        return await LoginAsync("203.0.113.2");
    }

    [Fact]
    public async Task KnownProxy_ClientsHaveTheirOwnBudget() =>
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await StatusAfterOtherClientsUsedTheLimitAsync(
                Proxy, new() { ["ReverseProxy:KnownNetworks:0"] = "10.0.0.0/8" }));

    [Fact]
    public async Task KnownProxy_WithItsOwnHeader_ClientsHaveTheirOwnBudget() =>
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await StatusAfterOtherClientsUsedTheLimitAsync(
                Proxy,
                new()
                {
                    ["ReverseProxy:KnownNetworks:0"] = "10.0.0.0/8",
                    ["ReverseProxy:ClientIpHeader"] = "X-Client-Ip",
                },
                "X-Client-Ip"));

    [Fact]
    public async Task UnknownProxy_HeaderIsIgnored() =>
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            await StatusAfterOtherClientsUsedTheLimitAsync(
                "198.51.100.7", new() { ["ReverseProxy:KnownNetworks:0"] = "10.0.0.0/8" }));

    [Fact]
    public async Task NoProxyConfigured_HeaderIsIgnored() =>
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            await StatusAfterOtherClientsUsedTheLimitAsync(Proxy, []));

    /// <summary>The test server has no real connection, so no remote IP of its own.</summary>
    private sealed class ConnectFrom(IPAddress ip) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ip;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
