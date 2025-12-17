namespace DivineAscension.Models;

/// <summary>
///     Represents religion prestige progression toward the next rank
/// </summary>
public class ReligionPrestigeProgress
{
    /// <summary>
    ///     Current prestige amount
    /// </summary>
    public int CurrentPrestige { get; set; }

    /// <summary>
    ///     Prestige required to reach next rank
    /// </summary>
    public int RequiredPrestige { get; set; }

    /// <summary>
    ///     Current prestige rank (0-4)
    /// </summary>
    public int CurrentRank { get; set; }

    /// <summary>
    ///     Next prestige rank (0-4)
    /// </summary>
    public int NextRank { get; set; }

    /// <summary>
    ///     Whether religion is at maximum rank
    /// </summary>
    public bool IsMaxRank { get; set; }

    /// <summary>
    ///     Progress percentage (0.0 to 1.0)
    /// </summary>
    public float ProgressPercentage =>
        IsMaxRank ? 1f : RequiredPrestige > 0 ? (float)CurrentPrestige / RequiredPrestige : 0f;
}