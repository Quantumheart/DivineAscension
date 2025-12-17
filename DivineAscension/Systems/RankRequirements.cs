using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Systems;

/// <summary>
///     Provides rank requirement calculations and lookups
/// </summary>
[ExcludeFromCodeCoverage]
public static class RankRequirements
{
    /// <summary>
    ///     Get favor required to reach next rank
    /// </summary>
    /// <param name="currentRank">Current favor rank (0-4)</param>
    /// <returns>Favor required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredFavorForNextRank(int currentRank)
    {
        return currentRank switch
        {
            0 => 500, // Initiate → Disciple
            1 => 2000, // Disciple → Zealot
            2 => 5000, // Zealot → Champion
            3 => 10000, // Champion → Avatar
            4 => 0, // Max rank
            _ => 0 // Invalid rank
        };
    }

    /// <summary>
    ///     Get prestige required to reach next rank
    /// </summary>
    /// <param name="currentRank">Current prestige rank (0-4)</param>
    /// <returns>Prestige required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredPrestigeForNextRank(int currentRank)
    {
        return currentRank switch
        {
            0 => 500, // Fledgling → Established
            1 => 1500, // Established → Renowned
            2 => 3500, // Renowned → Legendary
            3 => 7500, // Legendary → Mythic
            4 => 0, // Max rank
            _ => 0 // Invalid rank
        };
    }

    /// <summary>
    ///     Get favor rank name
    /// </summary>
    /// <param name="rank">Favor rank (0-4)</param>
    /// <returns>Rank name</returns>
    public static string GetFavorRankName(int rank)
    {
        return rank switch
        {
            0 => "Initiate",
            1 => "Disciple",
            2 => "Zealot",
            3 => "Champion",
            4 => "Avatar",
            _ => $"Rank {rank}"
        };
    }

    /// <summary>
    ///     Get prestige rank name
    /// </summary>
    /// <param name="rank">Prestige rank (0-4)</param>
    /// <returns>Rank name</returns>
    public static string GetPrestigeRankName(int rank)
    {
        return rank switch
        {
            0 => "Fledgling",
            1 => "Established",
            2 => "Renowned",
            3 => "Legendary",
            4 => "Mythic",
            _ => $"Rank {rank}"
        };
    }
}