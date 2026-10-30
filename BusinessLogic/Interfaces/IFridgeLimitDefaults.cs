namespace BusinessLogic.Interfaces;

/// <summary>
/// The limits a fridge is given when it is created. They are stored on the fridge and never touched
/// by a rename, so each one can still be overridden per fridge. Register another implementation to
/// take them from FeatureHub.
/// </summary>
public interface IFridgeLimitDefaults
{
    int MaxMembers { get; }
    int MaxContainers { get; }
}
