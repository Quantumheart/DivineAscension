namespace PantheonWars.Models.Enum;

/// <summary>
///     Religion prestige ranks - collective progression for the entire congregation
/// </summary>
public enum PrestigeRank
{
    /// <summary>
    ///     Starting rank - 0-499 prestige
    /// </summary>
    Fledgling = 0,

    /// <summary>
    ///     Second rank - 500-1999 prestige
    /// </summary>
    Established = 1,

    /// <summary>
    ///     Third rank - 2000-4999 prestige
    /// </summary>
    Renowned = 2,

    /// <summary>
    ///     Fourth rank - 5000-9999 prestige
    /// </summary>
    Legendary = 3,

    /// <summary>
    ///     Highest rank - 10000+ prestige
    /// </summary>
    Mythic = 4
}