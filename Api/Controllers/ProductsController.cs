using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Product;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController(IProductCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductResponse>> Search(
        string? search,
        Guid? productTypeId,
        CancellationToken ct
    ) => await catalog.SearchAsync(search, productTypeId, ct);

    [HttpGet("{barcode}")]
    public async Task<ActionResult<ProductResponse>> GetByBarcode(
        [StringLength(64, MinimumLength = 1)] string barcode,
        CancellationToken ct
    ) => await catalog.GetByBarcodeAsync(barcode, ct);

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken ct
    )
    {
        var product = await catalog.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByBarcode), new { barcode = product.Barcode }, product);
    }
}
