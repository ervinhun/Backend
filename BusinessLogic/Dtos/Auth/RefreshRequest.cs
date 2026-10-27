using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Auth;

public class RefreshRequest
{
    [Required, MaxLength(200)]
    public string RefreshToken { get; init; } = string.Empty;
}
