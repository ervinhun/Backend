using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>Anonymous auth endpoints that check passwords or create accounts.</summary>
    public const string Auth = "auth";

    /// <summary>Joining a fridge with an invite code and replacing a fridge's code.</summary>
    public const string InviteCode = "invite-code";
}

/// <summary>
/// Rate limit policies, applied per endpoint with <c>[EnableRateLimiting(RateLimitPolicies.X)]</c>.
/// Rejected requests get 429 with a Retry-After header and a ProblemDetails body.
/// </summary>
public class ConfigureRateLimiterOptions(
    IOptions<AuthRateLimitOptions> authOptions,
    IOptions<InviteCodeRateLimitOptions> inviteCodeOptions) : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions options)
    {
        var auth = authOptions.Value;
        var inviteCode = inviteCodeOptions.Value;

        // Per client IP: slows password guessing and mass sign-ups without
        // touching the account being targeted. Behind a reverse proxy this needs
        // forwarded headers, or every client shares the proxy's IP.
        AddFixedWindowPolicy(
            options,
            RateLimitPolicies.Auth,
            auth.PermitLimit,
            auth.WindowSeconds,
            ClientIp);

        // Per signed-in user, so people sharing an IP (a household, an office) don't share a budget
        // and one user can't use up another's. Counted in memory, so it resets on restart and each
        // app instance keeps its own count.
        AddFixedWindowPolicy(
            options,
            RateLimitPolicies.InviteCode,
            inviteCode.PermitLimit,
            inviteCode.WindowSeconds,
            UserOrClientIp);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Too many attempts. Please wait a moment and try again.",
                },
            });
        };
    }

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // The sub claim is always a user id here: AppJwtBearerEvents refuses tokens without one. Falls back to the IP
    // for requests that carry no valid token, so they can't all share one bucket with the signed-in users.
    private static string UserOrClientIp(HttpContext context) =>
        context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) is { } userId
            ? $"user:{userId}"
            : $"ip:{ClientIp(context)}";

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        int windowSeconds,
        Func<HttpContext, string> partitionKey) =>
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0,
                }));
}
