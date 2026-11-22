using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using DataAccess;
using DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and services on Postgres: the settings and items the app schedules a member's reminders from.</summary>
public class NotificationItemsEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public NotificationItemsEndpointsTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "correct-horse",
                displayName,
            }
        );
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> SendAsync(AuthResponse caller, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private async Task<(Guid FridgeId, Guid ItemId)> CreateFridgeWithItemAsync(AuthResponse owner)
    {
        var response = await SendAsync(owner, HttpMethod.Post, "/api/fridges", new { name = "Home" });
        var fridge = (await response.Content.ReadFromJsonAsync<FridgeResponse>())!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var container = new Container
        {
            FridgeId = fridge.Id,
            Name = "Shelf",
            ContainerType = ContainerType.Fridge,
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();
        var item = new Item
        {
            ContainerId = container.Id,
            Name = "Bread",
            AddedByUserId = owner.User.Id,
            AddedAt = new DateOnly(2030, 1, 1),
            ExpiryDate = new DateOnly(2030, 1, 4),
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (fridge.Id, item.Id);
    }

    [Fact]
    public async Task Member_GetsTheSettingsAndTheirItems_WithTheTimeAsHoursAndMinutes()
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, itemId) = await CreateFridgeWithItemAsync(anna);
        await SendAsync(
            anna,
            HttpMethod.Put,
            $"/api/fridges/{fridgeId}/notification-settings",
            new
            {
                warningDaysBefore = 7,
                dangerDaysBefore = 2,
                recipientsScope = "all_members",
                notificationTime = "08:30",
            }
        );

        var response = await SendAsync(anna, HttpMethod.Get, $"/api/fridges/{fridgeId}/notification-items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal("08:30:00", body.GetProperty("notificationTime").GetString());
        var item = body.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(itemId, item.GetProperty("id").GetGuid());
        Assert.Equal("2030-01-04", item.GetProperty("expiryDate").GetString());
        Assert.True(item.GetProperty("skipWarning").GetBoolean());
    }

    [Fact]
    public async Task NonMember_Gets404()
    {
        var anna = await RegisterAsync("Anna");
        var eve = await RegisterAsync("Eve");
        var (fridgeId, _) = await CreateFridgeWithItemAsync(anna);

        var response = await SendAsync(eve, HttpMethod.Get, $"/api/fridges/{fridgeId}/notification-items");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
