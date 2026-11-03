using BusinessLogic.Dtos.Container;

namespace BusinessLogic.Interfaces;

public interface IContainerService
{
    Task<ContainerResponse> CreateAsync(
        Guid fridgeId,
        Guid userId,
        SaveContainerRequest request,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<ContainerResponse>> GetForFridgeAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );
    Task<ContainerResponse> GetAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        CancellationToken ct = default
    );
    Task<ContainerResponse> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        SaveContainerRequest request,
        CancellationToken ct = default
    );
    Task DeleteAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        CancellationToken ct = default
    );
}
