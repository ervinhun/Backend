using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>Signed-in sessions (user_sessions) and their refresh tokens.</summary>
public interface IUserSessionRepository
{
    Task<UserSession> AddAsync(UserSession session, CancellationToken ct = default);

    /// <summary>Finds a signed-in session whose refresh token has this hash.</summary>
    Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Replaces the session's refresh token if it still holds <paramref name="currentHash"/>.
    /// Returns false if not — including when a concurrent request rotated it first.
    /// </summary>
    Task<bool> TryRotateRefreshTokenAsync(
        Guid sessionId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default);

    Task<bool> IsSignedInAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    /// <summary>Soft-deletes the session and clears its refresh token. Signing out twice is a no-op.</summary>
    Task SignOutAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct = default);
}
