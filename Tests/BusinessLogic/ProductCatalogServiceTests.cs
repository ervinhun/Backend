using BusinessLogic.Dtos.Product;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;
using Tests.Fakes;

namespace Tests.BusinessLogic;

/// <summary>ProductCatalogService against the real repository and Postgres.</summary>
public class ProductCatalogServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private ProductCatalogService Service =>
        new(new ProductCatalogRepository(Database.CreateDbContext(), TimeProvider.System));

    private async Task<ProductType> AnyTypeAsync()
    {
        await using var db = Database.CreateDbContext();
        return await db.ProductTypes.FirstAsync();
    }

    [Fact]
    public async Task GetCategoriesAndTypes_ReturnTheSeededLists()
    {
        var categories = await Service.GetCategoriesAsync();
        var dairy = categories.Single(c => c.Name == "Dairy & Eggs");

        var types = await Service.GetTypesAsync(dairy.Id);

        Assert.Equal(16, categories.Count);
        Assert.Equal(45, types.Count);
        Assert.Contains("Eggs (in shell)", types.Select(t => t.Name));
        var butter = types.Single(t => t.Name == "Butter");
        Assert.Equal(60, butter.DefaultExpiryDays);
        Assert.Null(butter.PantryExpiryDays);
        Assert.Equal(60, butter.FridgeExpiryDays);
        Assert.Equal(270, butter.FreezerExpiryDays);
        Assert.True((await Service.GetTypesAsync(null)).Count > types.Count);
    }

    [Fact]
    public async Task Create_ThenGetByBarcode_TrimsAndStoresTheProduct()
    {
        var type = await AnyTypeAsync();

        var created = await Service.CreateAsync(
            new CreateProductRequest
            {
                Barcode = " 5901234123457 ",
                Name = "  Milk ",
                ProductTypeId = type.Id,
                ImageUrl = "https://example.com/m.jpg",
            }
        );
        var found = await Service.GetByBarcodeAsync(" 5901234123457");

        Assert.Equal(created.Id, found.Id);
        Assert.Equal("5901234123457", found.Barcode);
        Assert.Equal("Milk", found.Name);
        Assert.Equal(type.Id, found.ProductTypeId);
        Assert.Equal("https://example.com/m.jpg", found.ImageUrl);
        Assert.Equal(CatalogProduct.ManualSource, found.Source);
    }

    [Fact]
    public async Task Create_EmptyImageUrl_IsStoredAsNone()
    {
        var created = await Service.CreateAsync(
            new CreateProductRequest { Barcode = "1", Name = "A", ImageUrl = "" }
        );

        Assert.Null(created.ImageUrl);
    }

    [Fact]
    public async Task Create_TakenBarcode_ThrowsConflict()
    {
        await Service.CreateAsync(new CreateProductRequest { Barcode = "1", Name = "A" });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CreateAsync(new CreateProductRequest { Barcode = " 1", Name = "B" })
        );
        Assert.Equal("A product with this barcode already exists.", ex.Message);
    }

    [Fact]
    public async Task Create_BarcodeTakenBetweenCheckAndInsert_ThrowsConflict()
    {
        var service = new ProductCatalogService(
            new CollidingProductCatalogRepository(
                new ProductCatalogRepository(Database.CreateDbContext(), TimeProvider.System)
            )
        );

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Barcode = "1", Name = "A" })
        );
        Assert.Equal("A product with this barcode already exists.", ex.Message);
    }

    [Fact]
    public async Task Create_UnknownProductType_ThrowsNotFoundAndSavesNothing()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CreateAsync(
                new CreateProductRequest
                {
                    Barcode = "1",
                    Name = "A",
                    ProductTypeId = Guid.NewGuid(),
                }
            )
        );

        Assert.Equal("Product type not found.", ex.Message);
        Assert.Empty(await Service.SearchAsync(null, null));
    }

    [Fact]
    public async Task GetByBarcode_Unknown_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Service.GetByBarcodeAsync("nope"));

        Assert.Equal("Product not found.", ex.Message);
    }

    [Fact]
    public async Task Search_ReturnsAtMostTheMaximum()
    {
        var service = Service;
        for (var i = 0; i < ProductCatalogService.MaxSearchResults + 2; i++)
            await service.CreateAsync(
                new CreateProductRequest { Barcode = $"b{i}", Name = $"Milk {i:000}" }
            );

        var found = await service.SearchAsync("milk", null);

        Assert.Equal(ProductCatalogService.MaxSearchResults, found.Count);
    }

    [Fact]
    public async Task Search_FiltersByProductType()
    {
        var type = await AnyTypeAsync();
        await Service.CreateAsync(
            new CreateProductRequest { Barcode = "1", Name = "Typed", ProductTypeId = type.Id }
        );
        await Service.CreateAsync(new CreateProductRequest { Barcode = "2", Name = "Untyped" });

        Assert.Equal("Typed", Assert.Single(await Service.SearchAsync(null, type.Id)).Name);
    }
}
