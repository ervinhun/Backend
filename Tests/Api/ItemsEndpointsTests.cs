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
}
