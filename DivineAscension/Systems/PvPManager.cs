using System;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages PvP interactions for favor and prestige rewards
/// </summary>
public class PvPManager : IPvPManager
{
    private const int BASE_FAVOR_REWARD = 10;
    private const int BASE_PRESTIGE_REWARD = 15;
    private const int DEATH_PENALTY_FAVOR = 5;
    private readonly IDeityRegistry _deityRegistry;
    private readonly IPlayerReligionDataManager _playerReligionDataManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;

    private readonly ICoreServerAPI _sapi;

    public PvPManager(
        ICoreServerAPI sapi,
        IPlayerReligionDataManager playerReligionDataManager,
        IReligionManager religionManager,
        IReligionPrestigeManager prestigeManager,
        IDeityRegistry deityRegistry)
    {
        _sapi = sapi;
        _playerReligionDataManager = playerReligionDataManager;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
        _deityRegistry = deityRegistry;
    }

    /// <summary>
    ///     Initializes the PvP manager and hooks into game events
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing PvP Manager...");

        // Hook into player death event for PvP favor/prestige rewards
        _sapi.Event.PlayerDeath += OnPlayerDeath;

        _sapi.Logger.Notification("[DivineAscension] PvP Manager initialized");
    }

    /// <summary>
    ///     Awards favor and prestige for deity-aligned actions (extensible for future features)
    /// </summary>
    public void AwardRewardsForAction(IServerPlayer player, string actionType, int favorAmount, int prestigeAmount)
    {
        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (playerData.ActiveDeity == DeityType.None || playerData.ReligionUID == null) return;

        // Award favor
        if (favorAmount > 0) _playerReligionDataManager.AddFavor(player.PlayerUID, favorAmount, actionType);

        // Award prestige
        if (prestigeAmount > 0)
            _prestigeManager.AddPrestige(playerData.ReligionUID, prestigeAmount,
                $"{actionType} by {player.PlayerName}");

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Reward] You gained {favorAmount} favor and your religion gained {prestigeAmount} prestige for {actionType}!",
            EnumChatType.Notification
        );
    }

    /// <summary>
    ///     Handles player death and awards/penalizes favor and prestige
    /// </summary>
    private void OnPlayerDeath(IServerPlayer deadPlayer, DamageSource damageSource)
    {
        // Check if death was caused by another player (PvP)
        if (damageSource?.SourceEntity is EntityPlayer attackerEntity)
            if (_sapi.World.PlayerByUid(attackerEntity.PlayerUID) is IServerPlayer attackerPlayer &&
                attackerPlayer != deadPlayer)
                ProcessPvPKill(attackerPlayer, deadPlayer);

        // Apply death penalty
        ProcessDeathPenalty(deadPlayer);
    }

    /// <summary>
    ///     Processes PvP kill and awards favor/prestige
    /// </summary>
    private void ProcessPvPKill(IServerPlayer attacker, IServerPlayer victim)
    {
        var attackerData = _playerReligionDataManager.GetOrCreatePlayerData(attacker.PlayerUID);
        var victimData = _playerReligionDataManager.GetOrCreatePlayerData(victim.PlayerUID);

        // Check if attacker has a religion
        if (attackerData.ReligionUID == null || attackerData.ActiveDeity == DeityType.None)
        {
            attacker.SendMessage(
                GlobalConstants.GeneralChatGroup,
                "[DivineAscension] Join a religion to earn favor and prestige from PvP!",
                EnumChatType.Notification
            );
            return;
        }

        var attackerReligion = _religionManager.GetReligion(attackerData.ReligionUID);
        if (attackerReligion == null)
        {
            _sapi.Logger.Warning(
                $"[DivineAscension] Attacker {attacker.PlayerName} has invalid religion UID: {attackerData.ReligionUID}");
            return;
        }

        // Calculate rewards
        var favorReward = CalculateFavorReward(attackerData.ActiveDeity, victimData.ActiveDeity);
        var prestigeReward = CalculatePrestigeReward(attackerData.ActiveDeity, victimData.ActiveDeity);

        // Award favor to player
        _playerReligionDataManager.AddFavor(attacker.PlayerUID, favorReward, $"PvP kill against {victim.PlayerName}");

        // Award prestige to religion
        _prestigeManager.AddPrestige(attackerReligion.ReligionUID, prestigeReward,
            $"PvP kill by {attacker.PlayerName} against {victim.PlayerName}");

        // Get deity for display
        var deity = _deityRegistry.GetDeity(attackerData.ActiveDeity);
        var deityName = deity?.Name ?? attackerData.ActiveDeity.ToString();

        // Notify attacker with combined rewards
        attacker.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Victory] {deityName} rewards you with {favorReward} favor! Your religion gains {prestigeReward} prestige!",
            EnumChatType.Notification
        );

        // Notify victim
        if (victimData.ActiveDeity != DeityType.None)
        {
            var victimDeity = _deityRegistry.GetDeity(victimData.ActiveDeity);
            var victimDeityName = victimDeity?.Name ?? victimData.ActiveDeity.ToString();
            victim.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Defeat] {victimDeityName} is displeased by your defeat.",
                EnumChatType.Notification
            );
        }

        _sapi.Logger.Debug(
            $"[DivineAscension] {attacker.PlayerName} earned {favorReward} favor and their religion earned {prestigeReward} prestige for killing {victim.PlayerName}");
    }

    /// <summary>
    ///     Applies death penalty to the player
    /// </summary>
    private void ProcessDeathPenalty(IServerPlayer player)
    {
        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (playerData.ActiveDeity == DeityType.None || playerData.ReligionUID == null) return;

        // Remove favor as penalty (minimum 0)
        var penalty = Math.Min(DEATH_PENALTY_FAVOR, playerData.Favor);
        if (penalty > 0)
        {
            playerData.RemoveFavor(penalty);

            player.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Disfavor] You lost {penalty} favor upon death.",
                EnumChatType.Notification
            );
        }
    }

    /// <summary>
    ///     Calculates favor reward
    /// </summary>
    private int CalculateFavorReward(DeityType attackerDeity, DeityType victimDeity)
    {
        return BASE_FAVOR_REWARD;
    }

    /// <summary>
    ///     Calculates prestige reward for religion
    /// </summary>
    private int CalculatePrestigeReward(DeityType attackerDeity, DeityType victimDeity)
    {
        return BASE_PRESTIGE_REWARD;
    }
}