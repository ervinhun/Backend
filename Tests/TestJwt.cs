using BusinessLogic.Options;

namespace Tests;

public static class TestJwt
{
    public static readonly JwtOptions Options = new()
    {
        Secret = "test-secret-that-is-at-least-32-bytes-long!!",
        Issuer = "FridgeFriend.Tests",
        Audience = "FridgeFriend.Tests.App",
        AccessTokenMinutes = 15,
    };
}
