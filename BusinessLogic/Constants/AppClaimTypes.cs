namespace BusinessLogic.Constants;

/// <summary>
/// Custom JWT claims for <c>users</c> columns that have no registered claim name.
/// id, email and default_display_name use the registered "sub", "email" and "name".
/// </summary>
public static class AppClaimTypes
{
    public const string Locale = "locale";
    public const string AuthProvider = "auth_provider";
}
