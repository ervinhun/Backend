using DataAccess.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Authentication;

/// <summary>
/// Resolved from DI per request (via <see cref="JwtBearerOptions.EventsType"/>),
/// so it can take scoped services like the repository.
/// </summary>
public class AppJwtBearerEvents(IUserRepository users) : JwtBearerEvents
{
    // A signature-valid token outlives its user row: reject it once the account is
    // soft-deleted (users.deleted_at) or gone, instead of waiting for it to expire.
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(sub, out var userId)
            || await users.GetByIdAsync(userId, context.HttpContext.RequestAborted) is null)
        {
            context.Fail("The user this token was issued for no longer exists.");
        }
    }
}
