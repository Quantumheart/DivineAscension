namespace DivineAscension.GUI.Models.Hud;

/// <summary>
///     Immutable view model containing all display data for the rank progress HUD overlay
/// </summary>
/// <param name="FavorRankName">Current favor rank display name (e.g., "Disciple")</param>
/// <param name="NextFavorRankName">Next favor rank display name (e.g., "Zealot")</param>
/// <param name="TotalFavorEarned">Total lifetime favor earned</param>
/// <param name="FavorRequiredForNext">Favor required to reach next rank</param>
/// <param name="FavorProgress">Progress percentage towards next rank (0.0 to 1.0)</param>
/// <param name="IsFavorMaxRank">Whether player is at max favor rank (Avatar)</param>
/// <param name="PrestigeRankName">Current religion prestige rank display name (e.g., "Established")</param>
/// <param name="NextPrestigeRankName">Next prestige rank display name (e.g., "Renowned")</param>
/// <param name="CurrentPrestige">Current religion prestige points</param>
/// <param name="PrestigeRequiredForNext">Prestige required for next rank</param>
/// <param name="PrestigeProgress">Progress percentage towards next prestige rank (0.0 to 1.0)</param>
/// <param name="IsPrestigeMaxRank">Whether religion is at max prestige rank (Mythic)</param>
/// <param name="SpendableFavor">Current spendable favor balance</param>
/// <param name="ScreenWidth">Screen width for positioning</param>
/// <param name="ScreenHeight">Screen height for positioning</param>
/// <param name="IsVisible">Whether the HUD should be displayed</param>
public readonly record struct RankProgressHudViewModel(
    string FavorRankName,
    string NextFavorRankName,
    int TotalFavorEarned,
    int FavorRequiredForNext,
    float FavorProgress,
    bool IsFavorMaxRank,
    string PrestigeRankName,
    string NextPrestigeRankName,
    int CurrentPrestige,
    int PrestigeRequiredForNext,
    float PrestigeProgress,
    bool IsPrestigeMaxRank,
    int SpendableFavor,
    float ScreenWidth,
    float ScreenHeight,
    bool IsVisible);
