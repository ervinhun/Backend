using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>Signed-out access tokens (revoked_tokens) and refresh tokens (refresh_tokens).</summary>
public interface ITokenRepository
{
    /// <summary>Records a signed-out access token. Revoking the same token twice is a no-op.</summary>
    Task RevokeAccessTokenAsync(RevokedToken token, CancellationToken ct = default);

    Task<bool> IsAccessTokenRevokedAsync(Guid jti, CancellationToken ct = default);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);

    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Revokes one refresh token if it is still active. Returns false if it was already
    /// revoked — including by a concurrent request that used it first.
    /// </summary>
    Task<bool> TryRevokeRefreshTokenAsync(Guid id, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>Revokes every still-active refresh token of one sign-in.</summary>
    Task RevokeRefreshTokenFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct = default);
}
