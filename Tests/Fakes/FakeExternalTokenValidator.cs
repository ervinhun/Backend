using System.Collections.Concurrent;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;

namespace Tests.Fakes;

/// <summary>Treats each registered string as a valid ID token for the given identity.</summary>
public class FakeExternalTokenValidator : IExternalTokenValidator
{
    private readonly ConcurrentDictionary<string, ExternalIdentity> _tokens = new();

    public string Issue(ExternalIdentity identity)
    {
        var token = $"fake-{Guid.NewGuid():N}";
        _tokens[token] = identity;
        return token;
    }

    public Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken ct = default) =>
        _tokens.TryGetValue(idToken, out var identity) && identity.Provider == provider
            ? Task.FromResult(identity)
            : throw new UnauthorizedException("Invalid sign-in token.");
}
