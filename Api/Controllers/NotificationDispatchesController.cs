using System.Security.Claims;
using BusinessLogic.Dtos.Notification;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

/// <summary>The app reports the notifications it showed, so the backend knows what already went out.</summary>
[ApiController]
[Authorize]
[Route("api/fridges/{fridgeId:guid}/notification-dispatches")]
public class NotificationDispatchesController(INotificationDispatchService dispatches)
    : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> ReportDispatches(
        Guid fridgeId,
        ReportDispatchesRequest request,
        CancellationToken ct
    )
    {
        await dispatches.ReportAsync(fridgeId, CurrentUserId, request, ct);
        return NoContent();
    }
}
