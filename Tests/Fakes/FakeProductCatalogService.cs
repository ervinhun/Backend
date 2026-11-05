using BusinessLogic.Dtos.Product;
using BusinessLogic.Interfaces;

namespace Tests.Fakes;

public class FakeProductCatalogService : IProductCatalogService
{
    public List<string> Calls { get; } = [];

    /// <summary>The token the last recorded call was given.</summary>
    public CancellationToken LastToken { get; private set; }

    public Guid? LastCategoryId { get; private set; }
    public string? LastSearch { get; private set; }
    public Guid? LastProductTypeId { get; private set; }

    public Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(
        CancellationToken ct = default
    )
    {
        Record(nameof(GetCategoriesAsync), ct);
        return Task.FromResult<IReadOnlyList<ProductCategoryResponse>>(
            [new ProductCategoryResponse { Id = Guid.NewGuid(), Name = "Dairy" }]
        );
    }

    public Task<IReadOnlyList<ProductTypeResponse>> GetTypesAsync(
        Guid? categoryId,
        CancellationToken ct = default
    )
    {
        Record(nameof(GetTypesAsync), ct);
        LastCategoryId = categoryId;
        return Task.FromResult<IReadOnlyList<ProductTypeResponse>>(
            [new ProductTypeResponse { Id = Guid.NewGuid(), Name = "Milk", DefaultExpiryDays = 7 }]
        );
    }

    public Task<ProductResponse> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        Record(nameof(GetByBarcodeAsync), ct);
        return Task.FromResult(Product(barcode, "Found"));
    }

    public Task<IReadOnlyList<ProductResponse>> SearchAsync(
        string? search,
        Guid? productTypeId,
        CancellationToken ct = default
    )
    {
        Record(nameof(SearchAsync), ct);
        LastSearch = search;
        LastProductTypeId = productTypeId;
        return Task.FromResult<IReadOnlyList<ProductResponse>>([Product("123", "Found")]);
    }

    public Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken ct = default
    )
    {
        Record(nameof(CreateAsync), ct);
        return Task.FromResult(Product(request.Barcode, request.Name));
    }

    private void Record(string call, CancellationToken ct)
    {
        Calls.Add(call);
        LastToken = ct;
    }

    private static ProductResponse Product(string barcode, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Name = name,
            Source = "manual",
        };
}
