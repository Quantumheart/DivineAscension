using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace PantheonWars.Systems.SpecialEffects;

/// <summary>
/// Interface for all special effect handlers in the blessing system.
/// Handlers are invoked by BlessingEntityBehavior in response to combat and game events.
/// </summary>
public interface ISpecialEffectHandler
{
    /// <summary>
    /// Called when the entity deals damage to another entity.
    /// Use for offensive effects like lifesteal, poison application, critical strikes.
    /// </summary>
    /// <param name="attacker">The entity dealing damage (player with blessing)</param>
    /// <param name="target">The entity receiving damage</param>
    /// <param name="source">Complete damage source information</param>
    /// <param name="damage">The damage amount (can be modified for critical strikes)</param>
    void OnDamageDealt(Entity attacker, Entity target, DamageSource source, ref float damage);

    /// <summary>
    /// Called when the entity receives damage.
    /// Use for defensive effects like damage reduction, shields, thorns.
    /// </summary>
    /// <param name="victim">The entity receiving damage (player with blessing)</param>
    /// <param name="attacker">The entity dealing damage (may be null for environmental damage)</param>
    /// <param name="source">Complete damage source information</param>
    /// <param name="damage">The damage amount (modify to reduce/negate damage)</param>
    void OnDamageReceived(Entity victim, Entity? attacker, DamageSource source, ref float damage);

    /// <summary>
    /// Called every game tick (default ~30 times per second).
    /// Use for tick-based effects like regeneration, buff timers, stealth updates.
    /// </summary>
    /// <param name="entity">The entity with this handler (player with blessing)</param>
    /// <param name="deltaTime">Time since last tick in seconds</param>
    void OnTick(Entity entity, float deltaTime);

    /// <summary>
    /// Called when an entity dies from damage dealt by this entity.
    /// Use for on-kill effects like soul harvest, death aura activation.
    /// </summary>
    /// <param name="killer">The entity that dealt the killing blow (player with blessing)</param>
    /// <param name="victim">The entity that died</param>
    /// <param name="source">The damage source that caused death</param>
    void OnKill(Entity killer, Entity victim, DamageSource source);

    /// <summary>
    /// Unique identifier for this effect type (e.g., "lifesteal10", "critical_strike").
    /// Must match the effect ID in BlessingDefinitions.
    /// </summary>
    string EffectId { get; }

    /// <summary>
    /// Called when this effect is activated for an entity (blessing unlocked/applied).
    /// Use for one-time setup like adding entity behaviors or modifying attributes.
    /// </summary>
    /// <param name="entity">The entity this effect is being activated on</param>
    void OnActivate(Entity entity);

    /// <summary>
    /// Called when this effect is deactivated for an entity (blessing removed/player leaves religion).
    /// Use for cleanup like removing entity behaviors or resetting attributes.
    /// </summary>
    /// <param name="entity">The entity this effect is being deactivated on</param>
    void OnDeactivate(Entity entity);
}
