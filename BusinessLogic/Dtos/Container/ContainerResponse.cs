namespace BusinessLogic.Dtos.Container;

public class ContainerResponse
{
    public Guid Id { get; set; }
    public Guid FridgeId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>fridge, freezer or pantry.</summary>
    public string Type { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
