using Api.Controllers;
using BusinessLogic.Dtos.Product;
using Microsoft.AspNetCore.Mvc;
using Tests.Fakes;

namespace Tests.Api;

public class ProductControllersTests
{
    private readonly FakeProductCatalogService _service = new();

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedPointingAtGetByBarcode()
    {
        var result = await new ProductsController(_service).Create(
            new CreateProductRequest { Barcode = "5901234123457", Name = "Milk" },
            CancellationToken.None
        );

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal(nameof(ProductsController.GetByBarcode), created.ActionName);
        Assert.Equal("5901234123457", created.RouteValues!["barcode"]);
        Assert.Equal("Milk", product.Name);
    }

    [Fact]
    public async Task Search_PassesTheFiltersOn()
    {
        var typeId = Guid.NewGuid();

        var result = await new ProductsController(_service).Search(
            "milk",
            typeId,
            CancellationToken.None
        );

        Assert.Single(result);
        Assert.Equal("milk", _service.LastSearch);
        Assert.Equal(typeId, _service.LastProductTypeId);
    }

    [Fact]
    public async Task GetByBarcode_ReturnsTheProduct()
    {
        var result = await new ProductsController(_service).GetByBarcode(
            "123",
            CancellationToken.None
        );

        Assert.Equal("123", result.Value!.Barcode);
    }

    [Fact]
    public async Task ProductTypes_GetAll_PassesTheCategoryOn()
    {
        var categoryId = Guid.NewGuid();

        var result = await new ProductTypesController(_service).GetAll(
            categoryId,
            CancellationToken.None
        );

        Assert.Single(result);
        Assert.Equal(categoryId, _service.LastCategoryId);
    }

    [Fact]
    public async Task ProductCategories_GetAll_ReturnsTheCategories()
    {
        var result = await new ProductCategoriesController(_service).GetAll(CancellationToken.None);

        Assert.Equal("Dairy", Assert.Single(result).Name);
    }

    [Fact]
    public async Task Actions_PassTheRequestsCancellationTokenOn()
    {
        using var cts = new CancellationTokenSource();
        var products = new ProductsController(_service);

        await products.Search(null, null, cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
        await products.GetByBarcode("1", cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
        await products.Create(new CreateProductRequest { Barcode = "1", Name = "A" }, cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
        await new ProductTypesController(_service).GetAll(null, cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
        await new ProductCategoriesController(_service).GetAll(cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
    }
}
