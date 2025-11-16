using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace PantheonWars.Systems.SpecialEffects.Handlers;

/// <summary>
/// Reduces incoming damage by a fixed percentage.
/// Used by Aethra (Light) and Gaia (Earth) defensive blessings.
/// </summary>
public class DamageReductionHandler : SpecialEffectHandlerBase
{
    private readonly float _reductionPercent;

    public override string EffectId { get; }

    /// <summary>
    /// Creates a damage reduction handler.
    /// </summary>
    /// <param name="effectId">Unique effect identifier (e.g., "damage_reduction10")</param>
    /// <param name="reductionPercent">Damage reduction as a decimal (0.10 = 10% reduction)</param>
    public DamageReductionHandler(string effectId, float reductionPercent)
    {
        EffectId = effectId;
        _reductionPercent = reductionPercent;
    }

    /// <summary>
    /// Reduces incoming damage before it's applied to the entity.
    /// </summary>
    public override void OnDamageReceived(Entity victim, Entity? attacker, DamageSource source, ref float damage)
    {
        // Don't reduce healing - we want players to receive full heals
        if (source.Type == EnumDamageType.Heal)
            return;

        // Don't reduce revive damage (used for full heal on respawn)
        if (source.Source == EnumDamageSource.Revive)
            return;

        // Apply damage reduction
        float originalDamage = damage;
        damage *= (1f - _reductionPercent);

        // Optional: Send feedback to player about damage blocked
        // This could be expanded to show particles or HUD updates
        float damageBlocked = originalDamage - damage;
        if (damageBlocked > 0.1f)
        {
            // TODO: Add visual/audio feedback for significant damage reduction
            // Example: SpawnShieldParticles(victim.Pos.XYZ);
            // Example: SendDamageBlockedMessage(victim, damageBlocked);
        }
    }
}
