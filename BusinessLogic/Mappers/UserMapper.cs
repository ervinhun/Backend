using BusinessLogic.Dtos.Auth;
using DataAccess.Entities;

namespace BusinessLogic.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Email, user.DefaultDisplayName, user.Locale);
}
