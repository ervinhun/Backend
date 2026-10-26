using System.Security.Claims;
using System.Text;
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

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
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
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options.Secret), SecurityAlgorithms.HmacSha256),
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string secret) =>
        new(Encoding.UTF8.GetBytes(secret));
}
