namespace DataAccess.Entities;

/// <summary>Stored lower case in <c>notification_dispatches.stage</c>, which only accepts these two.</summary>
public enum NotificationStage
{
    Warning,
    Expiry,
}
