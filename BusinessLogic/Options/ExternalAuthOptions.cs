namespace BusinessLogic.Options;

/// <summary>
/// Bound from the "ExternalAuth" config section. A provider with no client ids is disabled.
/// Env: ExternalAuth__Google__ClientIds__0, ExternalAuth__Apple__ClientIds__0, ...
/// </summary>
public class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    public ExternalProviderOptions Google { get; set; } = new();
    public ExternalProviderOptions Apple { get; set; } = new();
}

public class ExternalProviderOptions
{
    /// <summary>
    /// Accepted ID token audiences. Google: the OAuth client ids the app signs in with
    /// (iOS, Android, web/server). Apple: the iOS bundle id (and a Services ID if web sign-in is added).
    /// </summary>
    public string[] ClientIds { get; set; } = [];
}
