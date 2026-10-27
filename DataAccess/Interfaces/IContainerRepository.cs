using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>The containers of a fridge (containers). Deleted containers are never returned.</summary>
public interface IContainerRepository
{
    /// <summary>
    /// Saves the container, filling in its generated id, unless the fridge has reached its max_containers.
    /// Concurrent adds are counted one at a time, so the limit can't be passed.
    /// </summary>
    Task<AddContainerResult> AddAsync(Container container, CancellationToken ct = default);

    /// <summary>The fridge's containers, oldest first.</summary>
    Task<IReadOnlyList<Container>> GetForFridgeAsync(Guid fridgeId, CancellationToken ct = default);

    /// <summary>Null if there is no such container in this fridge.</summary>
    Task<Container?> GetAsync(Guid fridgeId, Guid containerId, CancellationToken ct = default);

    /// <summary>Returns false if there is no such container in this fridge.</summary>
    Task<bool> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        string name,
        ContainerType containerType,
        CancellationToken ct = default
    );

    /// <summary>Soft-deletes the container, which frees its slot. Returns false if there is no such container in this fridge.</summary>
    Task<bool> DeleteAsync(
        Guid fridgeId,
        Guid containerId,
        DateTimeOffset now,
        CancellationToken ct = default
    );
}
