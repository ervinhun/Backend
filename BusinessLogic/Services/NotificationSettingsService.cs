using BusinessLogic.Dtos.Notification;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

/// <summary>
/// Every member of the fridge can see its notification settings; only the owner can change them.
/// A fridge that never saved any gets the defaults the database would have given it.
/// </summary>
public class NotificationSettingsService(
    INotificationSettingsRepository settings,
    IFridgeRepository fridges,
    TimeProvider timeProvider
) : INotificationSettingsService
{
    private const string FridgeNotFoundMessage = "Fridge not found.";

    public async Task<NotificationSettingsResponse> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: false, ct);

        var stored = await settings.GetAsync(fridgeId, ct);
        return stored is null ? Defaults(fridgeId) : stored.ToResponse();
    }

    public async Task<NotificationSettingsResponse> UpdateAsync(
        Guid fridgeId,
        Guid userId,
        SaveNotificationSettingsRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: true, ct);

        var scope = request.ToScope();
        // Only a custom scope keeps recipients; switching away from it clears them.
        var recipients =
            scope == RecipientsScope.Custom ? request.RecipientUserIds.Distinct().ToList() : [];

        if (recipients.Count > 0)
        {
            var memberIds = (await fridges.GetMembersAsync(fridgeId, ct))
                .Select(m => m.UserId)
                .ToHashSet();
            if (recipients.Any(id => !memberIds.Contains(id)))
                throw new ConflictException("Every recipient must be a member of this fridge.");
        }

        await settings.SaveAsync(
            fridgeId,
            request.WarningDaysBefore,
            request.DangerDaysBefore,
            scope,
            recipients,
            timeProvider.GetUtcNow(),
            ct
        );

        return (await settings.GetAsync(fridgeId, ct))!.ToResponse();
    }

    private static NotificationSettingsResponse Defaults(Guid fridgeId)
    {
        var defaults = new FridgeNotificationSettings();
        return new NotificationSettingsResponse
        {
            FridgeId = fridgeId,
            WarningDaysBefore = defaults.WarningDaysBefore,
            DangerDaysBefore = defaults.DangerDaysBefore,
            RecipientsScope = defaults.RecipientsScope.ToWireName(),
        };
    }

    // Non-members get 404 so the existence of a fridge is not leaked.
    private async Task AuthorizeAsync(Guid fridgeId, Guid userId, bool write, CancellationToken ct)
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(FridgeNotFoundMessage);
        if (write && !member.IsOwner)
            throw new ForbiddenException("Only the fridge owner can change its notification settings.");
    }
}
