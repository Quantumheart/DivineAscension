using System.Collections.Generic;
using PantheonWars.Models.Enum;

namespace PantheonWars.Models;

/// <summary>
///     Contains formatted data for displaying blessing tooltips on hover
/// </summary>
public class BlessingTooltipData
{
    /// <summary>
    ///     Blessing name (title of tooltip)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Blessing description (main text)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Category of the blessing (Combat, Defense, etc.)
    /// </summary>
    public BlessingCategory Category { get; set; }

    /// <summary>
    ///     Blessing kind (Player or Religion)
    /// </summary>
    public BlessingKind Kind { get; set; }

    /// <summary>
    ///     Tier/level in the tree (1-4)
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    ///     Required favor rank to unlock (for Player blessings)
    /// </summary>
    public string RequiredFavorRank { get; set; } = string.Empty;

    /// <summary>
    ///     Required prestige rank to unlock (for Religion blessings)
    /// </summary>
    public string RequiredPrestigeRank { get; set; } = string.Empty;

    /// <summary>
    ///     List of prerequisite blessing names that must be unlocked first
    /// </summary>
    public List<string> PrerequisiteNames { get; set; } = new();

    /// <summary>
    ///     Formatted stat modifiers (e.g., "+10% melee damage", "+5 max health")
    /// </summary>
    public List<string> FormattedStats { get; set; } = new();

    /// <summary>
    ///     Special effect descriptions
    /// </summary>
    public List<string> SpecialEffectDescriptions { get; set; } = new();

    /// <summary>
    ///     Whether the blessing is unlocked
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    ///     Whether the blessing can be unlocked
    /// </summary>
    public bool CanUnlock { get; set; }

    /// <summary>
    ///     Reason why blessing cannot be unlocked (if applicable)
    ///     e.g., "Requires Initiate rank" or "Unlock 'Warrior's Resolve' first"
    /// </summary>
    public string UnlockBlockReason { get; set; } = string.Empty;

    /// <summary>
    ///     Create tooltip data from a Blessing and BlessingNodeState
    /// </summary>
    public static BlessingTooltipData FromBlessingAndState(Blessing blessing, BlessingNodeState state,
        Dictionary<string, Blessing>? blessingRegistry = null)
    {
        var tooltip = new BlessingTooltipData
        {
            Name = blessing.Name,
            Description = blessing.Description,
            Category = blessing.Category,
            Kind = blessing.Kind,
            Tier = state.Tier,
            IsUnlocked = state.IsUnlocked,
            CanUnlock = state.CanUnlock
        };

        // Add requirement text based on blessing kind
        if (blessing.Kind == BlessingKind.Player)
            tooltip.RequiredFavorRank = GetFavorRankName(blessing.RequiredFavorRank);
        else if (blessing.Kind == BlessingKind.Religion)
            tooltip.RequiredPrestigeRank = GetPrestigeRankName(blessing.RequiredPrestigeRank);

        // Add prerequisite names
        if (blessingRegistry != null && blessing.PrerequisiteBlessings is { Count: > 0 })
            foreach (var prereqId in blessing.PrerequisiteBlessings)
                if (blessingRegistry.TryGetValue(prereqId, out var prereqBlessing))
                    tooltip.PrerequisiteNames.Add(prereqBlessing.Name);

        // Format stat modifiers
        foreach (var stat in blessing.StatModifiers)
            tooltip.FormattedStats.Add(FormatStatModifier(stat.Key, stat.Value));

        // Add special effects
        tooltip.SpecialEffectDescriptions.AddRange(blessing.SpecialEffects);

        // Determine unlock block reason
        // Note: Don't set UnlockBlockReason for prerequisites since they're already displayed in PrerequisiteNames
        if (state is { IsUnlocked: false, CanUnlock: false })
            // Only set unlock block reason if there are no prerequisites (they're shown separately)
            if (blessing.PrerequisiteBlessings is not { Count: > 0 })
            {
                if (blessing.Kind == BlessingKind.Player)
                    tooltip.UnlockBlockReason = $"Requires {tooltip.RequiredFavorRank} rank";
                else
                    tooltip.UnlockBlockReason = $"Requires religion {tooltip.RequiredPrestigeRank} rank";
            }

        return tooltip;
    }

    /// <summary>
    ///     Format a stat modifier for display
    /// </summary>
    private static string FormatStatModifier(string statName, float value)
    {
        // Normalize stat name to lowercase for matching
        var statLower = statName.ToLower();

        // Check if this is a percentage stat (matching BlessingInfoRenderer.cs logic)
        var percentageStats = new[]
        {
            "walkspeed",
            "meleeDamage",
            "meleeweaponsdamage",
            "rangedDamage",
            "rangedweaponsdamage",
            "maxhealthExtraMultiplier",
            "maxhealthextramultiplier"
        };

        var isPercentage = false;
        foreach (var percentStat in percentageStats)
            if (statLower.Contains(percentStat.ToLower()))
            {
                isPercentage = true;
                break;
            }

        var displayName = FormatStatName(statName);
        var sign = value >= 0 ? "+" : "";

        if (isPercentage) return $"{sign}{value * 100:0.#}% {displayName}";

        return $"{sign}{value:0.#} {displayName}";
    }

    /// <summary>
    ///     Convert stat name to readable format
    /// </summary>
    private static string FormatStatName(string statName)
    {
        // Normalize to lowercase for matching (matching BlessingInfoRenderer.cs logic)
        var statLower = statName.ToLower();

        return statLower switch
        {
            var s when s.Contains("walkspeed") => "Movement Speed",
            var s when s.Contains("meleeDamage") || s.Contains("meleeweaponsdamage") => "Melee Damage",
            var s when s.Contains("rangedDamage") || s.Contains("rangedweaponsdamage") => "Ranged Damage",
            var s when s.Contains("maxhealth") && s.Contains("multiplier") => "Max Health",
            var s when s.Contains("maxhealth") && s.Contains("points") => "Max Health",
            var s when s.Contains("maxhealth") => "Max Health",
            var s when s.Contains("armor") => "Armor",
            var s when s.Contains("speed") => "Speed",
            var s when s.Contains("damage") => "Damage",
            var s when s.Contains("health") => "Health",
            var s when s.Contains("healingeffectivness") => "Healing Effectiveness",
            _ => statName
        };
    }

    /// <summary>
    ///     Get favor rank name from rank number
    /// </summary>
    private static string GetFavorRankName(int rank)
    {
        return rank switch
        {
            0 => "Initiate",
            1 => "Devoted",
            2 => "Zealot",
            3 => "Champion",
            4 => "Exalted",
            _ => $"Rank {rank}"
        };
    }

    /// <summary>
    ///     Get prestige rank name from rank number
    /// </summary>
    private static string GetPrestigeRankName(int rank)
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