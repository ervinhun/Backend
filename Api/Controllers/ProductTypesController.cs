using BusinessLogic.Dtos.Product;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/product-types")]
public class ProductTypesController(IProductCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductTypeResponse>> GetAll(
        Guid? categoryId,
        CancellationToken ct
    ) => await catalog.GetTypesAsync(categoryId, ct);
}
