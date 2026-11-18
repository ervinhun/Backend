using BusinessLogic.Dtos.Notification;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

/// <summary>
/// The settings and items the app schedules a member's reminders from. The fridge's recipient scope is applied
/// here, so the app only gets the items the member is to hear about. Non-members get 404.
/// </summary>
public class NotificationItemsService(
    INotificationSettingsRepository settings,
    IItemRepository items,
    IFridgeRepository fridges,
    TimeProvider timeProvider
) : INotificationItemsService
{
    public async Task<NotificationItemsResponse> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException("Fridge not found.");

        var stored = await settings.GetAsync(fridgeId, timeProvider.GetUtcNow(), ct);
        var config = stored?.Settings ?? new FridgeNotificationSettings();
        var picked = stored?.RecipientUserIds ?? [];

        var active = await items.GetForFridgeAsync(fridgeId, ItemStatus.Active, ct: ct);
        var visible = config.RecipientsScope switch
        {
            RecipientsScope.AllMembers => active,
            RecipientsScope.AdderAndOwners => active
                .Where(i => member.IsOwner || i.AddedByUserId == userId)
                .ToList(),
            _ => picked.Contains(userId) ? active : [],
        };

        return new NotificationItemsResponse
        {
            FridgeId = fridgeId,
            WarningDaysBefore = config.WarningDaysBefore,
            DangerDaysBefore = config.DangerDaysBefore,
            NotificationTime = config.NotificationTime,
            Items =
            [
                .. visible.Select(i => new NotificationItemResponse
                {
                    Id = i.Id,
                    ExpiryDate = i.ExpiryDate,
                    SkipWarning = i.ExpiryDate.DayNumber - i.AddedAt.DayNumber <= config.WarningDaysBefore,
                }),
            ],
        };
    }
}
