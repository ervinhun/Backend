using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class ProductCatalogRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private ProductCatalogRepository Repository => new(Database.CreateDbContext(), TimeProvider.System);

    // The seeded lookup rows survive the reset between tests, so a test that deletes one puts it back afterwards.
    private async Task SetCategoryDeletedAsync(string name, DateTimeOffset? at)
    {
        await using var db = Database.CreateDbContext();
        await db.Database.ExecuteSqlAsync($"UPDATE product_categories SET deleted_at = {at} WHERE name = {name}");
    }

    private async Task SetTypeDeletedAsync(string name, DateTimeOffset? at)
    {
        await using var db = Database.CreateDbContext();
        await db.Database.ExecuteSqlAsync($"UPDATE product_types SET deleted_at = {at} WHERE name = {name}");
    }

    private async Task<ProductType> AnyTypeAsync()
    {
        await using var db = Database.CreateDbContext();
        return await db.ProductTypes.OrderBy(t => t.Name).FirstAsync();
    }

    private async Task<CatalogProduct> AddProductAsync(
        string barcode,
        string name = "Milk",
        Guid? typeId = null
    )
    {
        var product = new CatalogProduct
        {
            Barcode = barcode,
            Name = name,
            ProductTypeId = typeId,
        };
        await Repository.AddAsync(product);
        return product;
    }

    [Fact]
    public async Task GetCategories_ReturnsTheSeededOnesByName()
    {
        var categories = await Repository.GetCategoriesAsync();

        Assert.NotEmpty(categories);
        Assert.Equal(categories.OrderBy(c => c.Name, StringComparer.Ordinal).Select(c => c.Name), categories.Select(c => c.Name));
    }

    [Fact]
    public async Task GetCategories_LeavesOutDeletedOnes()
    {
        await SetCategoryDeletedAsync("Bakery", DateTimeOffset.UtcNow);
        try
        {
            var categories = await Repository.GetCategoriesAsync();

            Assert.DoesNotContain(categories, c => c.Name == "Bakery");
        }
        finally
        {
            await SetCategoryDeletedAsync("Bakery", null);
        }
    }

    [Fact]
    public async Task GetTypes_WithoutCategory_ReturnsAllByName()
    {
        var types = await Repository.GetTypesAsync();

        // In the database's own order: its collation sorts "Bacon bits" before "Bacon (fully cooked)".
        await using var db = Database.CreateDbContext();
        var byName = await db
            .Database.SqlQuery<string>(
                $"SELECT name AS \"Value\" FROM product_types WHERE deleted_at IS NULL ORDER BY name"
            )
            .ToListAsync();
        Assert.True(types.Count > 1);
        Assert.Equal(byName, types.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTypes_WithCategory_ReturnsOnlyThatCategorysTypes()
    {
        var dairy = (await Repository.GetCategoriesAsync()).Single(c => c.Name == "Dairy & Eggs");

        var types = await Repository.GetTypesAsync(dairy.Id);

        Assert.Equal(45, types.Count);
        Assert.Contains("Yogurt", types.Select(t => t.Name));
        Assert.All(types, t => Assert.Equal(dairy.Id, t.CategoryId));
    }

    [Fact]
    public async Task Category_ProductTypes_LoadLazilyWithoutDeletedOnes()
    {
        await SetTypeDeletedAsync("Yogurt", DateTimeOffset.UtcNow);
        try
        {
            await using var db = Database.CreateDbContext();
            var dairy = await db.ProductCategories.SingleAsync(c => c.Name == "Dairy & Eggs");

            Assert.Equal(44, dairy.ProductTypes.Count);
            Assert.DoesNotContain("Yogurt", dairy.ProductTypes.Select(t => t.Name));
        }
        finally
        {
            await SetTypeDeletedAsync("Yogurt", null);
        }
    }

    [Fact]
    public void Category_ProductTypes_OnAHandMadeCategory_IsEmptyAndSettable()
    {
        var category = new ProductCategory();
        Assert.Empty(category.ProductTypes);

        category.ProductTypes = [new ProductType { Name = "Milk" }];

        Assert.Single(category.ProductTypes);
    }

    [Fact]
    public void Entities_HoldTheirValues()
    {
        var now = DateTimeOffset.UtcNow;
        var category = new ProductCategory { CreatedAt = now, UpdatedAt = now, DeletedAt = now };
        var type = new ProductType { CreatedAt = now, UpdatedAt = now, DeletedAt = now };
        var product = new CatalogProduct { CachedAt = now, CreatedAt = now, UpdatedAt = now, DeletedAt = now };

        Assert.Equal(now, category.DeletedAt);
        Assert.Equal(now, type.UpdatedAt);
        Assert.Equal(now, product.CachedAt);
    }

    [Fact]
    public async Task ProductTypeExists_KnownAndUnknownAndDeleted()
    {
        var type = await AnyTypeAsync();
        Assert.True(await Repository.ProductTypeExistsAsync(type.Id));
        Assert.False(await Repository.ProductTypeExistsAsync(Guid.NewGuid()));

        await SetTypeDeletedAsync(type.Name, DateTimeOffset.UtcNow);
        try
        {
            Assert.False(await Repository.ProductTypeExistsAsync(type.Id));
        }
        finally
        {
            await SetTypeDeletedAsync(type.Name, null);
        }
    }

    [Fact]
    public async Task Add_ThenGetByBarcode_ReturnsTheProductWithDefaults()
    {
        var type = await AnyTypeAsync();
        var added = await AddProductAsync("5901234123457", "Milk", type.Id);

        var found = await Repository.GetByBarcodeAsync("5901234123457");

        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(added.Id, found!.Id);
        Assert.Equal("Milk", found.Name);
        Assert.Equal(type.Id, found.ProductTypeId);
        Assert.Equal(CatalogProduct.ManualSource, found.Source);
        Assert.NotEqual(default, found.CachedAt);
    }

    [Fact]
    public async Task GetByBarcode_UnknownOrDeleted_ReturnsNull()
    {
        var product = await AddProductAsync("111");
        Assert.Null(await Repository.GetByBarcodeAsync("222"));

        await using (var db = Database.CreateDbContext())
            await db
                .CatalogProducts.Where(p => p.Id == product.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));

        Assert.Null(await Repository.GetByBarcodeAsync("111"));
    }

    [Fact]
    public async Task Add_DeletedBarcode_RestoresTheProductWithTheNewData()
    {
        var type = await AnyTypeAsync();
        var old = await AddProductAsync("111", "Old", null);
        await using (var db = Database.CreateDbContext())
            await db
                .CatalogProducts.Where(p => p.Id == old.Id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow)
                        .SetProperty(p => p.CachedAt, DateTimeOffset.UtcNow.AddDays(-30))
                );

        var added = new CatalogProduct
        {
            Barcode = "111",
            Name = "New",
            ProductTypeId = type.Id,
            ImageUrl = "https://example.com/n.jpg",
            Source = "other",
        };
        await Repository.AddAsync(added);

        var found = await Repository.GetByBarcodeAsync("111");
        Assert.Equal(old.Id, added.Id);
        Assert.Equal(old.Id, found!.Id);
        Assert.Equal("New", found.Name);
        Assert.Equal(type.Id, found.ProductTypeId);
        Assert.Equal("https://example.com/n.jpg", found.ImageUrl);
        Assert.Equal("other", found.Source);
        Assert.True(found.CachedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        await using var check = Database.CreateDbContext();
        Assert.Equal(1, await check.CatalogProducts.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Add_TakenBarcode_ThrowsDuplicateKeyAndStaysUsable()
    {
        var db = Database.CreateDbContext();
        var repository = new ProductCatalogRepository(db, TimeProvider.System);
        await repository.AddAsync(new CatalogProduct { Barcode = "111", Name = "A" });

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            repository.AddAsync(new CatalogProduct { Barcode = "111", Name = "B" })
        );

        // The failed row must not be retried by the next save on the same context.
        await repository.AddAsync(new CatalogProduct { Barcode = "222", Name = "C" });
        Assert.NotNull(await Repository.GetByBarcodeAsync("222"));
    }

    [Fact]
    public async Task Search_MatchesAnyPartOfTheNameInAnyCase_OrderedByName()
    {
        await AddProductAsync("1", "Oat milk");
        await AddProductAsync("2", "MILK chocolate");
        await AddProductAsync("3", "Bread");

        var found = await Repository.SearchAsync("  Milk ", null, 10);

        Assert.Equal(["MILK chocolate", "Oat milk"], found.Select(p => p.Name));
    }

    [Fact]
    public async Task Search_TreatsLikeWildcardsAsPlainText()
    {
        await AddProductAsync("1", "100% juice");
        await AddProductAsync("2", "a_b");
        await AddProductAsync("3", "axb");
        await AddProductAsync("4", @"back\slash");

        Assert.Equal("100% juice", Assert.Single(await Repository.SearchAsync("100%", null, 10)).Name);
        Assert.Equal("a_b", Assert.Single(await Repository.SearchAsync("a_b", null, 10)).Name);
        Assert.Equal(@"back\slash", Assert.Single(await Repository.SearchAsync(@"k\s", null, 10)).Name);
        Assert.Single(await Repository.SearchAsync("%", null, 10));
    }

    [Fact]
    public async Task Search_ByProductType_AndLimit()
    {
        var type = await AnyTypeAsync();
        await AddProductAsync("1", "A", type.Id);
        await AddProductAsync("2", "B", type.Id);
        await AddProductAsync("3", "C");

        Assert.Equal(["A", "B"], (await Repository.SearchAsync(null, type.Id, 10)).Select(p => p.Name));
        Assert.Equal(3, (await Repository.SearchAsync("  ", null, 10)).Count);
        Assert.Equal(["A"], (await Repository.SearchAsync(null, null, 1)).Select(p => p.Name));
    }
}
