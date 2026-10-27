using BusinessLogic.Constants;
using BusinessLogic.Services;
using DataAccess.Entities;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Tests.BusinessLogic;

public class JwtTokenServiceTests
{
    private static readonly User User = new()
    {
        Id = Guid.NewGuid(),
        Email = "anna@example.com",
        DefaultDisplayName = "Anna",
        Locale = "pl",
    };

    private static readonly Guid SessionId = Guid.NewGuid();

    [Fact]
    public async Task CreateAccessToken_ProducesTokenThatValidatesWithSameKey()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), time);

        var (token, expiresAt) = service.CreateAccessToken(User, SessionId);

        Assert.Equal(time.GetUtcNow().AddMinutes(TestJwt.Options.AccessTokenMinutes), expiresAt);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = TestJwt.Options.Issuer,
            ValidAudience = TestJwt.Options.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(TestJwt.Options.Secret),
        });

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(User.Id.ToString(), result.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal(User.Email, result.Claims[JwtRegisteredClaimNames.Email]);
        Assert.Equal(User.DefaultDisplayName, result.Claims[JwtRegisteredClaimNames.Name]);
        Assert.Equal(User.Locale, result.Claims[AppClaimTypes.Locale]);
        Assert.Equal(User.AuthProvider, result.Claims[AppClaimTypes.AuthProvider]);
        Assert.Equal(SessionId.ToString(), result.Claims[JwtRegisteredClaimNames.Sid]);
    }

    [Fact]
    public async Task CreateAccessToken_IsRejectedWithDifferentKey()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), TimeProvider.System);
        var (token, _) = service.CreateAccessToken(User, SessionId);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = TestJwt.Options.Issuer,
            ValidAudience = TestJwt.Options.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey("a-completely-different-secret-of-32+-bytes"),
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateRefreshToken_IsRandom_AndHashMatches()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), time);

        var a = service.CreateRefreshToken();
        var b = service.CreateRefreshToken();

        Assert.NotEqual(a.Token, b.Token);
        Assert.Equal(a.Hash, service.HashRefreshToken(a.Token));
        Assert.NotEqual(a.Token, a.Hash);
        Assert.Equal(time.GetUtcNow().AddDays(TestJwt.Options.RefreshTokenDays), a.ExpiresAt);
    }
}
