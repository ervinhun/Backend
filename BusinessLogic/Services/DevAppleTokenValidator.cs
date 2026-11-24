using BusinessLogic.Dtos.Auth;
using BusinessLogic.Interfaces;
using DataAccess.Entities;

namespace BusinessLogic.Services;

/// <summary>
/// Development only: accepts <see cref="Token"/> as an Apple ID token so the iOS simulator can sign in
/// without a paid Apple developer account. Everything else goes to the real validator.
/// Program.cs registers this only when the environment is Development.
/// </summary>
public class DevAppleTokenValidator(IExternalTokenValidator real) : IExternalTokenValidator
{
    public const string Token = "fake-apple-token";

    public Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken ct = default) =>
        provider == AuthProviders.Apple && idToken == Token
            ? Task.FromResult(new ExternalIdentity(AuthProviders.Apple, "fake-apple-user", "fake.apple@example.com", true, "Fake Apple User"))
            : real.ValidateAsync(provider, idToken, ct);
}
