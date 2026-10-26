namespace BusinessLogic.Dtos.Auth;

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
