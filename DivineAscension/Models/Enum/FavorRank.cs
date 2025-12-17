namespace PantheonWars.Models.Enum;

/// <summary>
///     Player favor ranks - individual progression for each player
/// </summary>
public enum FavorRank
{
    /// <summary>
    ///     Starting rank - 0-499 total favor earned
    /// </summary>
    Initiate = 0,

    /// <summary>
    ///     Second rank - 500-1999 total favor earned
    /// </summary>
    Disciple = 1,

    /// <summary>
    ///     Third rank - 2000-4999 total favor earned
    /// </summary>
    Zealot = 2,

    /// <summary>
    ///     Fourth rank - 5000-9999 total favor earned
    /// </summary>
    Champion = 3,

    /// <summary>
    ///     Highest rank - 10000+ total favor earned
    /// </summary>
    Avatar = 4
}