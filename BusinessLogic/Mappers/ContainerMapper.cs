using BusinessLogic.Dtos.Container;
using DataAccess.Entities;

namespace BusinessLogic.Mappers;

public static class ContainerMapper
{
    public static ContainerResponse ToResponse(this Container container) =>
        new()
        {
            Id = container.Id,
            FridgeId = container.FridgeId,
            Name = container.Name,
            Type = container.ContainerType.ToString().ToLowerInvariant(),
            CreatedAt = container.CreatedAt,
        };

    /// <summary>The request has been validated, so the type is one of the three names.</summary>
    public static ContainerType ToContainerType(this SaveContainerRequest request) =>
        Enum.Parse<ContainerType>(request.Type, ignoreCase: true);
}
