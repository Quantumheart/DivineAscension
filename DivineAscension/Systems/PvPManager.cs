using System;
using DivineAscension.Constants;
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
    private const int DEATH_PENALTY_FAVOR = 50;
    private readonly ICivilizationManager _civilizationManager;
    private readonly IDeityRegistry _deityRegistry;
    private readonly IDiplomacyManager _diplomacyManager;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;

    private readonly ICoreServerAPI _sapi;

    public PvPManager(
        ICoreServerAPI sapi,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        IReligionPrestigeManager prestigeManager,
        IDeityRegistry deityRegistry,
        ICivilizationManager civilizationManager,
        IDiplomacyManager diplomacyManager)
    {
        _sapi = sapi;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
        _deityRegistry = deityRegistry;
        _civilizationManager = civilizationManager;
        _diplomacyManager = diplomacyManager;
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
        var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (_playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID) == DeityType.None ||
            !_playerProgressionDataManager.HasReligion(player.PlayerUID)) return;

        // Award favor
        if (favorAmount > 0) _playerProgressionDataManager.AddFavor(player.PlayerUID, favorAmount, actionType);

        // Award prestige
        if (prestigeAmount > 0)
            _prestigeManager.AddPrestige(playerReligion!.ReligionUID, prestigeAmount,
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
    internal void ProcessPvPKill(IServerPlayer attacker, IServerPlayer victim)
    {
        var attackerActiveDeityType = _religionManager.GetPlayerActiveDeity(attacker.PlayerUID);
        var attackerReligion = _religionManager.GetPlayerReligion(attacker.PlayerUID);

        var victimActiveDeityType = _religionManager.GetPlayerActiveDeity(victim.PlayerUID);
        var victimReligion = _religionManager.GetPlayerReligion(victim.PlayerUID);

        // Check if attacker has a religion
        if (attackerReligion == null || attackerActiveDeityType == DeityType.None)
        {
            attacker.SendMessage(
                GlobalConstants.GeneralChatGroup,
                "[DivineAscension] Join a religion to earn favor and prestige from PvP!",
                EnumChatType.Notification
            );
            return;
        }

        // Prevention of "Friendly Fire" farming
        if (attackerReligion != null && victimReligion != null &&
            attackerReligion.ReligionUID == victimReligion.ReligionUID)
        {
            attacker.SendMessage(GlobalConstants.GeneralChatGroup,
                "[Divine Ascension] You gain no favor for shedding the blood of your own faith.",
                EnumChatType.Notification);
            return;
        }

        // Check diplomatic status between civilizations
        var attackerCiv = _civilizationManager.GetCivilizationByPlayer(attacker.PlayerUID);
        var victimCiv = _civilizationManager.GetCivilizationByPlayer(victim.PlayerUID);

        var diplomacyMultiplier = 1.0;

        if (attackerCiv != null && victimCiv != null && attackerCiv.CivId != victimCiv.CivId)
        {
            var diplomaticStatus = _diplomacyManager.GetDiplomaticStatus(attackerCiv.CivId, victimCiv.CivId);

            // Handle NAP or Alliance violations
            if (diplomaticStatus == DiplomaticStatus.Alliance || diplomaticStatus == DiplomaticStatus.NonAggressionPact)
            {
                var violationCount = _diplomacyManager.RecordPvPViolation(attackerCiv.CivId, victimCiv.CivId);

                attacker.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    $"[Diplomacy Warning] Attacking allied civilization! (Violation {violationCount}/3)",
                    EnumChatType.CommandError
                );

                _sapi.Logger.Warning(
                    $"[DivineAscension:Diplomacy] PvP violation: {attacker.PlayerName} ({attackerCiv.Name}) attacked {victim.PlayerName} ({victimCiv.Name}) - Status: {diplomaticStatus}, Violations: {violationCount}");

                // No rewards for attacking allies
                return;
            }

            // Apply War multiplier
            if (diplomaticStatus == DiplomaticStatus.War)
            {
                diplomacyMultiplier = DiplomacyConstants.WarFavorMultiplier;
            }
        }

        // Calculate rewards
        var baseFavorReward = CalculateFavorReward(attackerActiveDeityType, victimActiveDeityType);
        var basePrestigeReward = CalculatePrestigeReward(attackerActiveDeityType, victimActiveDeityType);

        // Apply diplomacy multiplier
        var favorReward = (int)(baseFavorReward * diplomacyMultiplier);
        var prestigeReward = (int)(basePrestigeReward * diplomacyMultiplier);

        // Award favor to player
        _playerProgressionDataManager.AddFavor(attacker.PlayerUID, favorReward,
            $"PvP kill against {victim.PlayerName}");

        // Award prestige to religion
        _prestigeManager.AddPrestige(attackerReligion.ReligionUID, prestigeReward,
            $"PvP kill by {attacker.PlayerName} against {victim.PlayerName}");

        // Get deity for display
        var deity = _deityRegistry.GetDeity(attackerActiveDeityType);
        var deityName = deity?.Name ?? nameof(attackerActiveDeityType);

        // Notify attacker with combined rewards
        var warBonus = diplomacyMultiplier > 1.0 ? " [WAR BONUS +50%]" : "";
        attacker.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Victory] {deityName} rewards you with {favorReward} favor! Your religion gains {prestigeReward} prestige!{warBonus}",
            EnumChatType.Notification
        );

        // Notify victim
        if (victimActiveDeityType != DeityType.None)
        {
            var victimDeity = _deityRegistry.GetDeity(victimActiveDeityType);
            var victimDeityName = victimDeity?.Name ?? nameof(victimActiveDeityType);
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
    internal void ProcessDeathPenalty(IServerPlayer player)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        var activeDeityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        var religionId = _religionManager.GetPlayerReligion(player.PlayerUID);

        if (activeDeityType == DeityType.None || religionId == null) return;

        // Remove favor as penalty (minimum 0)
        var penalty = Math.Min(DEATH_PENALTY_FAVOR, playerData.Favor);
        if (penalty > 0)
        {
            playerData.Favor -= penalty;

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