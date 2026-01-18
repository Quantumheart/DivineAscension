using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages religion prestige progression and religion-wide blessings
/// </summary>
public class ReligionPrestigeManager : IReligionPrestigeManager
{
    private readonly GameBalanceConfig _config;
    private readonly ILogger _logger;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;
    private IBlessingEffectSystem? _blessingEffectSystem;
    private IBlessingRegistry? _blessingRegistry;
    private CivilizationManager? _civilizationManager;
    private IDiplomacyManager? _diplomacyManager;

    public ReligionPrestigeManager(ILogger logger, IWorldService worldService, IReligionManager religionManager,
        GameBalanceConfig config)
    {
        _logger = logger;
        _worldService = worldService;
        _religionManager = religionManager;
        _config = config;
    }

    /// <summary>
    ///     Sets the blessing registry and effect system (called after they're initialized)
    /// </summary>
    public void SetBlessingSystems(IBlessingRegistry blessingRegistry, IBlessingEffectSystem blessingEffectSystem)
    {
        _blessingRegistry = blessingRegistry;
        _blessingEffectSystem = blessingEffectSystem;
    }

    /// <summary>
    ///     Initializes the religion prestige manager
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Religion Prestige Manager...");
        _logger.Notification("[DivineAscension] Religion Prestige Manager initialized");
    }

    /// <summary>
    ///     Adds prestige to a religion and updates rank if needed
    /// </summary>
    public void AddPrestige(string religionUID, int amount, string reason = "")
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error($"[DivineAscension] Cannot add prestige to non-existent religion: {religionUID}");
            return;
        }

        var oldRank = religion.PrestigeRank;
        var oldPrestige = religion.Prestige;

        // Add prestige
        religion.Prestige += amount;
        religion.TotalPrestige += amount;

        if (!string.IsNullOrEmpty(reason))
            _logger.Debug(
                $"[DivineAscension] Religion {religion.ReligionName} gained {amount} prestige: {reason}");

        // Update rank
        UpdatePrestigeRank(religionUID);

        // Check if rank changed
        if (religion.PrestigeRank > oldRank) SendReligionRankUpNotification(religionUID, religion.PrestigeRank);

        // Save immediately to prevent data loss (only if prestige actually changed)
        if (religion.Prestige != oldPrestige)
        {
            _religionManager.TriggerSave();
        }
    }

    /// <summary>
    ///     Adds fractional prestige to a religion and updates rank if needed.
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    public void AddFractionalPrestige(string religionUID, float amount, string reason = "")
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error(
                $"[DivineAscension] Cannot add fractional prestige to non-existent religion: {religionUID}");
            return;
        }

        var oldRank = religion.PrestigeRank;
        var oldPrestige = religion.Prestige;

        // Add fractional prestige
        religion.AddFractionalPrestige(amount);

        // Only log when prestige is actually awarded (when accumulated >= 1)
        if (religion.Prestige != oldPrestige && !string.IsNullOrEmpty(reason))
        {
            var prestigeGained = religion.Prestige - oldPrestige;
            _logger.Debug(
                $"[DivineAscension] Religion {religion.ReligionName} gained {prestigeGained} prestige: {reason}");
        }

        // Update rank
        UpdatePrestigeRank(religionUID);

        // Check if rank changed
        if (religion.PrestigeRank > oldRank) SendReligionRankUpNotification(religionUID, religion.PrestigeRank);

        // Save immediately to prevent data loss (only if prestige actually changed)
        if (religion.Prestige != oldPrestige)
        {
            _religionManager.TriggerSave();
        }
    }

    /// <summary>
    ///     Updates prestige rank based on total prestige earned
    /// </summary>
    public void UpdatePrestigeRank(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error(
                $"[DivineAscension] Cannot update prestige rank for non-existent religion: {religionUID}");
            return;
        }

        var oldRank = religion.PrestigeRank;
        var newRank = CalculatePrestigeRank(religion.TotalPrestige);

        if (newRank != oldRank)
        {
            religion.PrestigeRank = newRank;
            _logger.Notification(
                $"[DivineAscension] Religion {religion.ReligionName} rank changed: {oldRank} -> {newRank}");

            // Check for new blessing unlocks
            CheckForNewBlessingUnlocks(religionUID, newRank);
        }
    }

    /// <summary>
    ///     Unlocks a religion blessing if requirements are met
    /// </summary>
    public bool UnlockReligionBlessing(string religionUID, string blessingId)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error($"[DivineAscension] Cannot unlock blessing for non-existent religion: {religionUID}");
            return false;
        }

        // Check if already unlocked
        if (religion.IsBlessingUnlocked(blessingId)) return false;

        // Unlock the blessing
        religion.UnlockBlessing(blessingId);
        _logger.Notification(
            $"[DivineAscension] Religion {religion.ReligionName} unlocked blessing: {blessingId}");

        // Trigger blessing effect refresh for all members
        TriggerBlessingEffectRefresh(religionUID);

        return true;
    }

    /// <summary>
    ///     Gets all active (unlocked) religion blessings
    /// </summary>
    public List<string> GetActiveReligionBlessings(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return new List<string>();

        var activeBlessings = new List<string>();
        foreach (var kvp in religion.UnlockedBlessings)
            if (kvp.Value) // If unlocked
                activeBlessings.Add(kvp.Key);

        return activeBlessings;
    }

    /// <summary>
    ///     Gets prestige progress information for display
    /// </summary>
    public (int current, int nextThreshold, PrestigeRank nextRank) GetPrestigeProgress(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return (0, 0, PrestigeRank.Fledgling);

        var nextThreshold = religion.PrestigeRank switch
        {
            PrestigeRank.Fledgling => _config.EstablishedThreshold,
            PrestigeRank.Established => _config.RenownedThreshold,
            PrestigeRank.Renowned => _config.LegendaryThreshold,
            PrestigeRank.Legendary => _config.MythicThreshold,
            PrestigeRank.Mythic => _config.MythicThreshold, // Max rank
            _ => _config.EstablishedThreshold
        };

        var nextRank = religion.PrestigeRank switch
        {
            PrestigeRank.Fledgling => PrestigeRank.Established,
            PrestigeRank.Established => PrestigeRank.Renowned,
            PrestigeRank.Renowned => PrestigeRank.Legendary,
            PrestigeRank.Legendary => PrestigeRank.Mythic,
            PrestigeRank.Mythic => PrestigeRank.Mythic, // Max rank
            _ => PrestigeRank.Established
        };

        return (religion.TotalPrestige, nextThreshold, nextRank);
    }

    /// <summary>
    ///     Sets the diplomacy manager and civilization manager (called after they're initialized)
    /// </summary>
    public void SetDiplomacyManager(IDiplomacyManager diplomacyManager, CivilizationManager civilizationManager)
    {
        _diplomacyManager = diplomacyManager;
        _civilizationManager = civilizationManager;

        // Subscribe to diplomacy events
        _diplomacyManager.OnRelationshipEstablished += HandleRelationshipEstablished;
        _diplomacyManager.OnWarDeclared += HandleWarDeclared;
    }

    /// <summary>
    ///     Calculates prestige rank based on total prestige
    /// </summary>
    private PrestigeRank CalculatePrestigeRank(int totalPrestige)
    {
        if (totalPrestige >= _config.MythicThreshold) return PrestigeRank.Mythic;
        if (totalPrestige >= _config.LegendaryThreshold) return PrestigeRank.Legendary;
        if (totalPrestige >= _config.RenownedThreshold) return PrestigeRank.Renowned;
        if (totalPrestige >= _config.EstablishedThreshold) return PrestigeRank.Established;
        return PrestigeRank.Fledgling;
    }

    /// <summary>
    ///     Checks for new blessing unlocks when religion ranks up
    /// </summary>
    private void CheckForNewBlessingUnlocks(string religionUID, PrestigeRank newRank)
    {
        if (_blessingRegistry == null)
        {
            _logger.Debug(
                "[DivineAscension] Blessing registry not yet initialized, skipping blessing unlock check");
            return;
        }

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        // Get all religion blessings for this deity
        var allReligionBlessings = _blessingRegistry.GetBlessingsForDeity(religion.Domain, BlessingKind.Religion);

        // Find blessings that are now unlockable at the new rank
        var newlyUnlockableBlessings = new List<Blessing>();

        foreach (var blessing in allReligionBlessings)
        {
            // Check if this blessing requires the new rank (or lower)
            if (blessing.RequiredPrestigeRank > (int)newRank) continue; // Not yet available

            // Check if already unlocked
            if (religion.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var unlocked) &&
                unlocked) continue; // Already unlocked

            // Check if all prerequisites are met
            var allPrereqsMet = true;
            if (blessing.PrerequisiteBlessings != null)
                foreach (var prereqId in blessing.PrerequisiteBlessings)
                    if (!religion.UnlockedBlessings.TryGetValue(prereqId, out var prereqUnlocked) || !prereqUnlocked)
                    {
                        allPrereqsMet = false;
                        break;
                    }

            if (allPrereqsMet) newlyUnlockableBlessings.Add(blessing);
        }

        // Notify religion members about newly unlockable blessings
        if (newlyUnlockableBlessings.Count > 0)
            NotifyNewBlessingsAvailable(religionUID, newlyUnlockableBlessings);
        else
            _logger.Debug(
                $"[DivineAscension] No new blessings available for religion {religion.ReligionName} at rank {newRank}");
    }

    /// <summary>
    ///     Notifies all religion members about newly available blessings
    /// </summary>
    private void NotifyNewBlessingsAvailable(string religionUID, List<Blessing> newBlessings)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        var blessingNames = string.Join(", ", newBlessings.Select(p => p.Name));
        var message =
            $"New blessings available for '{religion.ReligionName}': {blessingNames}. Use /blessings religion to view and /blessings unlock to unlock them.";

        // Notify all members
        foreach (var memberUID in religion.MemberUIDs)
        {
            var player = _worldService.GetPlayerByUID(memberUID) as IServerPlayer;
            if (player != null)
                player.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    message,
                    EnumChatType.Notification
                );
        }

        _logger.Notification(
            $"[DivineAscension] Religion {religion.ReligionName} has {newBlessings.Count} new blessings available: {blessingNames}");
    }

    /// <summary>
    ///     Sends rank-up notification to all religion members
    /// </summary>
    private void SendReligionRankUpNotification(string religionUID, PrestigeRank newRank)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        var message = $"Your religion '{religion.ReligionName}' has ascended to {newRank} rank!";

        // Notify all members
        foreach (var memberUID in religion.MemberUIDs)
        {
            var player = _worldService.GetPlayerByUID(memberUID) as IServerPlayer;
            if (player != null)
                player.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    message,
                    EnumChatType.Notification
                );
        }

        _logger.Notification($"[DivineAscension] Religion {religion.ReligionName} reached {newRank} rank!");
    }

    /// <summary>
    ///     Triggers blessing effect refresh for all members
    /// </summary>
    private void TriggerBlessingEffectRefresh(string religionUID)
    {
        if (_blessingEffectSystem == null)
        {
            _logger.Debug(
                "[DivineAscension] Blessing effect system not yet initialized, skipping blessing refresh");
            return;
        }

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        _logger.Debug(
            $"[DivineAscension] Triggering blessing effect refresh for religion {religion.ReligionName}");

        // Refresh blessing effects for all members
        _blessingEffectSystem.RefreshReligionBlessings(religionUID);
    }

    /// <summary>
    ///     Handles when a diplomatic relationship is established
    /// </summary>
    private void HandleRelationshipEstablished(string civId1, string civId2, DiplomaticStatus status)
    {
        // Award prestige bonus for Alliance formation
        if (status == DiplomaticStatus.Alliance)
        {
            if (_civilizationManager == null)
            {
                _logger.Warning(
                    "[DivineAscension:Diplomacy] Civilization manager not set, cannot award Alliance prestige");
                return;
            }

            var civ1 = _civilizationManager.GetCivilization(civId1);
            var civ2 = _civilizationManager.GetCivilization(civId2);

            if (civ1 == null || civ2 == null)
            {
                _logger.Warning(
                    $"[DivineAscension:Diplomacy] Cannot find civilizations for Alliance prestige: {civId1}, {civId2}");
                return;
            }

            // Award prestige to all religions in both civilizations
            var allReligionIds = civ1.MemberReligionIds.Concat(civ2.MemberReligionIds).Distinct();

            foreach (var religionId in allReligionIds)
            {
                AddPrestige(religionId, DiplomacyConstants.AlliancePrestigeBonus,
                    $"Alliance formed between {civ1.Name} and {civ2.Name}");
            }

            _logger.Notification(
                $"[DivineAscension:Diplomacy] Alliance formed: {civ1.Name} and {civ2.Name} - {allReligionIds.Count()} religions gained {DiplomacyConstants.AlliancePrestigeBonus} prestige");
        }
    }

    /// <summary>
    ///     Handles when war is declared between civilizations
    /// </summary>
    private void HandleWarDeclared(string declarerCivId, string targetCivId)
    {
        if (_civilizationManager == null)
        {
            _logger.Warning("[DivineAscension:Diplomacy] Civilization manager not set, cannot announce war");
            return;
        }

        var declarerCiv = _civilizationManager.GetCivilization(declarerCivId);
        var targetCiv = _civilizationManager.GetCivilization(targetCivId);

        if (declarerCiv == null || targetCiv == null)
        {
            _logger.Warning(
                $"[DivineAscension:Diplomacy] Cannot find civilizations for war announcement: {declarerCivId}, {targetCivId}");
            return;
        }

        // Broadcast war declaration to all online players
        var message = $"[Diplomacy] {declarerCiv.Name} has declared WAR on {targetCiv.Name}!";

        foreach (var player in _worldService.GetAllOnlinePlayers())
        {
            if (player is IServerPlayer serverPlayer)
            {
                serverPlayer.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    message,
                    EnumChatType.Notification
                );
            }
        }

        _logger.Notification($"[DivineAscension:Diplomacy] WAR declared: {declarerCiv.Name} vs {targetCiv.Name}");
    }
}