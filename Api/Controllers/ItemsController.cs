using System.Security.Claims;
using BusinessLogic.Dtos.Item;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

/// <summary>Every member of the fridge can see its items; the owner and members who may put things in can change them.</summary>
[ApiController]
[Authorize]
[Route("api/fridges/{fridgeId:guid}/items")]
public class ItemsController(IItemService items) : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<ActionResult<ItemResponse>> Create(
        Guid fridgeId,
        CreateItemRequest request,
        CancellationToken ct
    )
    {
        var item = await items.CreateAsync(fridgeId, CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { fridgeId, itemId = item.Id }, item);
    }

    /// <summary>The fridge's items that are still active unless a status is given, soonest to expire first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemResponse>>> GetAll(
        Guid fridgeId,
        Guid? containerId,
        string? status,
        CancellationToken ct
    )
    {
        return Ok(await items.GetForFridgeAsync(fridgeId, CurrentUserId, containerId, status, ct));
    }

    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<ItemResponse>> Get(
        Guid fridgeId,
        Guid itemId,
        CancellationToken ct
    )
    {
        return await items.GetAsync(fridgeId, itemId, CurrentUserId, ct);
    }

    [HttpPut("{itemId:guid}")]
    public async Task<ActionResult<ItemResponse>> Update(
        Guid fridgeId,
        Guid itemId,
        UpdateItemRequest request,
        CancellationToken ct
    )
    {
        return await items.UpdateAsync(fridgeId, itemId, CurrentUserId, request, ct);
    }

    /// <summary>Marks the item consumed or removed, or active again. Members who may use things can do this.</summary>
    [HttpPut("{itemId:guid}/status")]
    public async Task<ActionResult<ItemResponse>> SetStatus(
        Guid fridgeId,
        Guid itemId,
        SetItemStatusRequest request,
        CancellationToken ct
    )
    {
        return await items.SetStatusAsync(fridgeId, itemId, CurrentUserId, request, ct);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid fridgeId, Guid itemId, CancellationToken ct)
    {
        await items.DeleteAsync(fridgeId, itemId, CurrentUserId, ct);
        return NoContent();
    }
}
