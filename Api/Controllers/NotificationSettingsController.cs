using System.Security.Claims;
using BusinessLogic.Dtos.Notification;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

/// <summary>Every member of the fridge can see its notification settings; only the owner can change them.</summary>
[ApiController]
[Authorize]
[Route("api/fridges/{fridgeId:guid}/notification-settings")]
public class NotificationSettingsController(INotificationSettingsService notificationSettings)
    : ControllerBase
{
    // AppJwtBearerEvents rejects any token whose sub isn't a GUID, so this can't fail behind [Authorize].
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpGet]
    public async Task<ActionResult<NotificationSettingsResponse>> Get(
        Guid fridgeId,
        CancellationToken ct
    )
    {
        return await notificationSettings.GetAsync(fridgeId, CurrentUserId, ct);
    }

    [HttpPut]
    public async Task<ActionResult<NotificationSettingsResponse>> Update(
        Guid fridgeId,
        SaveNotificationSettingsRequest request,
        CancellationToken ct
    )
    {
        return await notificationSettings.UpdateAsync(fridgeId, CurrentUserId, request, ct);
    }
}
