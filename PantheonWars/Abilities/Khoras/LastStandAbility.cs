using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PantheonWars.Abilities.Khoras;

/// <summary>
///     Last Stand - Temporary damage resistance when health is low
/// </summary>
[ExcludeFromCodeCoverage]
public class LastStandAbility : Ability
{
    private const float DURATION = 12f;
    private const float DAMAGE_REDUCTION = 0.5f; // Multiply incoming damage by 0.5 (50% reduction)

    public LastStandAbility() : base(
        "khoras_last_stand",
        "Last Stand",
        $"Channel Khoras's indomitable will, gaining 50% damage resistance for {DURATION} seconds. (Requires <30% health)",
        DeityType.Khoras,
        AbilityType.Defensive)
    {
        CooldownSeconds = 60f;
        FavorCost = 20;
        MinimumRank = DevotionRank.Disciple;
    }

    public override bool CanExecute(IServerPlayer caster, ICoreServerAPI sapi, out string failureReason)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null)
        {
            failureReason = "Entity not found";
            return false;
        }

        // Require low health to use (30% or less)
        var healthBehavior = casterEntity.GetBehavior<EntityBehaviorHealth>();
        if (healthBehavior == null)
        {
            failureReason = "Health system not found";
            return false;
        }

        var healthPercent = healthBehavior.Health / healthBehavior.MaxHealth;
        if (healthPercent > 0.3f)
        {
            failureReason =
                $"Last Stand can only be used when your health is below 30% (current: {healthPercent * 100:F0}%)";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null) return false;

        // Apply damage resistance buff
        if (buffManager != null)
        {
            var statModifiers = new Dictionary<string, float>
            {
                { "receivedDamageMultiplier", DAMAGE_REDUCTION }
            };

            buffManager.ApplyEffect(
                casterEntity,
                "last_stand_buff",
                DURATION,
                Id,
                caster.PlayerUID,
                statModifiers
            );
        }

        caster.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Last Stand] You refuse to fall! Khoras grants you unbreakable resolve for {DURATION} seconds! (50% damage resistance)",
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
                    $"[Last Stand] {caster.PlayerName} stands defiant, refusing to yield!",
                    EnumChatType.Notification
                );

        sapi.Logger.Debug($"[PantheonWars] {caster.PlayerName} used Last Stand");
        return true;
    }
}