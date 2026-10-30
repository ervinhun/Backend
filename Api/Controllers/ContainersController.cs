using System.Security.Claims;
using BusinessLogic.Dtos.Container;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

/// <summary>Every member of the fridge can see its containers; the owner and members who may put things in can change them.</summary>
[ApiController]
[Authorize]
[Route("api/fridges/{fridgeId:guid}/containers")]
public class ContainersController(IFridgeService fridges, IContainerService containers)
    : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<ActionResult<ContainerResponse>> Create(
        Guid fridgeId,
        SaveContainerRequest request,
        CancellationToken ct
    )
    {
        var error = await AuthorizeAsync(fridgeId, write: true, ct);
        if (error is not null)
            return error;

        var container = await containers.CreateAsync(fridgeId, request, ct);
        return CreatedAtAction(
            nameof(Get),
            new { fridgeId, containerId = container.Id },
            container
        );
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContainerResponse>>> GetAll(
        Guid fridgeId,
        CancellationToken ct
    )
    {
        var error = await AuthorizeAsync(fridgeId, write: false, ct);
        if (error is not null)
            return error;

        return Ok(await containers.GetForFridgeAsync(fridgeId, ct));
    }

    [HttpGet("{containerId:guid}")]
    public async Task<ActionResult<ContainerResponse>> Get(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct
    )
    {
        var error = await AuthorizeAsync(fridgeId, write: false, ct);
        if (error is not null)
            return error;

        return await containers.GetAsync(fridgeId, containerId, ct);
    }

    [HttpPut("{containerId:guid}")]
    public async Task<ActionResult<ContainerResponse>> Update(
        Guid fridgeId,
        Guid containerId,
        SaveContainerRequest request,
        CancellationToken ct
    )
    {
        var error = await AuthorizeAsync(fridgeId, write: true, ct);
        if (error is not null)
            return error;

        return await containers.UpdateAsync(fridgeId, containerId, request, ct);
    }

    [HttpDelete("{containerId:guid}")]
    public async Task<IActionResult> Delete(Guid fridgeId, Guid containerId, CancellationToken ct)
    {
        var error = await AuthorizeAsync(fridgeId, write: true, ct);
        if (error is not null)
            return error;

        await containers.DeleteAsync(fridgeId, containerId, ct);
        return NoContent();
    }

    // Non-members get 404 so the existence of a fridge is not leaked.
    private async Task<ActionResult?> AuthorizeAsync(
        Guid fridgeId,
        bool write,
        CancellationToken ct
    )
    {
        var member = await fridges.GetMemberAsync(fridgeId, CurrentUserId, ct);
        if (member is null)
            return NotFound();
        if (write && !member.IsOwner && !member.CanPut)
            return StatusCode(StatusCodes.Status403Forbidden);

        return null;
    }
}
