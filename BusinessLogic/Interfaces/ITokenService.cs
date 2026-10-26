using DataAccess.Entities;

namespace BusinessLogic.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
}
