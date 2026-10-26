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
    };

    [Fact]
    public async Task CreateAccessToken_ProducesTokenThatValidatesWithSameKey()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), time);

        var (token, expiresAt) = service.CreateAccessToken(User);

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
    }

    [Fact]
    public async Task CreateAccessToken_IsRejectedWithDifferentKey()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), TimeProvider.System);
        var (token, _) = service.CreateAccessToken(User);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = TestJwt.Options.Issuer,
            ValidAudience = TestJwt.Options.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey("a-completely-different-secret-of-32+-bytes"),
        });

        Assert.False(result.IsValid);
    }
}
