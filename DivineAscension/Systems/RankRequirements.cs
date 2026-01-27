using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Systems;

/// <summary>
///     Provides rank requirement calculations and lookups
/// </summary>
[ExcludeFromCodeCoverage]
public static class RankRequirements
{
    /// <summary>
    ///     Get favor required to reach next rank (uses default hardcoded values for backward compatibility)
    /// </summary>
    /// <param name="currentRank">Current favor rank (0-4)</param>
    /// <returns>Favor required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredFavorForNextRank(int currentRank)
    {
        return GetRequiredFavorForNextRank(currentRank, 500, 2000, 5000, 10000);
    }

    /// <summary>
    ///     Get favor required to reach next rank with custom config thresholds
    /// </summary>
    /// <param name="currentRank">Current favor rank (0-4)</param>
    /// <param name="discipleThreshold">Favor required for Disciple rank</param>
    /// <param name="zealotThreshold">Favor required for Zealot rank</param>
    /// <param name="championThreshold">Favor required for Champion rank</param>
    /// <param name="avatarThreshold">Favor required for Avatar rank</param>
    /// <returns>Favor required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredFavorForNextRank(int currentRank, int discipleThreshold, int zealotThreshold, int championThreshold, int avatarThreshold)
    {
        return currentRank switch
        {
            0 => discipleThreshold, // Initiate → Disciple
            1 => zealotThreshold, // Disciple → Zealot
            2 => championThreshold, // Zealot → Champion
            3 => avatarThreshold, // Champion → Avatar
            4 => 0, // Max rank
            _ => 0 // Invalid rank
        };
    }

    /// <summary>
    ///     Get prestige required to reach next rank (uses default hardcoded values for backward compatibility)
    /// </summary>
    /// <param name="currentRank">Current prestige rank (0-4)</param>
    /// <returns>Prestige required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredPrestigeForNextRank(int currentRank)
    {
        return GetRequiredPrestigeForNextRank(currentRank, 2500, 10000, 25000, 50000);
    }

    /// <summary>
    ///     Get prestige required to reach next rank with custom config thresholds
    /// </summary>
    /// <param name="currentRank">Current prestige rank (0-4)</param>
    /// <param name="establishedThreshold">Prestige required for Established rank</param>
    /// <param name="renownedThreshold">Prestige required for Renowned rank</param>
    /// <param name="legendaryThreshold">Prestige required for Legendary rank</param>
    /// <param name="mythicThreshold">Prestige required for Mythic rank</param>
    /// <returns>Prestige required for next rank, or 0 if at max rank or invalid</returns>
    public static int GetRequiredPrestigeForNextRank(int currentRank, int establishedThreshold, int renownedThreshold, int legendaryThreshold, int mythicThreshold)
    {
        return currentRank switch
        {
            0 => establishedThreshold, // Fledgling → Established
            1 => renownedThreshold, // Established → Renowned
            2 => legendaryThreshold, // Renowned → Legendary
            3 => mythicThreshold, // Legendary → Mythic
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

    /// <summary>
    ///     Get civilization rank name
    /// </summary>
    /// <param name="rank">Civilization rank (0-4)</param>
    /// <returns>Rank name</returns>
    public static string GetCivilizationRankName(int rank)
    {
        return rank switch
        {
            0 => "Nascent",
            1 => "Rising",
            2 => "Dominant",
            3 => "Hegemonic",
            4 => "Eternal",
            _ => $"Rank {rank}"
        };
    }
}