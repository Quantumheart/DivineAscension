namespace DivineAscension.Models.Enum;

/// <summary>
///     Civilization ranks - collective progression based on completed milestones
/// </summary>
public enum CivilizationRank
{
    /// <summary>
    ///     Starting rank - 0 major milestones completed
    /// </summary>
    Nascent = 0,

    /// <summary>
    ///     Second rank - 1 major milestone completed
    /// </summary>
    Rising = 1,

    /// <summary>
    ///     Third rank - 2 major milestones completed
    /// </summary>
    Dominant = 2,

    /// <summary>
    ///     Fourth rank - 3 major milestones completed
    /// </summary>
    Hegemonic = 3,

    /// <summary>
    ///     Highest rank - 4+ major milestones completed
    /// </summary>
    Eternal = 4
}
