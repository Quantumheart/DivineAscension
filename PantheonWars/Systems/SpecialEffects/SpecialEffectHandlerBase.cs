using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace PantheonWars.Systems.SpecialEffects;

/// <summary>
/// Base class for special effect handlers providing default no-op implementations.
/// Concrete handlers only need to override methods they actually use.
/// </summary>
public abstract class SpecialEffectHandlerBase : ISpecialEffectHandler
{
    /// <summary>
    /// Unique identifier for this effect type.
    /// </summary>
    public abstract string EffectId { get; }

    /// <summary>
    /// Called when entity deals damage. Default implementation does nothing.
    /// </summary>
    public virtual void OnDamageDealt(Entity attacker, Entity target, DamageSource source, ref float damage)
    {
        // Default: no-op
    }

    /// <summary>
    /// Called when entity receives damage. Default implementation does nothing.
    /// </summary>
    public virtual void OnDamageReceived(Entity victim, Entity? attacker, DamageSource source, ref float damage)
    {
        // Default: no-op
    }

    /// <summary>
    /// Called every game tick. Default implementation does nothing.
    /// </summary>
    public virtual void OnTick(Entity entity, float deltaTime)
    {
        // Default: no-op
    }

    /// <summary>
    /// Called when entity kills another entity. Default implementation does nothing.
    /// </summary>
    public virtual void OnKill(Entity killer, Entity victim, DamageSource source)
    {
        // Default: no-op
    }

    /// <summary>
    /// Called when effect is activated. Default implementation does nothing.
    /// </summary>
    public virtual void OnActivate(Entity entity)
    {
        // Default: no-op
    }

    /// <summary>
    /// Called when effect is deactivated. Default implementation does nothing.
    /// </summary>
    public virtual void OnDeactivate(Entity entity)
    {
        // Default: no-op
    }
}
