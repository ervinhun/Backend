using BusinessLogic.Dtos.Auth;

namespace BusinessLogic.Interfaces;

public interface IExternalTokenValidator
{
    /// <summary>
    /// Verifies a Google or Apple ID token (signature, issuer, audience, lifetime).
    /// Throws <see cref="Exceptions.UnauthorizedException"/> if it is not valid.
    /// </summary>
    Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken ct = default);
}
