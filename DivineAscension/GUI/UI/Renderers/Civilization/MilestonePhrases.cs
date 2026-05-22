using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Manuscript phrasing tables for the Chronicles ledger chapter. Verb
///     phrases form the "{0} of {1} ..." tail under each in-progress deed;
///     bonus phrases are the one-line sentences under "Boons of Standing".
///     Keyed off the string identifiers carried on the network DTOs so the
///     phrase table stays decoupled from the enum/model layers.
/// </summary>
internal static class MilestonePhrases
{
    private static readonly Dictionary<string, string> VerbPhrasesByTrigger = new()
    {
        ["ReligionCount"] = "Banner Orders raised",
        ["DomainCount"] = "domains gathered",
        ["HolySiteCount"] = "hallows claimed",
        ["RitualCount"] = "rites performed",
        ["MemberCount"] = "souls sworn",
        ["WarKillCount"] = "foes felled in war",
        ["HolySiteTier"] = "tiers attained",
        ["DiplomaticRelationship"] = "accords sealed",
        ["AllMajorMilestones"] = "great deeds set down",
    };

    /// <summary>
    ///     Verb phrase that follows a "{0} of {1}" count on an in-progress
    ///     deed. Falls back to a generic "set down" when the trigger type is
    ///     unknown so an added milestone type still renders sensibly.
    /// </summary>
    public static string GetVerbPhrase(string triggerType)
    {
        return VerbPhrasesByTrigger.TryGetValue(triggerType ?? string.Empty, out var phrase)
            ? phrase
            : "set down";
    }

    /// <summary>
    ///     Walks the bonus DTO and returns one manuscript sentence per active
    ///     bonus, in display order. Inactive bonuses (multiplier == 1.0,
    ///     slot count 0) are skipped.
    /// </summary>
    public static IEnumerable<string> ActiveBonusPhrases(CivilizationBonusesDto bonuses)
    {
        if (bonuses.PrestigeMultiplier > 1f)
            yield return "Deeds are remembered in greater measure.";
        if (bonuses.FavorMultiplier > 1f)
            yield return "Devotion is reckoned more keenly.";
        if (bonuses.ConquestMultiplier > 1f)
            yield return "The Realm's banners strike with greater wrath in war.";
        if (bonuses.BonusHolySiteSlots > 0)
            yield return "Hallows beyond the common count may be raised.";
    }
}
