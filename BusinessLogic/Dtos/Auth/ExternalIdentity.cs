namespace BusinessLogic.Dtos.Auth;

/// <summary>The verified contents of a Google or Apple ID token.</summary>
public record ExternalIdentity(
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    string? Name);
