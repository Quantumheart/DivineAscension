using System.Collections.Generic;
using System.Globalization;
using DivineAscension.Network;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     One active boon as shown under "Boons of Standing": a concrete value
///     lead-in (e.g. "+15% favor") paired with the manuscript prose that
///     describes it.
/// </summary>
internal readonly record struct BoonPhrase(string Value, string Prose);

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
    ///     Walks the bonus DTO and returns one boon per active bonus, in
    ///     display order, each carrying its concrete value alongside the
    ///     manuscript prose. Inactive bonuses (multiplier == 1.0, slot count
    ///     0) are skipped. Multipliers render as percentages to match the PvP
    ///     chat convention ("[CIV +X%]"); holy-site slots as a flat count.
    /// </summary>
    public static IEnumerable<BoonPhrase> ActiveBonusPhrases(CivilizationBonusesDto bonuses)
    {
        if (bonuses.PrestigeMultiplier > 1f)
            yield return new BoonPhrase(
                $"+{Percent(bonuses.PrestigeMultiplier)} prestige",
                "Deeds are remembered in greater measure.");
        if (bonuses.FavorMultiplier > 1f)
            yield return new BoonPhrase(
                $"+{Percent(bonuses.FavorMultiplier)} favor",
                "Devotion is reckoned more keenly.");
        if (bonuses.ConquestMultiplier > 1f)
            yield return new BoonPhrase(
                $"+{Percent(bonuses.ConquestMultiplier)} conquest (in war)",
                "The Realm's banners strike with greater wrath in war.");
        if (bonuses.BonusHolySiteSlots > 0)
            yield return new BoonPhrase(
                $"+{bonuses.BonusHolySiteSlots} {(bonuses.BonusHolySiteSlots == 1 ? "hallow" : "hallows")}",
                "Hallows beyond the common count may be raised.");
    }

    private static string Percent(float multiplier)
    {
        return ((multiplier - 1f) * 100f).ToString("F0", CultureInfo.InvariantCulture) + "%";
    }
}
