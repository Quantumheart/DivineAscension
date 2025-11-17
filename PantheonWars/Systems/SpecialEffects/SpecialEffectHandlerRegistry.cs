using System;
using System.Collections.Generic;
using PantheonWars.Systems.SpecialEffects.Handlers;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.SpecialEffects;

/// <summary>
/// Central registry for all special effect handlers.
/// Manages handler lifecycle and provides lookup by effect ID.
/// </summary>
public class SpecialEffectHandlerRegistry(ICoreServerAPI api)
{
    private readonly Dictionary<string, ISpecialEffectHandler> _handlers = new();
    private readonly ICoreServerAPI _api = api ?? throw new ArgumentNullException(nameof(api));

    /// <summary>
    /// Initializes the registry and registers all effect handlers.
    /// Call this during mod initialization.
    /// </summary>
    public void Initialize()
    {
        RegisterAllHandlers();

        _api.Logger.Notification($"[PantheonWars] Registered {_handlers.Count} special effect handlers");
    }

    /// <summary>
    /// Registers all special effect handlers.
    /// Add new handler registrations here as effects are implemented.
    /// </summary>
    private void RegisterAllHandlers()
    {
        // Combat - Damage Enhancement
        // (Lifesteal handlers will be added in next iteration)
        // RegisterHandler(new LifestealHandler("lifesteal3", 0.03f));
        // RegisterHandler(new LifestealHandler("lifesteal10", 0.10f));
        // RegisterHandler(new LifestealHandler("lifesteal15", 0.15f));
        // RegisterHandler(new LifestealHandler("lifesteal20", 0.20f));

        // (Critical strike handlers will be added in next iteration)
        // RegisterHandler(new CriticalStrikeHandler("critical_chance10", 0.10f, 2.0f));
        // RegisterHandler(new CriticalStrikeHandler("critical_chance20", 0.20f, 2.0f));

        // Defense - Damage Reduction
        RegisterHandler(new DamageReductionHandler("damage_reduction10", 0.10f));

        // Combat - DoT Effects
        // (Poison handlers will be added in next iteration)
        // RegisterHandler(new PoisonDotHandler("poison_dot", 2.0f, 5.0f));
        // RegisterHandler(new PoisonDotHandler("poison_dot_strong", 5.0f, 8.0f));

        // TODO: Add remaining handlers as they are implemented
        // Priority order (from special_effects_implementation_plan.md):
        // 1. Critical: lifesteal, damage_reduction (DONE), critical_strike, execute_threshold
        // 2. High: poison_dot, stealth_bonus, aoe_cleave, multishot
        // 3. Medium: tracking_vision, animal_companion, war_cry, plague_aura
        // 4. Low: death_aura, headshot_bonus, pack_tracking, death_mark
    }

    /// <summary>
    /// Registers a single handler instance.
    /// </summary>
    /// <param name="handler">The handler to register</param>
    private void RegisterHandler(ISpecialEffectHandler handler)
    {
        if (_handlers.ContainsKey(handler.EffectId))
        {
            _api.Logger.Warning($"[PantheonWars] Duplicate handler registration for effect '{handler.EffectId}' - skipping");
            return;
        }

        _handlers[handler.EffectId] = handler;
        _api.Logger.Debug($"[PantheonWars] Registered handler for effect '{handler.EffectId}'");
    }

    /// <summary>
    /// Gets a handler by effect ID.
    /// </summary>
    /// <param name="effectId">The effect ID (e.g., "lifesteal10", "critical_strike")</param>
    /// <returns>The handler if found, null otherwise</returns>
    public ISpecialEffectHandler? GetHandler(string effectId)
    {
        return _handlers.GetValueOrDefault(effectId);
    }

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    /// <returns>Collection of all registered handlers</returns>
    public IEnumerable<ISpecialEffectHandler> GetAllHandlers()
    {
        return _handlers.Values;
    }

    /// <summary>
    /// Checks if a handler is registered for the given effect ID.
    /// </summary>
    /// <param name="effectId">The effect ID to check</param>
    /// <returns>True if handler is registered, false otherwise</returns>
    public bool HasHandler(string effectId)
    {
        return _handlers.ContainsKey(effectId);
    }

    /// <summary>
    /// Gets handlers for multiple effect IDs.
    /// Useful for loading all effects for a player's blessings.
    /// </summary>
    /// <param name="effectIds">Collection of effect IDs</param>
    /// <returns>Collection of handlers (skips missing handlers)</returns>
    public List<ISpecialEffectHandler> GetHandlers(IEnumerable<string> effectIds)
    {
        var handlers = new List<ISpecialEffectHandler>();

        foreach (var effectId in effectIds)
        {
            var handler = GetHandler(effectId);
            if (handler != null)
            {
                handlers.Add(handler);
            }
            else
            {
                _api.Logger.Debug($"[PantheonWars] No handler found for effect '{effectId}' - may not be implemented yet");
            }
        }

        return handlers;
    }
}
