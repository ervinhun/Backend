using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryRevokedTokenRepository : IRevokedTokenRepository
{
    private readonly ConcurrentDictionary<Guid, RevokedToken> _tokens = new();

    public IReadOnlyCollection<RevokedToken> All => _tokens.Values.ToList();

    public Task RevokeAsync(RevokedToken token, CancellationToken ct = default)
    {
        _tokens.TryAdd(token.Jti, token);
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(Guid jti, CancellationToken ct = default) =>
        Task.FromResult(_tokens.ContainsKey(jti));
}
