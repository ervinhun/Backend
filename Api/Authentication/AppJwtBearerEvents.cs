using DataAccess.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Authentication;

/// <summary>
/// Resolved from DI per request (via <see cref="JwtBearerOptions.EventsType"/>),
/// so it can take scoped services like the repository.
/// </summary>
public class AppJwtBearerEvents(IUserRepository users, ITokenRepository tokens) : JwtBearerEvents
{
    // A signature-valid token stays valid until it expires, so also reject it once
    // it has been signed out (revoked_tokens) or its account is soft-deleted or gone.
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var ct = context.HttpContext.RequestAborted;
        var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        // Every token we issue has both; one without them can't be signed out, so refuse it.
        if (!Guid.TryParse(sub, out var userId) || !Guid.TryParse(jti, out var tokenId))
        {
            context.Fail("The token is missing its subject or id.");
            return;
        }

        if (await tokens.IsAccessTokenRevokedAsync(tokenId, ct))
        {
            context.Fail("This token has been signed out.");
            return;
        }

        if (await users.GetByIdAsync(userId, ct) is null)
            context.Fail("The user this token was issued for no longer exists.");
    }
}
