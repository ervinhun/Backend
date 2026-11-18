using System.Security.Claims;
using BusinessLogic.Dtos.Notification;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

/// <summary>The items the app schedules the member's expiry reminders from.</summary>
[ApiController]
[Authorize]
[Route("api/fridges/{fridgeId:guid}/notification-items")]
public class NotificationItemsController(INotificationItemsService notificationItems)
    : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpGet]
    public async Task<ActionResult<NotificationItemsResponse>> GetNotificationItems(
        Guid fridgeId,
        CancellationToken ct
    )
    {
        return await notificationItems.GetAsync(fridgeId, CurrentUserId, ct);
    }
}
