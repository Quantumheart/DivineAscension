using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.SpecialEffects;

/// <summary>
/// Entity behavior that applies blessing special effects to player entities.
/// Attached to players when they login and dispatches combat/game events to effect handlers.
/// </summary>
public class BlessingEntityBehavior(Entity entity, SpecialEffectHandlerRegistry handlerRegistry)
    : EntityBehavior(entity)
{
    private List<ISpecialEffectHandler> _activeHandlers = new();
    private readonly SpecialEffectHandlerRegistry _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));

    public override string PropertyName() => "pantheonwars_blessings";

    /// <summary>
    /// Called when entity spawns/loads. Initialize blessing handlers.
    /// </summary>
    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);

        // Note: Handlers will be loaded via LoadHandlers() call from BlessingEffectSystem
        // when player data is available (after player login)
    }

    /// <summary>
    /// Called before entity receives damage. Dispatch to defensive effect handlers.
    /// This is where damage reduction, shields, and other defensive effects trigger.
    /// </summary>
    /// <param name="damageSource">Source of incoming damage</param>
    /// <param name="damage">Damage amount (can be modified)</param>
    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
        if (_activeHandlers.Count == 0) return;

        // Get attacker entity (may be null for environmental damage)
        Entity? attacker = damageSource.GetCauseEntity();

        // Dispatch to all defensive handlers
        foreach (var handler in _activeHandlers)
        {
            handler.OnDamageReceived(entity, attacker, damageSource, ref damage);
        }
    }

    /// <summary>
    /// Called after entity successfully attacks another entity.
    /// This is where lifesteal, poison application, and other on-hit effects trigger.
    /// </summary>
    /// <param name="source">Damage source for the attack</param>
    /// <param name="targetEntity">Entity that was attacked</param>
    /// <param name="handled">Whether to prevent subsequent behaviors</param>
    public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
    {
        if (_activeHandlers.Count == 0) return;
        
        float damageDealt = targetEntity.WatchedAttributes.GetFloat("onHurt", 0f);

        // Only process if damage was actually dealt (not blocked, not zero)
        if (damageDealt > 0)
        {
            // Dispatch to all offensive handlers with the ACTUAL damage value
            foreach (var handler in _activeHandlers)
            {
                handler.OnDamageDealt(entity, targetEntity, source, ref damageDealt);
            }
        }

        // Check if target died from this attack
        if (!targetEntity.Alive)
        {
            foreach (var handler in _activeHandlers)
            {
                handler.OnKill(entity, targetEntity, source);
            }
        }
    }

    /// <summary>
    /// Called every game tick. Dispatch to tick-based effect handlers.
    /// This is where regeneration, buff timers, and other continuous effects update.
    /// </summary>
    /// <param name="deltaTime">Time since last tick in seconds</param>
    public override void OnGameTick(float deltaTime)
    {
        if (_activeHandlers.Count == 0) return;

        // Dispatch to all tick-based handlers
        foreach (var handler in _activeHandlers)
        {
            handler.OnTick(entity, deltaTime);
        }
    }

    /// <summary>
    /// Loads effect handlers for the given effect IDs.
    /// Called by BlessingEffectSystem when player blessings change.
    /// </summary>
    /// <param name="effectIds">Collection of effect IDs from player's blessings</param>
    public void LoadHandlers(IEnumerable<string> effectIds)
    {
        // Deactivate old handlers
        foreach (var handler in _activeHandlers)
        {
            handler.OnDeactivate(entity);
        }

        // Clear old handlers
        _activeHandlers.Clear();

        // Load new handlers from registry
        _activeHandlers = _handlerRegistry.GetHandlers(effectIds);

        // Activate new handlers
        foreach (var handler in _activeHandlers)
        {
            handler.OnActivate(entity);
        }

        // Debug logging
        if (entity.Api.Side.IsServer())
        {
            var api = entity.Api as ICoreServerAPI;
            api?.Logger.Debug($"[PantheonWars] Loaded {_activeHandlers.Count} effect handlers for entity {entity.EntityId}");
        }
    }

    /// <summary>
    /// Clears all active handlers.
    /// Called when player leaves religion or blessing system needs reset.
    /// </summary>
    public void ClearHandlers()
    {
        // Deactivate all handlers
        foreach (var handler in _activeHandlers)
        {
            handler.OnDeactivate(entity);
        }

        _activeHandlers.Clear();
    }

    /// <summary>
    /// Gets the count of currently active handlers.
    /// Useful for debugging and UI display.
    /// </summary>
    public int GetActiveHandlerCount() => _activeHandlers.Count;
}
