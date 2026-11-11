using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Models.Notification;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class NotificationSettingsRepository(AppDbContext db) : INotificationSettingsRepository
{
    public async Task<NotificationSettingsWithRecipients?> GetAsync(
        Guid fridgeId,
        CancellationToken ct = default
    )
    {
        var settings = await db
            .NotificationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.FridgeId == fridgeId, ct);
        if (settings is null)
            return null;

        // Members who left are not recipients any more, even if their row is still there.
        var recipients = await db
            .NotificationRecipients.AsNoTracking()
            .Where(r =>
                r.FridgeId == fridgeId
                && db.FridgeMembers.Any(m => m.FridgeId == fridgeId && m.UserId == r.UserId)
            )
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Select(r => r.UserId)
            .ToListAsync(ct);

        return new NotificationSettingsWithRecipients(settings, recipients);
    }

    public async Task SaveAsync(
        Guid fridgeId,
        int warningDaysBefore,
        int dangerDaysBefore,
        RecipientsScope scope,
        IReadOnlyCollection<Guid> recipientUserIds,
        DateTimeOffset now,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var settings = await db.NotificationSettings.FirstOrDefaultAsync(
            s => s.FridgeId == fridgeId,
            ct
        );
        if (settings is null)
        {
            settings = new FridgeNotificationSettings { FridgeId = fridgeId };
            db.NotificationSettings.Add(settings);
        }
        settings.WarningDaysBefore = warningDaysBefore;
        settings.DangerDaysBefore = dangerDaysBefore;
        settings.RecipientsScope = scope;

        var existing = await db
            .NotificationRecipients.Where(r => r.FridgeId == fridgeId)
            .ToListAsync(ct);
        foreach (var recipient in existing.Where(r => !recipientUserIds.Contains(r.UserId)))
            recipient.DeletedAt = now;
        foreach (var userId in recipientUserIds.Except(existing.Select(r => r.UserId)))
            db.NotificationRecipients.Add(
                new FridgeNotificationRecipient { FridgeId = fridgeId, UserId = userId }
            );

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
