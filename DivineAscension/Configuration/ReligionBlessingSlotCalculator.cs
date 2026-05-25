using System;
using DivineAscension.Models.Enum;

namespace DivineAscension.Configuration;

/// <summary>
/// Resolves the maximum number of religion blessings a religion may have inscribed at once,
/// based on its prestige rank (#479). Unlike personal blessing slots, religion slots are not
/// additive — the religion gets exactly the configured count for its current rank.
/// </summary>
public static class ReligionBlessingSlotCalculator
{
    /// <summary>
    /// Returns the religion blessing inscribe-slot allowance for the given prestige rank.
    /// </summary>
    public static int GetMaxUnlocks(GameBalanceConfig config, PrestigeRank prestigeRank)
    {
        ArgumentNullException.ThrowIfNull(config);

        return prestigeRank switch
        {
            PrestigeRank.Fledgling => config.FledglingReligionBlessingSlots,
            PrestigeRank.Established => config.EstablishedReligionBlessingSlots,
            PrestigeRank.Renowned => config.RenownedReligionBlessingSlots,
            PrestigeRank.Legendary => config.LegendaryReligionBlessingSlots,
            PrestigeRank.Mythic => config.MythicReligionBlessingSlots,
            _ => throw new ArgumentOutOfRangeException(nameof(prestigeRank), prestigeRank, "Unknown prestige rank")
        };
    }
}
