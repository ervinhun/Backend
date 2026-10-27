using BusinessLogic.Dtos.Container;

namespace BusinessLogic.Interfaces;

public interface IContainerService
{
    Task<ContainerResponse> CreateAsync(
        Guid fridgeId,
        SaveContainerRequest request,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<ContainerResponse>> GetForFridgeAsync(
        Guid fridgeId,
        CancellationToken ct = default
    );
    Task<ContainerResponse> GetAsync(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct = default
    );
    Task<ContainerResponse> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        SaveContainerRequest request,
        CancellationToken ct = default
    );
    Task DeleteAsync(Guid fridgeId, Guid containerId, CancellationToken ct = default);
}
