namespace PantheonWars.Models;

/// <summary>
///     Represents player favor progression toward the next rank
/// </summary>
public class PlayerFavorProgress
{
    /// <summary>
    ///     Current favor amount
    /// </summary>
    public int CurrentFavor { get; set; }

    /// <summary>
    ///     Favor required to reach next rank
    /// </summary>
    public int RequiredFavor { get; set; }

    /// <summary>
    ///     Current favor rank (0-4)
    /// </summary>
    public int CurrentRank { get; set; }

    /// <summary>
    ///     Next favor rank (0-4)
    /// </summary>
    public int NextRank { get; set; }

    /// <summary>
    ///     Whether player is at maximum rank
    /// </summary>
    public bool IsMaxRank { get; set; }

    /// <summary>
    ///     Progress percentage (0.0 to 1.0)
    /// </summary>
    public float ProgressPercentage => IsMaxRank ? 1f : RequiredFavor > 0 ? (float)CurrentFavor / RequiredFavor : 0f;
}