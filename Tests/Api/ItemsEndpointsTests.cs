using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Container;
using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Dtos.Item;
using DataAccess;
using DataAccess.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and services on Postgres, including who may see and change a fridge's items.</summary>
public class ItemsEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ItemsEndpointsTests(PostgresFixture database)
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

    private Task<HttpResponseMessage> SendAsync(
        AuthResponse? caller,
        HttpMethod method,
        string url,
        object? body = null
    )
    {
        var request = new HttpRequestMessage(method, url);
        if (caller is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                caller.AccessToken
            );
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private record Setup(AuthResponse Owner, FridgeResponse Fridge, ContainerResponse Container);

    private async Task<Setup> CreateFridgeAsync()
    {
        var owner = await RegisterAsync("Anna");
        var fridge = (
            await (
                await SendAsync(owner, HttpMethod.Post, "/api/fridges", new { name = "Home" })
            ).Content.ReadFromJsonAsync<FridgeResponse>()
        )!;
        var container = (
            await (
                await SendAsync(
                    owner,
                    HttpMethod.Post,
                    $"/api/fridges/{fridge.Id}/containers",
                    new { name = "Top shelf", type = "fridge" }
                )
            ).Content.ReadFromJsonAsync<ContainerResponse>()
        )!;
        return new Setup(owner, fridge, container);
    }

    private async Task AddMemberAsync(Setup s, AuthResponse user, bool canPut, bool canUse)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = s.Fridge.Id,
                UserId = user.User.Id,
                CanPut = canPut,
                CanUse = canUse,
            }
        );
        await db.SaveChangesAsync();
    }

    private static string Url(Setup s, Guid? itemId = null, string? suffix = null) =>
        $"/api/fridges/{s.Fridge.Id}/items" + (itemId is null ? "" : $"/{itemId}") + suffix;

    private static object NewItem(Setup s, string name = "Milk") =>
        new
        {
            containerId = s.Container.Id,
            name,
            amount = "1",
            unit = "l",
            expiryDate = "2030-07-01",
        };

    private async Task<ItemResponse> CreateItemAsync(
        Setup s,
        AuthResponse caller,
        string name = "Milk"
    )
    {
        var response = await SendAsync(caller, HttpMethod.Post, Url(s), NewItem(s, name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    private static async Task<ProblemDetails> AssertProblemAsync(
        HttpResponseMessage response,
        int status
    )
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(status, problem!.Status);
        return problem;
    }

    [Fact]
    public async Task Owner_CreatesListsChangesAndDeletesItems()
    {
        var s = await CreateFridgeAsync();

        var response = await SendAsync(s.Owner, HttpMethod.Post, Url(s), NewItem(s));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
        Assert.EndsWith(Url(s, created.Id), response.Headers.Location!.AbsolutePath);
        Assert.Equal(("Milk", "1", "l"), (created.Name, created.Amount, created.Unit));
        Assert.Equal(new DateOnly(2030, 7, 1), created.ExpiryDate);
        Assert.Equal(s.Owner.User.Id, created.AddedByUserId);

        var list = await (
            await SendAsync(s.Owner, HttpMethod.Get, Url(s) + $"?containerId={s.Container.Id}")
        ).Content.ReadFromJsonAsync<List<ItemResponse>>();
        Assert.Equal(created.Id, Assert.Single(list!).Id);

        var updated = await SendAsync(
            s.Owner,
            HttpMethod.Put,
            Url(s, created.Id),
            new
            {
                containerId = s.Container.Id,
                name = "Oat milk",
                expiryDate = "2030-08-01",
            }
        );
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Oat milk", (await updated.Content.ReadFromJsonAsync<ItemResponse>())!.Name);

        var consumed = await SendAsync(
            s.Owner,
            HttpMethod.Put,
            Url(s, created.Id, "/status"),
            new { status = "consumed" }
        );
        Assert.Equal(
            "consumed",
            (await consumed.Content.ReadFromJsonAsync<ItemResponse>())!.Status
        );
        Assert.Empty(
            (
                await (await SendAsync(s.Owner, HttpMethod.Get, Url(s))).Content.ReadFromJsonAsync<
                    List<ItemResponse>
                >()
            )!
        );
        Assert.Single(
            (
                await (
                    await SendAsync(s.Owner, HttpMethod.Get, Url(s) + "?status=consumed")
                ).Content.ReadFromJsonAsync<List<ItemResponse>>()
            )!
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(s.Owner, HttpMethod.Delete, Url(s, created.Id))).StatusCode
        );
        await AssertProblemAsync(
            await SendAsync(s.Owner, HttpMethod.Get, Url(s, created.Id)),
            StatusCodes.Status404NotFound
        );
    }

    [Fact]
    public async Task WithoutAToken_Returns401()
    {
        var s = await CreateFridgeAsync();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await SendAsync(null, HttpMethod.Get, Url(s))).StatusCode
        );
    }

    [Fact]
    public async Task Stranger_GetsNotFound_ForEveryAction()
    {
        var s = await CreateFridgeAsync();
        var cleo = await RegisterAsync("Cleo");
        var item = await CreateItemAsync(s, s.Owner);
        var update = new
        {
            containerId = s.Container.Id,
            name = "Mine",
            expiryDate = "2030-07-01",
        };

        var responses = new[]
        {
            await SendAsync(cleo, HttpMethod.Get, Url(s)),
            await SendAsync(cleo, HttpMethod.Get, Url(s, item.Id)),
            await SendAsync(cleo, HttpMethod.Post, Url(s), NewItem(s)),
            await SendAsync(cleo, HttpMethod.Put, Url(s, item.Id), update),
            await SendAsync(
                cleo,
                HttpMethod.Put,
                Url(s, item.Id, "/status"),
                new { status = "removed" }
            ),
            await SendAsync(cleo, HttpMethod.Delete, Url(s, item.Id)),
        };

        foreach (var response in responses)
            await AssertProblemAsync(response, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task MemberRights_DecideWhoMayChangeAndWhoMayUse()
    {
        var s = await CreateFridgeAsync();
        var ben = await RegisterAsync("Ben");
        var cleo = await RegisterAsync("Cleo");
        await AddMemberAsync(s, ben, canPut: true, canUse: false);
        await AddMemberAsync(s, cleo, canPut: false, canUse: true);
        var item = await CreateItemAsync(s, ben, "Bens milk");

        await AssertProblemAsync(
            await SendAsync(
                ben,
                HttpMethod.Put,
                Url(s, item.Id, "/status"),
                new { status = "consumed" }
            ),
            StatusCodes.Status403Forbidden
        );
        await AssertProblemAsync(
            await SendAsync(cleo, HttpMethod.Post, Url(s), NewItem(s)),
            StatusCodes.Status403Forbidden
        );
        await AssertProblemAsync(
            await SendAsync(cleo, HttpMethod.Delete, Url(s, item.Id)),
            StatusCodes.Status403Forbidden
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await SendAsync(
                    cleo,
                    HttpMethod.Put,
                    Url(s, item.Id, "/status"),
                    new { status = "consumed" }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(ben, HttpMethod.Delete, Url(s, item.Id))).StatusCode
        );
    }

    [Fact]
    public async Task InvalidRequests_Return400()
    {
        var s = await CreateFridgeAsync();

        var missingName = await AssertProblemAsync(
            await SendAsync(
                s.Owner,
                HttpMethod.Post,
                Url(s),
                new { containerId = s.Container.Id, expiryDate = "2030-07-01" }
            ),
            StatusCodes.Status400BadRequest
        );
        await AssertProblemAsync(
            await SendAsync(
                s.Owner,
                HttpMethod.Post,
                Url(s),
                new { name = "Milk", expiryDate = "2030-07-01" }
            ),
            StatusCodes.Status400BadRequest
        );
        await AssertProblemAsync(
            await SendAsync(
                s.Owner,
                HttpMethod.Put,
                Url(s, Guid.NewGuid(), "/status"),
                new { status = "expired" }
            ),
            StatusCodes.Status400BadRequest
        );
        await AssertProblemAsync(
            await SendAsync(s.Owner, HttpMethod.Get, Url(s) + "?status=spoiled"),
            StatusCodes.Status400BadRequest
        );

        Assert.Equal("A name is required when no product is given.", missingName.Detail);
    }

    [Fact]
    public async Task UnknownItemOrContainer_Returns404()
    {
        var s = await CreateFridgeAsync();
        var item = await CreateItemAsync(s, s.Owner);

        await AssertProblemAsync(
            await SendAsync(s.Owner, HttpMethod.Get, Url(s, Guid.NewGuid())),
            StatusCodes.Status404NotFound
        );
        await AssertProblemAsync(
            await SendAsync(
                s.Owner,
                HttpMethod.Put,
                Url(s, item.Id),
                new
                {
                    containerId = Guid.NewGuid(),
                    name = "Milk",
                    expiryDate = "2030-07-01",
                }
            ),
            StatusCodes.Status404NotFound
        );
    }

    private static object Synced(
        DateTimeOffset changedAt,
        string name = "Milk",
        string storage = "fridge",
        Guid? productTypeId = null,
        string? openedOn = null
    ) =>
        new
        {
            storage,
            name,
            productTypeId,
            amount = "1 l",
            expiryDate = openedOn is null ? "2030-07-01" : "2030-06-20",
            bestBefore = "2030-07-01",
            openedOn,
            changedAt,
        };

    private Task<HttpResponseMessage> SyncAsync(Setup s, AuthResponse caller, Guid id, object body) =>
        SendAsync(caller, HttpMethod.Put, Url(s, id, "/sync"), body);

    private async Task<ItemResponse> SyncedAsync(Setup s, Guid id, object body)
    {
        var response = await SyncAsync(s, s.Owner, id, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    private async Task<List<ItemResponse>> ListAsync(Setup s) =>
        (await (await SendAsync(s.Owner, HttpMethod.Get, Url(s))).Content.ReadFromJsonAsync<List<ItemResponse>>())!;

    [Fact]
    public async Task Sync_AddsTheItemUnderItsOwnId_OnlyOnce()
    {
        var s = await CreateFridgeAsync();
        var id = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow.AddDays(-2);

        await SyncedAsync(s, id, Synced(changedAt));
        await SyncedAsync(s, id, Synced(changedAt));

        var item = Assert.Single(await ListAsync(s));
        Assert.Equal((id, s.Container.Id, "Milk", "1 l"), (item.Id, item.ContainerId, item.Name, item.Amount));
        // Added when the user added it, not when it got here.
        Assert.Equal(DateOnly.FromDateTime(changedAt.UtcDateTime), item.AddedAt);
    }

    [Fact]
    public async Task Sync_KeepsTheTypeAndWhenItWasOpened()
    {
        var s = await CreateFridgeAsync();
        var id = Guid.NewGuid();
        ProductType type;
        using (var scope = _factory.Services.CreateScope())
            type = scope.ServiceProvider.GetRequiredService<AppDbContext>().ProductTypes
                .First(t => t.CategoryId != null);
        var now = DateTimeOffset.UtcNow;
        await SyncedAsync(s, id, Synced(now.AddHours(-1), productTypeId: type.Id));

        // A type that isn't known any more, or is left out, doesn't lose the old one.
        var opened = await SyncedAsync(
            s, id, Synced(now, productTypeId: Guid.NewGuid(), openedOn: "2030-06-16"));

        Assert.Equal((type.Id, type.CategoryId), (opened.ProductTypeId!.Value, opened.CategoryId));
        Assert.Equal(new DateOnly(2030, 6, 16), opened.OpenedOn);
        Assert.Equal(new DateOnly(2030, 6, 20), opened.ExpiryDate);
        Assert.Equal(new DateOnly(2030, 7, 1), opened.BestBefore);
    }

    [Fact]
    public async Task Sync_LaterChangeWins_WhicheverArrivesFirst()
    {
        var s = await CreateFridgeAsync();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SyncedAsync(s, id, Synced(now.AddHours(-1), "Later change"));

        var answer = await SyncedAsync(s, id, Synced(now.AddHours(-2), "Earlier, sent late"));
        Assert.Equal("Later change", answer.Name);

        Assert.Equal("Latest", (await SyncedAsync(s, id, Synced(now, "Latest"))).Name);
    }

    [Fact]
    public async Task Sync_WithAClockThatIsAhead_DoesNotBlockLaterChanges()
    {
        var s = await CreateFridgeAsync();
        var id = Guid.NewGuid();
        await SyncedAsync(s, id, Synced(DateTimeOffset.UtcNow.AddYears(1), "From the future"));

        Assert.Equal("Now", (await SyncedAsync(s, id, Synced(DateTimeOffset.UtcNow, "Now"))).Name);
    }

    [Fact]
    public async Task Sync_OlderThanAChangeMadeOnline_KeepsThatChange()
    {
        var s = await CreateFridgeAsync();
        var item = await CreateItemAsync(s, s.Owner);
        await SendAsync(
            s.Owner,
            HttpMethod.Put,
            Url(s, item.Id),
            new { containerId = s.Container.Id, name = "Oat milk", expiryDate = "2030-08-01" }
        );

        var answer = await SyncedAsync(s, item.Id, Synced(DateTimeOffset.UtcNow.AddMinutes(-5), "Old"));

        Assert.Equal("Oat milk", answer.Name);
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("removed")]
    [InlineData("consumed")]
    public async Task Sync_OfARemovedItem_Returns410_AndDoesNotBringItBack(string how)
    {
        var s = await CreateFridgeAsync();
        var item = await CreateItemAsync(s, s.Owner);
        if (how == "deleted")
            await SendAsync(s.Owner, HttpMethod.Delete, Url(s, item.Id));
        else
            await SendAsync(s.Owner, HttpMethod.Put, Url(s, item.Id, "/status"), new { status = how });

        var response = await SyncAsync(s, s.Owner, item.Id, Synced(DateTimeOffset.UtcNow.AddMinutes(1)));

        await AssertProblemAsync(response, StatusCodes.Status410Gone);
        Assert.Empty(await ListAsync(s));
    }

    [Fact]
    public async Task Sync_OfAnItemInAnotherFridge_Returns410_AndLeavesIt()
    {
        var s = await CreateFridgeAsync();
        var other = await CreateFridgeAsync();
        var item = await CreateItemAsync(other, other.Owner, "Theirs");

        var response = await SyncAsync(s, s.Owner, item.Id, Synced(DateTimeOffset.UtcNow, "Mine"));

        await AssertProblemAsync(response, StatusCodes.Status410Gone);
        Assert.Equal("Theirs", Assert.Single(await ListAsync(other)).Name);
    }

    [Fact]
    public async Task Sync_CreatesTheContainerOnce_AndLeavesAnItemWhereItIs()
    {
        var s = await CreateFridgeAsync();
        var now = DateTimeOffset.UtcNow;
        var door = (await (
            await SendAsync(
                s.Owner,
                HttpMethod.Post,
                $"/api/fridges/{s.Fridge.Id}/containers",
                new { name = "Door", type = "fridge" }
            )
        ).Content.ReadFromJsonAsync<ContainerResponse>())!;
        var inDoor = await CreateItemAsync(s with { Container = door }, s.Owner);

        var peas = await SyncedAsync(s, Guid.NewGuid(), Synced(now, "Peas", "freezer"));
        var fish = await SyncedAsync(s, Guid.NewGuid(), Synced(now, "Fish", "freezer"));
        var stillInDoor = await SyncedAsync(s, inDoor.Id, Synced(now.AddMinutes(1), "Milk"));

        Assert.Equal(peas.ContainerId, fish.ContainerId);
        Assert.NotEqual(s.Container.Id, peas.ContainerId);
        Assert.Equal(door.Id, stillInDoor.ContainerId);
    }

    [Fact]
    public async Task Sync_AtTheSameTime_AddsOneItemAndOneContainer()
    {
        var s = await CreateFridgeAsync();
        var id = Guid.NewGuid();
        var body = Synced(DateTimeOffset.UtcNow, "Rice", "pantry");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => SyncAsync(s, s.Owner, id, body)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Single(await ListAsync(s));
        var containers = await (
            await SendAsync(s.Owner, HttpMethod.Get, $"/api/fridges/{s.Fridge.Id}/containers")
        ).Content.ReadFromJsonAsync<List<ContainerResponse>>();
        Assert.Single(containers!, c => c.Type == "pantry");
    }

    [Fact]
    public async Task Sync_WithoutRights_IsRefused()
    {
        var s = await CreateFridgeAsync();
        var ben = await RegisterAsync("Ben");
        var cleo = await RegisterAsync("Cleo");
        await AddMemberAsync(s, ben, canPut: false, canUse: true);
        var body = Synced(DateTimeOffset.UtcNow);

        await AssertProblemAsync(
            await SyncAsync(s, ben, Guid.NewGuid(), body), StatusCodes.Status403Forbidden);
        await AssertProblemAsync(
            await SyncAsync(s, cleo, Guid.NewGuid(), body), StatusCodes.Status404NotFound);
        await AssertProblemAsync(
            await SyncAsync(s, s.Owner, Guid.NewGuid(), Synced(DateTimeOffset.UtcNow, storage: "shed")),
            StatusCodes.Status400BadRequest);
        Assert.Empty(await ListAsync(s));
    }
}
