using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryTokenRepository : ITokenRepository
{
    private readonly ConcurrentDictionary<Guid, RevokedToken> _revoked = new();
    private readonly ConcurrentDictionary<Guid, RefreshToken> _refresh = new();
    private readonly object _lock = new();

    public IReadOnlyCollection<RevokedToken> RevokedAccessTokens => _revoked.Values.ToList();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refresh.Values.ToList();

    public Task RevokeAccessTokenAsync(RevokedToken token, CancellationToken ct = default)
    {
        _revoked.TryAdd(token.Jti, token);
        return Task.CompletedTask;
    }

    public Task<bool> IsAccessTokenRevokedAsync(Guid jti, CancellationToken ct = default) =>
        Task.FromResult(_revoked.ContainsKey(jti));

    public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        token.Id = Guid.NewGuid();
        _refresh[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(_refresh.Values.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<bool> TryRevokeRefreshTokenAsync(Guid id, DateTimeOffset now, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_refresh.TryGetValue(id, out var token) || token.RevokedAt is not null)
                return Task.FromResult(false);
            token.RevokedAt = now;
            return Task.FromResult(true);
        }
    }

    public Task RevokeRefreshTokenFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct = default)
    {
        lock (_lock)
        {
            foreach (var token in _refresh.Values.Where(t => t.FamilyId == familyId && t.RevokedAt is null))
                token.RevokedAt = now;
        }
        return Task.CompletedTask;
    }
}
