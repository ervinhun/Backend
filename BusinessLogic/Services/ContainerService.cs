using BusinessLogic.Dtos.Container;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

/// <summary>
/// Every member of the fridge can see its containers; the owner and members who may put things in can change them.
/// Also keeps a fridge within its max_containers.
/// </summary>
public class ContainerService(
    IContainerRepository containers,
    IFridgeRepository fridges,
    TimeProvider timeProvider
) : IContainerService
{
    private const string FridgeNotFoundMessage = "Fridge not found.";
    private const string ContainerNotFoundMessage = "Container not found.";

    public async Task<ContainerResponse> CreateAsync(
        Guid fridgeId,
        Guid userId,
        SaveContainerRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: true, ct);

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
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: false, ct);

        return (await containers.GetForFridgeAsync(fridgeId, ct))
            .Select(c => c.ToResponse())
            .ToList();
    }

    public async Task<ContainerResponse> GetAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: false, ct);

        return await FindAsync(fridgeId, containerId, ct);
    }

    public async Task<ContainerResponse> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        SaveContainerRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: true, ct);

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

        return await FindAsync(fridgeId, containerId, ct);
    }

    public async Task DeleteAsync(
        Guid fridgeId,
        Guid containerId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, write: true, ct);

        if (!await containers.DeleteAsync(fridgeId, containerId, timeProvider.GetUtcNow(), ct))
            throw new NotFoundException(ContainerNotFoundMessage);
    }

    private async Task<ContainerResponse> FindAsync(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct
    ) =>
        (
            await containers.GetAsync(fridgeId, containerId, ct)
            ?? throw new NotFoundException(ContainerNotFoundMessage)
        ).ToResponse();

    // Non-members get 404 so the existence of a fridge is not leaked.
    private async Task AuthorizeAsync(Guid fridgeId, Guid userId, bool write, CancellationToken ct)
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(FridgeNotFoundMessage);
        if (write && !member.IsOwner && !member.CanPut)
            throw new ForbiddenException("You are not allowed to change this fridge's containers.");
    }
}
