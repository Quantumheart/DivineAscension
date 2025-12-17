using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.BuffSystem;

/// <summary>
///     Entity behavior that tracks active buffs/debuffs and applies their stat modifications
/// </summary>
public class EntityBehaviorBuffTracker : EntityBehavior
{
    private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds
    private readonly List<ActiveEffect> activeEffects = new();

    private readonly Dictionary<string, float> lastAppliedModifiers = new();
    private float timeSinceLastUpdate;

    public EntityBehaviorBuffTracker(Entity entity) : base(entity)
    {
    }

    public override string PropertyName()
    {
        return "PantheonWarsBuffTracker";
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);

        // Hook into damage events to apply damage modifiers
        var behaviorHealth = entity.GetBehavior<EntityBehaviorHealth>();
        if (behaviorHealth != null) behaviorHealth.onDamaged += OnEntityDamaged;

        // Load active effects from entity attributes if they exist
        LoadEffectsFromAttributes();
    }

    public override void OnGameTick(float deltaTime)
    {
        base.OnGameTick(deltaTime);

        timeSinceLastUpdate += deltaTime;

        if (timeSinceLastUpdate >= UPDATE_INTERVAL)
        {
            UpdateEffects(timeSinceLastUpdate);
            timeSinceLastUpdate = 0f;
        }
    }

    /// <summary>
    ///     Add a new effect to this entity
    /// </summary>
    public virtual void AddEffect(ActiveEffect effect)
    {
        if (effect == null) return;

        // Check if effect already exists - if so, refresh duration
        var existing = activeEffects.FirstOrDefault(e => e.EffectId == effect.EffectId);
        if (existing != null)
        {
            // Refresh duration and update modifiers
            existing.DurationRemaining = effect.DurationRemaining;
            existing.StatModifiers = effect.StatModifiers;
        }
        else
        {
            activeEffects.Add(effect);
        }

        // Apply stat modifiers immediately
        ApplyAllStatModifiers();

        // Persist effects
        SaveEffectsToAttributes();
    }

    /// <summary>
    ///     Remove an effect by ID
    /// </summary>
    public virtual bool RemoveEffect(string effectId)
    {
        var effect = activeEffects.FirstOrDefault(e => e.EffectId == effectId);
        if (effect != null)
        {
            activeEffects.Remove(effect);

            // Reapply all stat modifiers (this will remove the old ones)
            ApplyAllStatModifiers();

            // Persist changes
            SaveEffectsToAttributes();

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Get all active effects on this entity
    /// </summary>
    public virtual List<ActiveEffect> GetActiveEffects()
    {
        return [..activeEffects];
    }

    /// <summary>
    ///     Check if entity has a specific effect active
    /// </summary>
    public virtual bool HasEffect(string effectId)
    {
        return activeEffects.Any(e => e.EffectId == effectId);
    }

    /// <summary>
    ///     Update all active effects (decrement duration, remove expired)
    /// </summary>
    private void UpdateEffects(float deltaTime)
    {
        if (activeEffects.Count == 0) return;

        var expiredEffects = new List<ActiveEffect>();

        // Update durations
        foreach (var effect in activeEffects)
        {
            effect.UpdateDuration(deltaTime);

            if (effect.IsExpired()) expiredEffects.Add(effect);
        }

        // Remove expired effects
        if (expiredEffects.Count > 0)
        {
            foreach (var expired in expiredEffects)
            {
                activeEffects.Remove(expired);

                // Notify player
                if (entity is EntityPlayer player && player.Player is IServerPlayer serverPlayer)
                    serverPlayer.SendMessage(
                        GlobalConstants.InfoLogChatGroup,
                        $"[{expired.EffectId}] effect has worn off.",
                        EnumChatType.Notification
                    );
            }

            // Reapply stat modifiers after removing expired effects
            ApplyAllStatModifiers();

            // Persist changes
            SaveEffectsToAttributes();
        }
    }

    /// <summary>
    ///     Apply all active stat modifiers to entity stats
    /// </summary>
    private void ApplyAllStatModifiers()
    {
        if (entity.Stats == null) return;

        // First, remove all previously applied modifiers
        foreach (var kvp in lastAppliedModifiers)
        {
            var statName = kvp.Key;
            entity.Stats.Remove(statName, "pantheonwars-buff");
        }

        lastAppliedModifiers.Clear();

        // Now accumulate all active modifiers
        var accumulatedModifiers = new Dictionary<string, float>();

        foreach (var effect in activeEffects)
        foreach (var modifier in effect.StatModifiers)
        {
            if (!accumulatedModifiers.ContainsKey(modifier.Key)) accumulatedModifiers[modifier.Key] = 0f;

            // Stack modifiers additively
            accumulatedModifiers[modifier.Key] += modifier.Value;
        }

        // Apply accumulated modifiers
        foreach (var modifier in accumulatedModifiers)
        {
            entity.Stats.Set(modifier.Key, "pantheonwars-buff", modifier.Value);
            lastAppliedModifiers[modifier.Key] = modifier.Value;
        }
    }

    /// <summary>
    ///     Modify incoming damage based on active effects
    /// </summary>
    private float OnEntityDamaged(float damage, DamageSource dmgSource)
    {
        if (activeEffects.Count == 0) return damage;

        var damageMultiplier = 1.0f;

        // Check for damage resistance buffs
        foreach (var effect in activeEffects)
            if (effect.StatModifiers.ContainsKey("receivedDamageMultiplier"))
                // receivedDamageMultiplier is a multiplier (1.0 = normal, 0.5 = 50% reduction)
                damageMultiplier *= effect.StatModifiers["receivedDamageMultiplier"];

        return damage * damageMultiplier;
    }

    /// <summary>
    ///     Get the total damage multiplier this entity should deal (for buffs like War Banner)
    /// </summary>
    public virtual float GetOutgoingDamageMultiplier()
    {
        var multiplier = 1.0f;

        foreach (var effect in activeEffects)
        {
            if (effect.StatModifiers.ContainsKey("meleeDamageMultiplier"))
                multiplier += effect.StatModifiers["meleeDamageMultiplier"];

            if (effect.StatModifiers.ContainsKey("rangedDamageMultiplier"))
                multiplier += effect.StatModifiers["rangedDamageMultiplier"];
        }

        return multiplier;
    }

    /// <summary>
    ///     Check if entity is marked (for Hunter's Mark debuff)
    /// </summary>
    public virtual float GetReceivedDamageMultiplier()
    {
        var multiplier = 1.0f;

        foreach (var effect in activeEffects)
            if (effect.StatModifiers.ContainsKey("receivedDamageAmplification"))
                // This is for debuffs like Hunter's Mark that make target take MORE damage
                multiplier += effect.StatModifiers["receivedDamageAmplification"];

        return multiplier;
    }

    /// <summary>
    ///     Save active effects to entity attributes for persistence
    /// </summary>
    private void SaveEffectsToAttributes()
    {
        var effectsTree = new TreeAttribute();
        effectsTree.SetInt("count", activeEffects.Count);

        for (var i = 0; i < activeEffects.Count; i++) effectsTree[$"effect_{i}"] = activeEffects[i].ToTreeAttribute();

        entity.WatchedAttributes["pantheonwars_effects"] = effectsTree;
        entity.WatchedAttributes.MarkPathDirty("pantheonwars_effects");
    }

    /// <summary>
    ///     Load active effects from entity attributes
    /// </summary>
    private void LoadEffectsFromAttributes()
    {
        var effectsTree = entity.WatchedAttributes.GetTreeAttribute("pantheonwars_effects");
        if (effectsTree == null) return;

        var count = effectsTree.GetInt("count");
        activeEffects.Clear();

        for (var i = 0; i < count; i++)
        {
            var effectTree = effectsTree.GetTreeAttribute($"effect_{i}");
            if (effectTree != null)
            {
                var effect = ActiveEffect.FromTreeAttribute(effectTree);
                if (effect != null) activeEffects.Add(effect);
            }
        }

        // Apply loaded effects
        if (activeEffects.Count > 0) ApplyAllStatModifiers();
    }
}