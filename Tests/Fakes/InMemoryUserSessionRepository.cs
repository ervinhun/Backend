using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryUserSessionRepository : IUserSessionRepository
{
    private readonly ConcurrentDictionary<Guid, UserSession> _sessions = new();
    private readonly object _lock = new();

    public IReadOnlyCollection<UserSession> All => _sessions.Values.ToList();

    public Task<UserSession> AddAsync(UserSession session, CancellationToken ct = default)
    {
        session.Id = Guid.NewGuid();
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(_sessions.Values.FirstOrDefault(d =>
            d.DeletedAt is null && d.RefreshTokenHash == tokenHash));

    public Task<bool> TryRotateRefreshTokenAsync(
        Guid sessionId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var d) || d.DeletedAt is not null || d.RefreshTokenHash != currentHash)
                return Task.FromResult(false);
            d.RefreshTokenHash = newHash;
            d.RefreshTokenExpiresAt = expiresAt;
            d.LastSeenAt = now;
            return Task.FromResult(true);
        }
    }

    public Task<bool> IsSignedInAsync(Guid sessionId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var d) && d.UserId == userId && d.DeletedAt is null);

    public Task SignOutAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var d) && d.DeletedAt is null)
            {
                d.DeletedAt = now;
                d.IsActive = false;
                d.RefreshTokenHash = null;
                d.RefreshTokenExpiresAt = null;
            }
        }
        return Task.CompletedTask;
    }
}
