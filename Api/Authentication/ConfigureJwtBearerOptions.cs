using BusinessLogic.Options;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Authentication;

/// <summary>
/// Sets how incoming bearer tokens are validated. Reads the validated
/// <see cref="JwtOptions"/> from DI instead of raw config, so it uses the same
/// issuer, audience and key that <see cref="JwtTokenService"/> signs with.
/// </summary>
public class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var jwt = jwtOptions.Value;

        // Keep claim names as issued ("sub", "email") instead of mapping to long WS-* URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.Secret),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
        };
        options.EventsType = typeof(AppJwtBearerEvents);
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}
