using BusinessLogic.Dtos.Container;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

/// <summary>
/// Who may do what is decided by the caller (the controller checks membership and rights first);
/// this only applies the changes and keeps a fridge within its max_containers.
/// </summary>
public class ContainerService(IContainerRepository containers, TimeProvider timeProvider)
    : IContainerService
{
    private const string FridgeNotFoundMessage = "Fridge not found.";
    private const string ContainerNotFoundMessage = "Container not found.";

    public async Task<ContainerResponse> CreateAsync(
        Guid fridgeId,
        SaveContainerRequest request,
        CancellationToken ct = default
    )
    {
        var container = new Container
        {
            FridgeId = fridgeId,
            Name = request.Name.Trim(),
            ContainerType = request.ToContainerType(),
        };

        var result = await containers.AddAsync(container, ct);

        return result.Status switch
        {
            AddContainerStatus.Added => container.ToResponse(),
            AddContainerStatus.FridgeNotFound => throw new NotFoundException(FridgeNotFoundMessage),
            AddContainerStatus.Full => throw new ConflictException(
                $"This fridge has reached its limit of {result.MaxContainers} containers."
            ),
            _ => throw new InvalidOperationException($"Unexpected add result {result.Status}."),
        };
    }

    public async Task<IReadOnlyList<ContainerResponse>> GetForFridgeAsync(
        Guid fridgeId,
        CancellationToken ct = default
    ) => (await containers.GetForFridgeAsync(fridgeId, ct)).Select(c => c.ToResponse()).ToList();

    public async Task<ContainerResponse> GetAsync(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct = default
    ) =>
        (
            await containers.GetAsync(fridgeId, containerId, ct)
            ?? throw new NotFoundException(ContainerNotFoundMessage)
        ).ToResponse();

    public async Task<ContainerResponse> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        SaveContainerRequest request,
        CancellationToken ct = default
    )
    {
        if (
            !await containers.UpdateAsync(
                fridgeId,
                containerId,
                request.Name.Trim(),
                request.ToContainerType(),
                ct
            )
        )
            throw new NotFoundException(ContainerNotFoundMessage);

        return await GetAsync(fridgeId, containerId, ct);
    }

    public async Task DeleteAsync(Guid fridgeId, Guid containerId, CancellationToken ct = default)
    {
        if (!await containers.DeleteAsync(fridgeId, containerId, timeProvider.GetUtcNow(), ct))
            throw new NotFoundException(ContainerNotFoundMessage);
    }
}
