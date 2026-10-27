using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class UserSessionRepository(AppDbContext db) : IUserSessionRepository
{
    public async Task<UserSession> AddAsync(UserSession session, CancellationToken ct = default)
    {
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.UserSessions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeletedAt == null && d.RefreshTokenHash == tokenHash, ct);

    // A single conditional UPDATE, so two requests racing with the same token
    // can't both rotate it: only one sees a row change.
    public async Task<bool> TryRotateRefreshTokenAsync(
        Guid sessionId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default) =>
        await db.UserSessions
            .Where(d => d.Id == sessionId && d.DeletedAt == null && d.RefreshTokenHash == currentHash)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.RefreshTokenHash, newHash)
                .SetProperty(d => d.RefreshTokenExpiresAt, expiresAt)
                .SetProperty(d => d.LastSeenAt, now), ct) == 1;

    public Task<bool> IsSignedInAsync(Guid sessionId, Guid userId, CancellationToken ct = default) =>
        db.UserSessions.AnyAsync(d => d.Id == sessionId && d.UserId == userId && d.DeletedAt == null, ct);

    public Task SignOutAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct = default) =>
        db.UserSessions
            .Where(d => d.Id == sessionId && d.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.DeletedAt, now)
                .SetProperty(d => d.IsActive, false)
                .SetProperty(d => d.RefreshTokenHash, (string?)null)
                .SetProperty(d => d.RefreshTokenExpiresAt, (DateTimeOffset?)null), ct);
}
