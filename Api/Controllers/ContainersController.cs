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
public class ContainersController(IContainerService containers)
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
        var container = await containers.CreateAsync(fridgeId, CurrentUserId, request, ct);
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
        return Ok(await containers.GetForFridgeAsync(fridgeId, CurrentUserId, ct));
    }

    [HttpGet("{containerId:guid}")]
    public async Task<ActionResult<ContainerResponse>> Get(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct
    )
    {
        return await containers.GetAsync(fridgeId, containerId, CurrentUserId, ct);
    }

    [HttpPut("{containerId:guid}")]
    public async Task<ActionResult<ContainerResponse>> Update(
        Guid fridgeId,
        Guid containerId,
        SaveContainerRequest request,
        CancellationToken ct
    )
    {
        return await containers.UpdateAsync(fridgeId, containerId, CurrentUserId, request, ct);
    }

    [HttpDelete("{containerId:guid}")]
    public async Task<IActionResult> Delete(Guid fridgeId, Guid containerId, CancellationToken ct)
    {
        await containers.DeleteAsync(fridgeId, containerId, CurrentUserId, ct);
        return NoContent();
    }
}
