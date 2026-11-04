using BusinessLogic.Dtos.Product;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/product-categories")]
public class ProductCategoriesController(IProductCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductCategoryResponse>> GetAll(CancellationToken ct) =>
        await catalog.GetCategoriesAsync(ct);
}
