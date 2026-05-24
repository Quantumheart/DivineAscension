using System;
using DivineAscension.Models.Enum;

namespace DivineAscension.Configuration;

/// <summary>
/// Resolves the maximum number of active blessing slots a player may unlock,
/// based on their favor rank and (optionally) their religion's prestige rank.
/// </summary>
public static class BlessingSlotCalculator
{
    /// <summary>
    /// Returns the player's active blessing slot allowance: favor-rank slots plus
    /// any prestige-rank bonus. When <paramref name="prestigeRank"/> is null (player
    /// has no religion), only the favor-rank slot count is returned.
    /// </summary>
    public static int GetMaxUnlocks(GameBalanceConfig config, FavorRank favorRank, PrestigeRank? prestigeRank)
    {
        ArgumentNullException.ThrowIfNull(config);

        var favorSlots = GetFavorSlots(config, favorRank);
        if (prestigeRank is null)
        {
            return favorSlots;
        }

        return favorSlots + GetPrestigeBonus(config, prestigeRank.Value);
    }

    private static int GetFavorSlots(GameBalanceConfig config, FavorRank rank) => rank switch
    {
        FavorRank.Initiate => config.InitiateActiveBlessingSlots,
        FavorRank.Disciple => config.DiscipleActiveBlessingSlots,
        FavorRank.Zealot => config.ZealotActiveBlessingSlots,
        FavorRank.Champion => config.ChampionActiveBlessingSlots,
        FavorRank.Avatar => config.AvatarActiveBlessingSlots,
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown favor rank")
    };

    private static int GetPrestigeBonus(GameBalanceConfig config, PrestigeRank rank) => rank switch
    {
        PrestigeRank.Fledgling => config.FledglingBonusSlots,
        PrestigeRank.Established => config.EstablishedBonusSlots,
        PrestigeRank.Renowned => config.RenownedBonusSlots,
        PrestigeRank.Legendary => config.LegendaryBonusSlots,
        PrestigeRank.Mythic => config.MythicBonusSlots,
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown prestige rank")
    };
}
