using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace PantheonWars.Systems.BuffSystem;

/// <summary>
///     Represents an active buff or debuff effect on an entity
/// </summary>
public class ActiveEffect
{
    public ActiveEffect()
    {
        StatModifiers = new Dictionary<string, float>();
        ApplicationTime = DateTime.UtcNow;
    }

    /// <summary>
    ///     Constructor with parameters
    /// </summary>
    public ActiveEffect(string effectId, float duration, string sourceAbilityId, string casterPlayerUID,
        bool isBuff = true)
    {
        EffectId = effectId;
        DurationRemaining = duration;
        SourceAbilityId = sourceAbilityId;
        CasterPlayerUID = casterPlayerUID;
        IsBuff = isBuff;
        StatModifiers = new Dictionary<string, float>();
        ApplicationTime = DateTime.UtcNow;
    }

    /// <summary>
    ///     Unique identifier for this effect (e.g., "war_banner_buff", "hunters_mark_debuff")
    /// </summary>
    public string? EffectId { get; set; }

    /// <summary>
    ///     How much time remaining on this effect in seconds
    /// </summary>
    public float DurationRemaining { get; set; }

    /// <summary>
    ///     The source ability that applied this effect
    /// </summary>
    public string? SourceAbilityId { get; set; }

    /// <summary>
    ///     The player who cast this effect (for debuffs and team buffs)
    /// </summary>
    public string? CasterPlayerUID { get; set; }

    /// <summary>
    ///     Stat modifiers applied by this effect
    ///     Key: stat name (walkspeed, meleeWeaponsDamage, etc.)
    ///     Value: modifier value
    /// </summary>
    public Dictionary<string, float> StatModifiers { get; set; }

    /// <summary>
    ///     When this effect was applied (game time)
    /// </summary>
    public DateTime ApplicationTime { get; set; }

    /// <summary>
    ///     Whether this is a buff (positive) or debuff (negative)
    /// </summary>
    public bool IsBuff { get; set; }

    /// <summary>
    ///     Add a stat modifier to this effect
    /// </summary>
    public void AddStatModifier(string statName, float value)
    {
        StatModifiers[statName] = value;
    }

    /// <summary>
    ///     Check if the effect has expired
    /// </summary>
    public bool IsExpired()
    {
        return DurationRemaining <= 0;
    }

    /// <summary>
    ///     Update the remaining duration
    /// </summary>
    /// <param name="deltaTime">Time elapsed in seconds</param>
    public void UpdateDuration(float deltaTime)
    {
        DurationRemaining -= deltaTime;
    }

    /// <summary>
    ///     Serialize to tree attribute for persistence
    /// </summary>
    public ITreeAttribute ToTreeAttribute()
    {
        var tree = new TreeAttribute();
        tree.SetString("effectId", EffectId);
        tree.SetFloat("durationRemaining", DurationRemaining);
        tree.SetString("sourceAbilityId", SourceAbilityId ?? "");
        tree.SetString("casterPlayerUID", CasterPlayerUID ?? "");
        tree.SetBool("isBuff", IsBuff);
        tree.SetLong("applicationTime", ApplicationTime.Ticks);

        // Serialize stat modifiers
        var modifiersTree = new TreeAttribute();
        foreach (var kvp in StatModifiers) modifiersTree.SetFloat(kvp.Key, kvp.Value);
        tree["statModifiers"] = modifiersTree;

        return tree;
    }

    /// <summary>
    ///     Deserialize from tree attribute
    /// </summary>
    public static ActiveEffect FromTreeAttribute(ITreeAttribute? tree)
    {
        if (tree == null) return null!;

        var effect = new ActiveEffect
        {
            EffectId = tree.GetString("effectId"),
            DurationRemaining = tree.GetFloat("durationRemaining"),
            SourceAbilityId = tree.GetString("sourceAbilityId"),
            CasterPlayerUID = tree.GetString("casterPlayerUID"),
            IsBuff = tree.GetBool("isBuff", true),
            ApplicationTime = new DateTime(tree.GetLong("applicationTime"))
        };

        // Deserialize stat modifiers
        var modifiersTree = tree.GetTreeAttribute("statModifiers");
        if (modifiersTree != null && modifiersTree is TreeAttribute treeAttr)
            foreach (var key in treeAttr.Keys)
                effect.StatModifiers[key] = modifiersTree.GetFloat(key);

        return effect;
    }
}