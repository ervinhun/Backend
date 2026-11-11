using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Product;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and service on Postgres: checks the routes, binding, validation and error mapping.</summary>
public class ProductsEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsEndpointsTests(PostgresFixture database)
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

    private async Task<AuthResponse> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "correct-horse",
                displayName = "Anna",
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

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/products/123")]
    [InlineData("/api/product-types")]
    [InlineData("/api/product-categories")]
    public async Task Get_WithoutAToken_Returns401(string url)
    {
        var response = await SendAsync(null, HttpMethod.Get, url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetAndSearch_ReturnsTheProduct()
    {
        var anna = await RegisterAsync();

        var create = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/products",
            new { barcode = "5901234123457", name = "Oat milk" }
        );

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.EndsWith("/api/products/5901234123457", create.Headers.Location!.AbsolutePath);

        var get = await SendAsync(anna, HttpMethod.Get, "/api/products/5901234123457");
        Assert.Equal(created.Id, (await get.Content.ReadFromJsonAsync<ProductResponse>())!.Id);

        var search = await SendAsync(anna, HttpMethod.Get, "/api/products?search=OAT");
        Assert.Equal(
            created.Id,
            Assert.Single((await search.Content.ReadFromJsonAsync<List<ProductResponse>>())!).Id
        );
    }

    [Fact]
    public async Task Create_TakenBarcode_Returns409Problem()
    {
        var anna = await RegisterAsync();
        await SendAsync(anna, HttpMethod.Post, "/api/products", new { barcode = "1", name = "A" });

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/products",
            new { barcode = "1", name = "B" }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_DeletedBarcode_RestoresTheProduct()
    {
        var anna = await RegisterAsync();
        var first = await (
            await SendAsync(anna, HttpMethod.Post, "/api/products", new { barcode = "1", name = "Old" })
        ).Content.ReadFromJsonAsync<ProductResponse>();
        using (var scope = _factory.Services.CreateScope())
            await scope
                .ServiceProvider.GetRequiredService<AppDbContext>()
                .CatalogProducts.ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(anna, HttpMethod.Get, "/api/products/1")).StatusCode
        );

        var response = await SendAsync(anna, HttpMethod.Post, "/api/products", new { barcode = "1", name = "New" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var restored = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(first!.Id, restored.Id);
        Assert.Equal("New", restored.Name);
    }

    [Fact]
    public async Task Create_UnknownProductType_Returns404()
    {
        var anna = await RegisterAsync();

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/products",
            new
            {
                barcode = "1",
                name = "A",
                productTypeId = Guid.NewGuid(),
            }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400()
    {
        var anna = await RegisterAsync();

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/products",
            new { barcode = "", name = "A" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByBarcode_UnknownOrTooLong_Returns404Or400()
    {
        var anna = await RegisterAsync();

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(anna, HttpMethod.Get, "/api/products/nope")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await SendAsync(anna, HttpMethod.Get, $"/api/products/{new string('1', 65)}")
            ).StatusCode
        );
    }

    [Fact]
    public async Task CategoriesAndTypes_ReturnTheSeededLists()
    {
        var anna = await RegisterAsync();

        var categories = (
            await (
                await SendAsync(anna, HttpMethod.Get, "/api/product-categories")
            ).Content.ReadFromJsonAsync<List<ProductCategoryResponse>>()
        )!;
        var dairy = categories.Single(c => c.Name == "Dairy & Eggs");
        var types = (
            await (
                await SendAsync(anna, HttpMethod.Get, $"/api/product-types?categoryId={dairy.Id}")
            ).Content.ReadFromJsonAsync<List<ProductTypeResponse>>()
        )!;

        Assert.Equal(45, types.Count);
        Assert.All(types, t => Assert.Equal(dairy.Id, t.CategoryId));
        var butter = types.Single(t => t.Name == "Butter");
        Assert.Equal((60, 270), (butter.FridgeExpiryDays, butter.FreezerExpiryDays));
    }
}
