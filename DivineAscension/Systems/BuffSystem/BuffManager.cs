using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BuffSystem;

/// <summary>
///     Central manager for applying and removing buffs/debuffs
/// </summary>
public class BuffManager : IBuffManager
{
    private readonly ILogger _logger;
    private readonly IWorldService _worldService;

    public BuffManager(ILogger logger, IWorldService worldService)
    {
        _logger = logger;
        _worldService = worldService;
    }

    /// <summary>
    ///     Apply a buff or debuff to an entity
    /// </summary>
    /// <param name="target">The entity to apply the effect to</param>
    /// <param name="effectId">Unique ID for this effect</param>
    /// <param name="duration">Duration in seconds</param>
    /// <param name="sourceAbilityId">The ability that applied this effect</param>
    /// <param name="casterPlayerUID">The player who cast the ability</param>
    /// <param name="statModifiers">Dictionary of stat modifications</param>
    /// <param name="isBuff">True for buffs, false for debuffs</param>
    /// <returns>True if successfully applied</returns>
    public bool ApplyEffect(
        EntityAgent target,
        string effectId,
        float duration,
        string sourceAbilityId,
        string casterPlayerUID,
        Dictionary<string, float>? statModifiers,
        bool isBuff = true)
    {
        if (target == null || string.IsNullOrEmpty(effectId)) return false;

        // Get or create the buff tracker behavior
        var buffTracker = GetOrCreateBuffTracker(target);
        if (buffTracker == null) return false;

        // Create the effect
        var effect = new ActiveEffect(effectId, duration, sourceAbilityId, casterPlayerUID, isBuff);

        // Add stat modifiers
        if (statModifiers is null)
            return false;

        foreach (var modifier in statModifiers)
            effect.AddStatModifier(modifier.Key, modifier.Value);

        // Apply the effect
        buffTracker.AddEffect(effect);

        _logger.Debug($"[DivineAscension] Applied effect {effectId} to {target.GetName()} for {duration}s");

        return true;
    }

    /// <summary>
    ///     Apply a simple buff with a single stat modifier
    /// </summary>
    public bool ApplySimpleBuff(
        EntityAgent target,
        string effectId,
        float duration,
        string sourceAbilityId,
        string casterPlayerUID,
        string statName,
        float statValue)
    {
        var modifiers = new Dictionary<string, float>
        {
            { statName, statValue }
        };

        return ApplyEffect(target, effectId, duration, sourceAbilityId, casterPlayerUID, modifiers);
    }

    /// <summary>
    ///     Remove a specific effect from an entity
    /// </summary>
    public bool RemoveEffect(EntityAgent target, string effectId)
    {
        if (target == null || string.IsNullOrEmpty(effectId)) return false;

        var buffTracker = target.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffTracker == null) return false;

        return buffTracker.RemoveEffect(effectId);
    }

    /// <summary>
    ///     Check if an entity has a specific effect active
    /// </summary>
    public bool HasEffect(EntityAgent target, string effectId)
    {
        if (target == null) return false;

        var buffTracker = target.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffTracker == null) return false;

        return buffTracker.HasEffect(effectId);
    }

    /// <summary>
    ///     Get all active effects on an entity
    /// </summary>
    public List<ActiveEffect> GetActiveEffects(EntityAgent target)
    {
        if (target == null) return new List<ActiveEffect>();

        var buffTracker = target.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffTracker == null) return new List<ActiveEffect>();

        return buffTracker.GetActiveEffects();
    }

    /// <summary>
    ///     Helper: Apply a damage boost buff to nearby allies (e.g., War Banner)
    /// </summary>
    public int ApplyAoEBuff(
        EntityAgent caster,
        string effectId,
        float duration,
        string sourceAbilityId,
        float radius,
        Dictionary<string, float> statModifiers,
        bool affectCaster = true)
    {
        var affectedCount = 0;

        // Find all entities in radius
        _worldService.World.GetNearestEntity(caster.ServerPos.XYZ, radius, radius, entity =>
        {
            // Check if it's a player entity
            if (entity is EntityPlayer player)
            {
                // Skip caster if specified
                if (!affectCaster && player.EntityId == caster.EntityId) return false;

                // Apply buff
                var casterUID = caster is EntityPlayer casterPlayer ? casterPlayer.PlayerUID : "";
                if (ApplyEffect(player, effectId, duration, sourceAbilityId, casterUID, statModifiers))
                {
                    affectedCount++;

                    // Notify the player
                    if (player.Player is IServerPlayer serverPlayer)
                        serverPlayer.SendMessage(
                            GlobalConstants.InfoLogChatGroup,
                            $"You are affected by [{effectId}]!",
                            EnumChatType.Notification
                        );
                }
            }

            return false; // Continue searching
        });

        return affectedCount;
    }

    /// <summary>
    ///     Helper: Get damage multiplier from an entity's buffs (for outgoing damage)
    /// </summary>
    public float GetOutgoingDamageMultiplier(EntityAgent entity)
    {
        if (entity == null) return 1.0f;

        var buffTracker = entity.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffTracker == null) return 1.0f;

        return buffTracker.GetOutgoingDamageMultiplier();
    }

    /// <summary>
    ///     Helper: Get damage multiplier for incoming damage (debuffs like Hunter's Mark)
    /// </summary>
    public float GetReceivedDamageMultiplier(EntityAgent entity)
    {
        if (entity == null) return 1.0f;

        var buffTracker = entity.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffTracker == null) return 1.0f;

        return buffTracker.GetReceivedDamageMultiplier();
    }

    /// <summary>
    ///     Get or create a buff tracker behavior on an entity
    /// </summary>
    private EntityBehaviorBuffTracker GetOrCreateBuffTracker(EntityAgent target)
    {
        // Try to get existing behavior
        var buffTracker = target.GetBehavior<EntityBehaviorBuffTracker>();

        // If it doesn't exist, create and add it
        if (buffTracker == null)
        {
            buffTracker = new EntityBehaviorBuffTracker(target);
            target.AddBehavior(buffTracker);

            // Initialize the behavior
            try
            {
                buffTracker.Initialize(target.Properties, null!);
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Failed to initialize BuffTracker: {ex.Message}");
                return null!;
            }
        }

        return buffTracker;
    }
}