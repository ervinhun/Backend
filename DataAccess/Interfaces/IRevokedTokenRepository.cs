using DataAccess.Entities;

namespace DataAccess.Interfaces;

public interface IRevokedTokenRepository
{
    /// <summary>Records a signed-out token. Revoking the same token twice is a no-op.</summary>
    Task RevokeAsync(RevokedToken token, CancellationToken ct = default);

    Task<bool> IsRevokedAsync(Guid jti, CancellationToken ct = default);
}
