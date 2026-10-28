using System.Security.Claims;
using Api.RateLimiting;
using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fridges")]
public class FridgesController(IFridgeService fridges) : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<ActionResult<FridgeResponse>> Create(
        SaveFridgeRequest request,
        CancellationToken ct
    )
    {
        var fridge = await fridges.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = fridge.Id }, fridge);
    }

    [HttpGet]
    public async Task<IReadOnlyList<FridgeResponse>> GetMine(CancellationToken ct) =>
        await fridges.GetForUserAsync(CurrentUserId, ct);

    [HttpPost(nameof(Join))]
    [EnableRateLimiting(RateLimitPolicies.InviteCode)]
    public async Task<ActionResult<FridgeResponse>> Join(
        JoinFridgeRequest request,
        CancellationToken ct
    ) => await fridges.JoinAsync(CurrentUserId, request, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FridgeResponse>> Get(Guid id, CancellationToken ct)
        => await fridges.GetAsync(id, CurrentUserId, ct);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FridgeResponse>> Update(
        Guid id,
        SaveFridgeRequest request,
        CancellationToken ct
    )
        => await fridges.UpdateAsync(id, CurrentUserId, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await fridges.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/invite-code")]
    [EnableRateLimiting(RateLimitPolicies.InviteCode)]
    public async Task<ActionResult<FridgeResponse>> RegenerateInviteCode(
        Guid id,
        CancellationToken ct
    )
        => await fridges.RegenerateInviteCodeAsync(id, CurrentUserId, ct);

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<FridgeMemberResponse>>> GetMembers(
        Guid id,
        CancellationToken ct
    )
    {
        return Ok(await fridges.GetMembersAsync(id, CurrentUserId, ct));
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult<FridgeMemberResponse>> UpdateMemberRights(
        Guid id,
        Guid userId,
        UpdateMemberRightsRequest request,
        CancellationToken ct
    )
        => await fridges.UpdateMemberRightsAsync(id, CurrentUserId, userId, request, ct);

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        await fridges.RemoveMemberAsync(id, CurrentUserId, userId, ct);
        return NoContent();
    }
}
