using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class UserDeviceRepository(AppDbContext db) : IUserDeviceRepository
{
    public async Task<UserDevice> AddAsync(UserDevice device, CancellationToken ct = default)
    {
        db.UserDevices.Add(device);
        await db.SaveChangesAsync(ct);
        return device;
    }

    public Task<UserDevice?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.UserDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeletedAt == null && d.RefreshTokenHash == tokenHash, ct);

    // A single conditional UPDATE, so two requests racing with the same token
    // can't both rotate it: only one sees a row change.
    public async Task<bool> TryRotateRefreshTokenAsync(
        Guid deviceId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default) =>
        await db.UserDevices
            .Where(d => d.Id == deviceId && d.DeletedAt == null && d.RefreshTokenHash == currentHash)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.RefreshTokenHash, newHash)
                .SetProperty(d => d.RefreshTokenExpiresAt, expiresAt)
                .SetProperty(d => d.LastSeenAt, now), ct) == 1;

    public Task<bool> IsSignedInAsync(Guid deviceId, Guid userId, CancellationToken ct = default) =>
        db.UserDevices.AnyAsync(d => d.Id == deviceId && d.UserId == userId && d.DeletedAt == null, ct);

    public Task SignOutAsync(Guid deviceId, DateTimeOffset now, CancellationToken ct = default) =>
        db.UserDevices
            .Where(d => d.Id == deviceId && d.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.DeletedAt, now)
                .SetProperty(d => d.IsActive, false)
                .SetProperty(d => d.RefreshTokenHash, (string?)null)
                .SetProperty(d => d.RefreshTokenExpiresAt, (DateTimeOffset?)null), ct);
}
