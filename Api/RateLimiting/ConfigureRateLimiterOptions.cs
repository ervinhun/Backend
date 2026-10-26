using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>Anonymous auth endpoints that check passwords or create accounts.</summary>
    public const string Auth = "auth";
}

/// <summary>
/// Rate limit policies, applied per endpoint with <c>[EnableRateLimiting(RateLimitPolicies.X)]</c>.
/// Rejected requests get 429 with a Retry-After header and a ProblemDetails body.
/// </summary>
public class ConfigureRateLimiterOptions(IOptions<AuthRateLimitOptions> authOptions) : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions options)
    {
        var auth = authOptions.Value;

        // Per client IP: slows password guessing and mass sign-ups without
        // touching the account being targeted. Behind a reverse proxy this needs
        // forwarded headers, or every client shares the proxy's IP.
        options.AddPolicy(RateLimitPolicies.Auth, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = auth.PermitLimit,
                    Window = TimeSpan.FromSeconds(auth.WindowSeconds),
                    QueueLimit = 0,
                }));

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
}
