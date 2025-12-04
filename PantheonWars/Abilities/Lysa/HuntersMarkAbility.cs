using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Abilities.Lysa;

/// <summary>
///     Hunter's Mark - Mark target to take extra damage
/// </summary>
[ExcludeFromCodeCoverage]
public class HuntersMarkAbility : Ability
{
    private const float RANGE = 20f;
    private const float DURATION = 20f;
    private const float DAMAGE_AMPLIFICATION = 0.25f; // 25% extra damage taken

    public HuntersMarkAbility() : base(
        "lysa_hunters_mark",
        "Hunter's Mark",
        $"Mark your prey. Marked targets take 25% extra damage for {DURATION} seconds.",
        DeityType.Lysa,
        AbilityType.Debuff)
    {
        CooldownSeconds = 25f;
        FavorCost = 10;
        MinimumRank = DevotionRank.Initiate;
    }

    public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null) return false;

        // Find target the player is looking at
        EntityAgent? target = null;
        var lookVec = casterEntity.SidedPos.GetViewVector();
        var startPos = casterEntity.SidedPos.XYZ.Add(0, casterEntity.LocalEyePos.Y, 0);

        // Raycast to find target
        var entities = sapi.World.GetEntitiesAround(
            startPos,
            RANGE,
            RANGE,
            entity => entity != casterEntity && entity is EntityAgent
        );

        var closestDistance = double.MaxValue;
        foreach (var entity in entities)
            if (entity is EntityAgent agent && agent.Alive)
            {
                var toEntity = entity.Pos.XYZ.Sub(startPos).Normalize();
                var dot = lookVec.Dot(toEntity);

                // Check if entity is roughly in the direction player is looking (within 30 degrees)
                if (dot > 0.866) // cos(30°) ≈ 0.866
                {
                    var distance = startPos.DistanceTo(entity.Pos.XYZ);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        target = agent;
                    }
                }
            }

        if (target == null)
        {
            caster.SendMessage(
                GlobalConstants.GeneralChatGroup,
                "[Hunter's Mark] No valid target in range.",
                EnumChatType.Notification
            );
            return false;
        }

        // Apply debuff using BuffManager
        if (buffManager != null)
        {
            var statModifiers = new Dictionary<string, float>
            {
                { "receivedDamageAmplification", DAMAGE_AMPLIFICATION }
            };

            buffManager.ApplyEffect(
                target,
                "hunters_mark_debuff",
                DURATION,
                Id,
                caster.PlayerUID,
                statModifiers,
                false // This is a debuff
            );
        }

        // Notify caster
        var targetName = target is EntityPlayer targetPlayer
            ? sapi.World.PlayerByUid(targetPlayer.PlayerUID)?.PlayerName ?? "Unknown"
            : target.GetName();

        caster.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Hunter's Mark] You mark {targetName}! Target will take 25% extra damage for {DURATION} seconds.",
            EnumChatType.Notification
        );

        // Notify the marked target if they're a player
        if (target is EntityPlayer markedPlayer)
        {
            var player = sapi.World.PlayerByUid(markedPlayer.PlayerUID) as IServerPlayer;
            player?.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Hunter's Mark] You have been marked by {caster.PlayerName}!",
                EnumChatType.Notification
            );
        }

        sapi.Logger.Debug($"[PantheonWars] {caster.PlayerName} marked {targetName} with Hunter's Mark");
        return true;
    }
}