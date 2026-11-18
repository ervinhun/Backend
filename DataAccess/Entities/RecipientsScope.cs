namespace DataAccess.Entities;

/// <summary>Who gets a fridge's expiry notifications. Stored as 'all_members', 'adder_and_owners' or 'custom'.</summary>
public enum RecipientsScope
{
    AllMembers,
    AdderAndOwners,
    Custom,
}
