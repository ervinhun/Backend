using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLogic.Constants;
using BusinessLogic.Interfaces;
using BusinessLogic.Options;
using DataAccess.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLogic.Services;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, Guid sessionId)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DefaultDisplayName),
                new Claim(AppClaimTypes.Locale, user.Locale),
                new Claim(AppClaimTypes.AuthProvider, user.AuthProvider),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options.Secret), SecurityAlgorithms.HmacSha256),
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }

    public (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken()
    {
        // 256 bits of randomness; opaque to the client, unlike the JWT access token.
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (token, HashRefreshToken(token), timeProvider.GetUtcNow().AddDays(_options.RefreshTokenDays));
    }

    // A plain hash is enough (no salt/stretching): the token is random, not a guessable password.
    public string HashRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static SymmetricSecurityKey CreateSigningKey(string secret) =>
        new(Encoding.UTF8.GetBytes(secret));
}
