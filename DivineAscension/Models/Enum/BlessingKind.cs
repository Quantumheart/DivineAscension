namespace DivineAscension.Models.Enum;

/// <summary>
///     Type of blessing - determines who benefits and unlock requirements
/// </summary>
public enum BlessingKind
{
    /// <summary>
    ///     Personal blessing - unlocked by player favor rank, benefits individual player
    /// </summary>
    Player,

    /// <summary>
    ///     Religion blessing - unlocked by religion prestige rank, benefits all congregation members
    /// </summary>
    Religion,

    /// <summary>
    ///     Civilization blessing - unlocked by civilization milestones, benefits all civilization members
    /// </summary>
    Civilization
}