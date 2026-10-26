using DataAccess.Entities;

namespace BusinessLogic.Interfaces;

public interface ITokenService
{
    /// <param name="sessionId">The signed-in device (user_devices row) this token belongs to, sent as <c>sid</c>.</param>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, Guid sessionId);

    /// <summary>A new random refresh token, plus the hash to store instead of it.</summary>
    (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string token);
}
