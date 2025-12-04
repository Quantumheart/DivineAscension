using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Abilities.Khoras;

/// <summary>
///     Blade Storm - Spin attack dealing damage to nearby enemies
/// </summary>
[ExcludeFromCodeCoverage]
public class BladeStormAbility : Ability
{
    private const float RANGE = 4f;
    private const float DAMAGE = 5f;

    public BladeStormAbility() : base(
        "khoras_blade_storm",
        "Blade Storm",
        $"Spin in a deadly circle, dealing {DAMAGE} damage to all enemies within {RANGE} blocks.",
        DeityType.Khoras,
        AbilityType.Damage)
    {
        CooldownSeconds = 20f;
        FavorCost = 12;
        MinimumRank = DevotionRank.Initiate;
    }

    public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null) return false;

        // Find nearby entities
        var nearbyEntities = sapi.World.GetEntitiesAround(
            casterEntity.Pos.XYZ,
            RANGE,
            RANGE,
            entity => entity != casterEntity && entity is EntityAgent
        );

        var hitCount = 0;
        foreach (var entity in nearbyEntities)
            if (entity is EntityAgent agent && agent.Alive)
            {
                // Apply damage
                var damageSource = new DamageSource
                {
                    Source = EnumDamageSource.Player,
                    SourceEntity = casterEntity,
                    Type = EnumDamageType.PiercingAttack,
                    DamageTier = 0
                };

                agent.ReceiveDamage(damageSource, DAMAGE);
                hitCount++;
            }

        // Notify caster
        caster.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Blade Storm] You spin in a whirlwind of steel, striking {hitCount} enemies!",
            EnumChatType.Notification
        );

        // Notify nearby players
        var nearbyPlayers = sapi.World.GetPlayersAround(
            casterEntity.Pos.XYZ,
            15f,
            15f
        );

        foreach (var player in nearbyPlayers)
            if (player.PlayerUID != caster.PlayerUID)
                (player as IServerPlayer)?.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    $"[Blade Storm] {caster.PlayerName} unleashes a devastating blade storm!",
                    EnumChatType.Notification
                );

        sapi.Logger.Debug($"[PantheonWars] {caster.PlayerName} used Blade Storm, hit {hitCount} targets");
        return true;
    }
}