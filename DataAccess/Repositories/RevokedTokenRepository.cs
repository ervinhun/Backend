using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess.Repositories;

public class RevokedTokenRepository(AppDbContext db, TimeProvider timeProvider) : IRevokedTokenRepository
{
    public async Task RevokeAsync(RevokedToken token, CancellationToken ct = default)
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

    public Task<bool> IsRevokedAsync(Guid jti, CancellationToken ct = default) =>
        db.RevokedTokens.AnyAsync(t => t.Jti == jti, ct);
}
