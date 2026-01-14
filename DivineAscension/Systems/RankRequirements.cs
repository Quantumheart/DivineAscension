using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Systems;

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
            0 => 2500, // Fledgling → Established (5x scaling)
            1 => 10000, // Established → Renowned (5x scaling)
            2 => 25000, // Renowned → Legendary (5x scaling)
            3 => 50000, // Legendary → Mythic (5x scaling)
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