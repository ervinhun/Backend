using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess.Repositories;

public class TokenRepository(AppDbContext db, TimeProvider timeProvider) : ITokenRepository
{
    public async Task RevokeAccessTokenAsync(RevokedToken token, CancellationToken ct = default)
    {
        // Expired tokens fail lifetime validation anyway, so their rows can go.
        // Cleaning up on each logout keeps the table small without a background job.
        var now = timeProvider.GetUtcNow();
        await db.RevokedTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(ct);

        db.RevokedTokens.Add(token);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Already revoked (e.g. logout sent twice).
        }
    }

    public Task<bool> IsAccessTokenRevokedAsync(Guid jti, CancellationToken ct = default) =>
        db.RevokedTokens.AnyAsync(t => t.Jti == jti, ct);

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    // A single conditional UPDATE, so two requests racing with the same token
    // can't both rotate it: only one sees a row change.
    public async Task<bool> TryRevokeRefreshTokenAsync(Guid id, DateTimeOffset now, CancellationToken ct = default) =>
        await db.RefreshTokens
            .Where(t => t.Id == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct) == 1;

    public Task RevokeRefreshTokenFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct = default) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
}
