using System.Security.Claims;
using System.Security.Cryptography;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Tests.BusinessLogic;

public class OidcExternalTokenValidatorTests
{
    private const string Issuer = "https://appleid.apple.com";
    private const string Audience = "com.fridgefriend.app";

    private static readonly RsaSecurityKey ProviderKey = new(RSA.Create(2048)) { KeyId = "provider-key" };

    private static OidcExternalTokenValidator Validator(string[]? audiences = null)
    {
        var discovery = new OpenIdConnectConfiguration();
        discovery.SigningKeys.Add(ProviderKey);
        return new OidcExternalTokenValidator(new Dictionary<string, OidcExternalTokenValidator.Provider>
        {
            [AuthProviders.Apple] = new(
                [Issuer], audiences ?? [Audience],
                new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery)),
        });
    }

    private static string Token(
        SecurityKey? key = null, string audience = Audience, DateTime? expires = null, params Claim[] extra) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(10),
            Subject = new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, "apple-sub-1"), .. extra]),
            SigningCredentials = new SigningCredentials(key ?? ProviderKey, SecurityAlgorithms.RsaSha256),
        });

    [Fact]
    public async Task ValidToken_ReturnsIdentity_AndParsesAppleStringEmailVerified()
    {
        var token = Token(extra:
        [
            new Claim(JwtRegisteredClaimNames.Email, "anna@privaterelay.appleid.com"),
            new Claim("email_verified", "true"),
        ]);

        var identity = await Validator().ValidateAsync(AuthProviders.Apple, token);

        Assert.Equal(AuthProviders.Apple, identity.Provider);
        Assert.Equal("apple-sub-1", identity.ProviderUserId);
        Assert.Equal("anna@privaterelay.appleid.com", identity.Email);
        Assert.True(identity.EmailVerified);
    }

    [Fact]
    public async Task GoogleStyleBooleanEmailVerified_IsParsed()
    {
        var token = Token(extra: new Claim("email_verified", "true", ClaimValueTypes.Boolean));

        Assert.True((await Validator().ValidateAsync(AuthProviders.Apple, token)).EmailVerified);
    }

    public static TheoryData<string> InvalidTokens => new()
    {
        Token(key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "provider-key" }), // forged signature
        Token(audience: "some.other.app"),
        Token(expires: DateTime.UtcNow.AddMinutes(-10)),
        "not-a-jwt",
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task InvalidToken_ThrowsUnauthorized(string token)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Validator().ValidateAsync(AuthProviders.Apple, token));
    }

    [Fact]
    public async Task ProviderWithoutClientIds_IsDisabled()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Validator(audiences: []).ValidateAsync(AuthProviders.Apple, Token()));
    }

    [Fact]
    public async Task UnknownProvider_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Validator().ValidateAsync(AuthProviders.Google, Token()));
    }
}
