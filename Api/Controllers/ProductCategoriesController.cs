using BusinessLogic.Dtos.Product;
using BusinessLogic.Interfaces;
using Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
// Reference data the app needs before anyone signs in, e.g. for guests.
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Catalog)]
[Route("api/product-categories")]
public class ProductCategoriesController(IProductCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductCategoryResponse>> GetProductCategories(CancellationToken ct) =>
        await catalog.GetCategoriesAsync(ct);
}
