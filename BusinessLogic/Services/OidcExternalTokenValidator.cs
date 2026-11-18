using System.Text.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Options;
using DataAccess.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLogic.Services;

/// <summary>
/// Validates Google and Apple ID tokens against each provider's published signing keys.
/// The keys come from the provider's OpenID discovery document and are cached and
/// refreshed by <see cref="ConfigurationManager{T}"/>, so this should be a singleton.
/// </summary>
public class OidcExternalTokenValidator : IExternalTokenValidator
{
    public record Provider(
        string[] Issuers,
        string[] Audiences,
        IConfigurationManager<OpenIdConnectConfiguration> Configuration);

    private const string InvalidTokenMessage = "Invalid sign-in token.";

    private readonly IReadOnlyDictionary<string, Provider> _providers;
    private readonly JsonWebTokenHandler _handler = new();

    public OidcExternalTokenValidator(IOptions<ExternalAuthOptions> options)
        : this(new Dictionary<string, Provider>
        {
            [AuthProviders.Google] = new(
                ["https://accounts.google.com", "accounts.google.com"],
                ClientIds(options.Value.Google),
                Discovery("https://accounts.google.com/.well-known/openid-configuration")),
            [AuthProviders.Apple] = new(
                ["https://appleid.apple.com"],
                ClientIds(options.Value.Apple),
                Discovery("https://appleid.apple.com/.well-known/openid-configuration")),
        })
    {
    }

    /// <summary>For tests: supply providers with fixed signing keys.</summary>
    public OidcExternalTokenValidator(IReadOnlyDictionary<string, Provider> providers) => _providers = providers;

    public async Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken ct = default)
    {
        if (!_providers.TryGetValue(provider, out var config) || config.Audiences.Length == 0)
            throw new UnauthorizedException($"Sign-in with '{provider}' is not enabled.");

        var result = await ValidateAsync(config, idToken, ct);

        // The provider rotated its keys since they were cached: refetch once and retry.
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            config.Configuration.RequestRefresh();
            result = await ValidateAsync(config, idToken, ct);
        }

        if (!result.IsValid)
            throw new UnauthorizedException(InvalidTokenMessage);

        var sub = GetString(result, JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(sub))
            throw new UnauthorizedException(InvalidTokenMessage);

        return new ExternalIdentity(
            provider,
            sub,
            GetString(result, JwtRegisteredClaimNames.Email),
            IsTrue(GetClaim(result, "email_verified")),
            GetString(result, JwtRegisteredClaimNames.Name));
    }

    private async Task<TokenValidationResult> ValidateAsync(Provider provider, string idToken, CancellationToken ct)
    {
        var discovery = await provider.Configuration.GetConfigurationAsync(ct);
        return await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = provider.Issuers,
            ValidAudiences = provider.Audiences,
            IssuerSigningKeys = discovery.SigningKeys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        });
    }

    // Blank entries (e.g. an unset env var) must not count as an accepted audience.
    private static string[] ClientIds(ExternalProviderOptions provider) =>
        provider.ClientIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();

    private static IConfigurationManager<OpenIdConnectConfiguration> Discovery(string url) =>
        new ConfigurationManager<OpenIdConnectConfiguration>(
            url, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });

    private static object? GetClaim(TokenValidationResult result, string claim) =>
        result.Claims.TryGetValue(claim, out var value) ? value : null;

    private static string? GetString(TokenValidationResult result, string claim) =>
        GetClaim(result, claim)?.ToString();

    // Google sends email_verified as a JSON boolean, Apple as the string "true".
    private static bool IsTrue(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var b) && b,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.String } e => bool.TryParse(e.GetString(), out var b) && b,
        _ => false,
    };
}
