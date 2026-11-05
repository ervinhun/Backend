namespace DataAccess.Entities;

/// <summary>Stored lower case in <c>items.status</c>, which only accepts these four.</summary>
public enum ItemStatus
{
    Active,
    Consumed,
    Expired,
    Removed,
}
