namespace BusinessLogic.Dtos.Auth;

public record UserDto(Guid Id, string Email, string DisplayName, string Locale);
