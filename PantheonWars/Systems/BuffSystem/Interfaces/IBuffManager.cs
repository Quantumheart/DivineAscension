using System.Collections.Generic;
using Vintagestory.API.Common;

namespace PantheonWars.Systems.BuffSystem.Interfaces;

/// <summary>
///     Interface for applying and removing buffs/debuffs
/// </summary>
public interface IBuffManager
{
    /// <summary>
    ///     Apply a buff or debuff to an entity
    /// </summary>
    bool ApplyEffect(
        EntityAgent target,
        string effectId,
        float duration,
        string sourceAbilityId,
        string casterPlayerUID,
        Dictionary<string, float> statModifiers,
        bool isBuff = true);

    /// <summary>
    ///     Apply a simple buff with a single stat modifier
    /// </summary>
    bool ApplySimpleBuff(
        EntityAgent target,
        string effectId,
        float duration,
        string sourceAbilityId,
        string casterPlayerUID,
        string statName,
        float statValue);

    /// <summary>
    ///     Remove a specific effect from an entity
    /// </summary>
    bool RemoveEffect(EntityAgent target, string effectId);

    /// <summary>
    ///     Check if an entity has a specific effect active
    /// </summary>
    bool HasEffect(EntityAgent target, string effectId);

    /// <summary>
    ///     Get all active effects on an entity
    /// </summary>
    List<ActiveEffect> GetActiveEffects(EntityAgent target);

    /// <summary>
    ///     Helper: Apply a damage boost buff to nearby allies (e.g., War Banner)
    /// </summary>
    int ApplyAoEBuff(
        EntityAgent caster,
        string effectId,
        float duration,
        string sourceAbilityId,
        float radius,
        Dictionary<string, float> statModifiers,
        bool affectCaster = true);

    /// <summary>
    ///     Helper: Get damage multiplier from an entity's buffs (for outgoing damage)
    /// </summary>
    float GetOutgoingDamageMultiplier(EntityAgent entity);

    /// <summary>
    ///     Helper: Get damage multiplier for incoming damage (debuffs like Hunter's Mark)
    /// </summary>
    float GetReceivedDamageMultiplier(EntityAgent entity);
}