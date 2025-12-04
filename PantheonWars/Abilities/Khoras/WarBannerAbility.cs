using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Abilities.Khoras;

/// <summary>
///     War Banner - Temporary damage boost for nearby allies
/// </summary>
[ExcludeFromCodeCoverage]
public class WarBannerAbility : Ability
{
    private const float RANGE = 10f;
    private const float DURATION = 15f;
    private const float DAMAGE_BOOST = 0.2f; // 20% damage increase (additive)

    public WarBannerAbility() : base(
        "khoras_warbanner",
        "War Banner",
        $"Raises a war banner, granting nearby allies +20% damage for {DURATION} seconds.",
        DeityType.Khoras,
        AbilityType.Buff)
    {
        CooldownSeconds = 45f;
        FavorCost = 15;
        MinimumRank = DevotionRank.Initiate;
    }

    public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
    {
        var casterEntity = caster.Entity;
        if (casterEntity == null) return false;

        // If BuffManager not available, fall back to MVP behavior
        if (buffManager == null)
        {
            caster.SendMessage(
                GlobalConstants.GeneralChatGroup,
                "[War Banner] Buff system not available (MVP mode)",
                EnumChatType.Notification
            );
            return true;
        }

        // Find nearby allies (including self)
        var nearbyEntities = sapi.World.GetEntitiesAround(
            casterEntity.Pos.XYZ,
            RANGE,
            RANGE,
            entity => entity is EntityPlayer
        );

        // Define stat modifiers for the buff
        var statModifiers = new Dictionary<string, float>
        {
            { "meleeDamageMultiplier", DAMAGE_BOOST },
            { "rangedDamageMultiplier", DAMAGE_BOOST }
        };

        var affectedCount = 0;
        foreach (var entity in nearbyEntities)
            if (entity is EntityPlayer playerEntity)
            {
                // Apply the buff using BuffManager
                var success = buffManager.ApplyEffect(
                    playerEntity,
                    "war_banner_buff",
                    DURATION,
                    Id,
                    caster.PlayerUID,
                    statModifiers
                );

                if (success)
                {
                    affectedCount++;

                    var player = sapi.World.PlayerByUid(playerEntity.PlayerUID) as IServerPlayer;
                    if (player != null)
                        player.SendMessage(
                            GlobalConstants.GeneralChatGroup,
                            "[War Banner] You feel empowered by Khoras's blessing! (+20% damage)",
                            EnumChatType.Notification
                        );
                }
            }

        // Notify caster
        caster.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[War Banner] You raise the banner of war, empowering {affectedCount} allies for {DURATION} seconds!",
            EnumChatType.Notification
        );

        sapi.Logger.Debug($"[PantheonWars] {caster.PlayerName} used War Banner, affecting {affectedCount} entities");
        return affectedCount > 0;
    }
}