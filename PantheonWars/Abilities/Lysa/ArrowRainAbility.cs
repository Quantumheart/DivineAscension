using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Abilities.Lysa;

/// <summary>
///     Arrow Rain - Ranged AoE attack hitting an area
/// </summary>
[ExcludeFromCodeCoverage]
public class ArrowRainAbility : Ability
{
    private const float RANGE = 25f;
    private const float AOE_RADIUS = 5f;
    private const float DAMAGE = 4f;

    public ArrowRainAbility() : base(
        "lysa_arrow_rain",
        "Arrow Rain",
        $"Call down a rain of arrows in a {AOE_RADIUS} block radius, dealing {DAMAGE} damage to all enemies.",
        DeityType.Lysa,
        AbilityType.Damage)
    {
        CooldownSeconds = 35f;
        FavorCost = 15;
        MinimumRank = DevotionRank.Disciple;
    }

    public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null) return false;

        // Get target location (where player is looking, at range)
        var lookVec = casterEntity.SidedPos.GetViewVector();
        var startPos = casterEntity.SidedPos.XYZ.Add(0, casterEntity.LocalEyePos.Y, 0);
        var targetPos = startPos.Add(lookVec.X * RANGE, lookVec.Y * RANGE, lookVec.Z * RANGE);

        // Find all entities in AoE radius
        var nearbyEntities = sapi.World.GetEntitiesAround(
            targetPos,
            AOE_RADIUS,
            AOE_RADIUS,
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
            $"[Arrow Rain] A deadly hail of arrows rains down, striking {hitCount} enemies!",
            EnumChatType.Notification
        );

        // Notify nearby players
        var nearbyPlayers = sapi.World.GetPlayersAround(
            targetPos,
            20f,
            20f
        );

        foreach (var player in nearbyPlayers)
            if (player.PlayerUID != caster.PlayerUID)
                (player as IServerPlayer)?.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    $"[Arrow Rain] {caster.PlayerName} calls down a rain of arrows!",
                    EnumChatType.Notification
                );

        sapi.Logger.Debug($"[PantheonWars] {caster.PlayerName} used Arrow Rain, hit {hitCount} targets");
        return true;
    }
}