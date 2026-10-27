using DataAccess.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Authentication;

/// <summary>
/// Resolved from DI per request (via <see cref="JwtBearerOptions.EventsType"/>),
/// so it can take scoped services like the repository.
/// </summary>
public class AppJwtBearerEvents(IUserRepository users, IUserSessionRepository sessions) : JwtBearerEvents
{
    // A signature-valid token stays valid until it expires, so also reject it once
    // its session has signed out or its account is soft-deleted or gone.
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var ct = context.HttpContext.RequestAborted;
        var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var sid = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;

        // Every token we issue has both; one without them can't be signed out, so refuse it.
        if (!Guid.TryParse(sub, out var userId) || !Guid.TryParse(sid, out var sessionId))
        {
            context.Fail("The token is missing its subject or session.");
            return;
        }

        if (!await sessions.IsSignedInAsync(sessionId, userId, ct))
        {
            context.Fail("This token has been signed out.");
            return;
        }

        if (await users.GetByIdAsync(userId, ct) is null)
            context.Fail("The user this token was issued for no longer exists.");
    }
}
