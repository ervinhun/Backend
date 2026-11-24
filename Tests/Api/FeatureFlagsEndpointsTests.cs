using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;
using Tests.Fakes;

namespace Tests.Api;

/// <summary>Flag-gated actions and the feature-toggle endpoint, with FeatureHub replaced by <see cref="FakeFeatureFlags"/>.</summary>
public class FeatureFlagsEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    private readonly FakeFeatureFlags _flags;

    public FeatureFlagsEndpointsTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
        _flags = (FakeFeatureFlags)_factory.Services.GetRequiredService<FeatureHubHelper.IFeatureFlags>();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "correct-horse", displayName = "Anna" });
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> SendAsync(AuthResponse? caller, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (caller is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task CreateContainer_WhenFlagOff_Returns403_AndFlagGetsTheUserAsKey()
    {
        var anna = await RegisterAsync();
        var fridge = (await (await SendAsync(anna, HttpMethod.Post, "/api/fridges", new { name = "Home" }))
            .Content.ReadFromJsonAsync<FridgeResponse>())!;
        _flags.Enabled = false;

        var response = await SendAsync(anna, HttpMethod.Post, $"/api/fridges/{fridge.Id}/containers",
            new { name = "Top shelf", type = "fridge" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Feature not enabled", problem!.Title);
        Assert.Equal("createContainer", _flags.LastFeature);
        Assert.Equal(anna.User.Id.ToString(), _flags.LastRequestId);
    }

    [Fact]
    public async Task FeatureToggle_RequiresLogin()
    {
        var response = await SendAsync(null, HttpMethod.Get, "/feature-toggles/createContainer");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FeatureToggle_ReturnsTheFlag_AndPassesCountry(bool enabled)
    {
        var anna = await RegisterAsync();
        _flags.Enabled = enabled;

        var response = await SendAsync(anna, HttpMethod.Get, "/feature-toggles/createContainer?country=Hungary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(enabled, await response.Content.ReadFromJsonAsync<bool>());
        Assert.Equal("Hungary", _flags.LastCountry);
        Assert.Equal(anna.User.Id.ToString(), _flags.LastRequestId);
    }
}
