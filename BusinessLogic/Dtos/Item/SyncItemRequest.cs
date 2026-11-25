using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Item;

/// <summary>An item as it is on a phone, which may have changed it while offline.</summary>
public class SyncItemRequest
{
    /// <summary>
    /// fridge, freezer or pantry. The item goes in the fridge's oldest container of that type, which is created if
    /// there is none; one that is already in a container of that type stays there.
    /// </summary>
    [Required, AllowedValues("fridge", "freezer", "pantry")]
    public string Storage { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Left as it was when missing or no longer known.</summary>
    public Guid? ProductTypeId { get; set; }

    [StringLength(50)]
    public string? Amount { get; set; }

    /// <summary>The day it must be eaten by, which opening it may have moved forward.</summary>
    [Required]
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>The date printed on it, if opening it moved the expiry date forward.</summary>
    public DateOnly? BestBefore { get; set; }

    public DateOnly? OpenedOn { get; set; }

    /// <summary>When the user made the change. A time in the future counts as now.</summary>
    [Required]
    public DateTimeOffset? ChangedAt { get; set; }
}
