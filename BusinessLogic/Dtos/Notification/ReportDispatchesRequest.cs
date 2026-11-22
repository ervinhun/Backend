using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Notification;

public class ReportDispatchesRequest
{
    /// <summary>When the device showed the notification.</summary>
    [Required]
    public DateTimeOffset? SentAt { get; set; }

    [Required, MinLength(1), MaxLength(500)]
    public List<DispatchedItem> Items { get; set; } = [];
}

public class DispatchedItem
{
    public Guid ItemId { get; set; }

    /// <summary>warning or expiry.</summary>
    [Required, AllowedValues("warning", "expiry")]
    public string Stage { get; set; } = string.Empty;
}
