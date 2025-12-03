using System.Collections.Generic;
using System.Linq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Systems;

/// <summary>
///     Manages religion prestige progression and religion-wide blessings
/// </summary>
public class ReligionPrestigeManager : IReligionPrestigeManager
{
    // Prestige rank thresholds
    private const int FLEDGLING_THRESHOLD = 0;
    private const int ESTABLISHED_THRESHOLD = 500;
    private const int RENOWNED_THRESHOLD = 2000;
    private const int LEGENDARY_THRESHOLD = 5000;
    private const int MYTHIC_THRESHOLD = 10000;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private IBlessingEffectSystem? _blessingEffectSystem;
    private IBlessingRegistry? _blessingRegistry;

    public ReligionPrestigeManager(ICoreServerAPI sapi, IReligionManager religionManager)
    {
        _sapi = sapi;
        _religionManager = religionManager;
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
        _sapi.Logger.Notification("[PantheonWars] Initializing Religion Prestige Manager...");
        _sapi.Logger.Notification("[PantheonWars] Religion Prestige Manager initialized");
    }

    /// <summary>
    ///     Adds prestige to a religion and updates rank if needed
    /// </summary>
    public void AddPrestige(string religionUID, int amount, string reason = "")
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot add prestige to non-existent religion: {religionUID}");
            return;
        }

        var oldRank = religion.PrestigeRank;

        // Add prestige
        religion.Prestige += amount;
        religion.TotalPrestige += amount;

        if (!string.IsNullOrEmpty(reason))
            _sapi.Logger.Debug($"[PantheonWars] Religion {religion.ReligionName} gained {amount} prestige: {reason}");

        // Update rank
        UpdatePrestigeRank(religionUID);

        // Check if rank changed
        if (religion.PrestigeRank > oldRank) SendReligionRankUpNotification(religionUID, religion.PrestigeRank);

        // Save immediately to prevent data loss
        _religionManager.TriggerSave();
    }

    /// <summary>
    ///     Updates prestige rank based on total prestige earned
    /// </summary>
    public void UpdatePrestigeRank(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot update prestige rank for non-existent religion: {religionUID}");
            return;
        }

        var oldRank = religion.PrestigeRank;
        var newRank = CalculatePrestigeRank(religion.TotalPrestige);

        if (newRank != oldRank)
        {
            religion.PrestigeRank = newRank;
            _sapi.Logger.Notification(
                $"[PantheonWars] Religion {religion.ReligionName} rank changed: {oldRank} -> {newRank}");

            // Check for new blessing unlocks
            CheckForNewBlessingUnlocks(religionUID, newRank);
        }
    }

    /// <summary>
    ///     Calculates prestige rank based on total prestige
    /// </summary>
    private PrestigeRank CalculatePrestigeRank(int totalPrestige)
    {
        if (totalPrestige >= MYTHIC_THRESHOLD) return PrestigeRank.Mythic;
        if (totalPrestige >= LEGENDARY_THRESHOLD) return PrestigeRank.Legendary;
        if (totalPrestige >= RENOWNED_THRESHOLD) return PrestigeRank.Renowned;
        if (totalPrestige >= ESTABLISHED_THRESHOLD) return PrestigeRank.Established;
        return PrestigeRank.Fledgling;
    }

    /// <summary>
    ///     Checks for new blessing unlocks when religion ranks up
    /// </summary>
    private void CheckForNewBlessingUnlocks(string religionUID, PrestigeRank newRank)
    {
        if (_blessingRegistry == null)
        {
            _sapi.Logger.Debug("[PantheonWars] Blessing registry not yet initialized, skipping blessing unlock check");
            return;
        }

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        // Get all religion blessings for this deity
        var allReligionBlessings = _blessingRegistry.GetBlessingsForDeity(religion.Deity, BlessingKind.Religion);

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
            _sapi.Logger.Debug(
                $"[PantheonWars] No new blessings available for religion {religion.ReligionName} at rank {newRank}");
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
            var player = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (player != null)
                player.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    message,
                    EnumChatType.Notification
                );
        }

        _sapi.Logger.Notification(
            $"[PantheonWars] Religion {religion.ReligionName} has {newBlessings.Count} new blessings available: {blessingNames}");
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
            var player = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (player != null)
                player.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    message,
                    EnumChatType.Notification
                );
        }

        _sapi.Logger.Notification($"[PantheonWars] Religion {religion.ReligionName} reached {newRank} rank!");
    }

    /// <summary>
    ///     Unlocks a religion blessing if requirements are met
    /// </summary>
    public bool UnlockReligionBlessing(string religionUID, string blessingId)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot unlock blessing for non-existent religion: {religionUID}");
            return false;
        }

        // Check if already unlocked
        if (religion.UnlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked) return false;

        // Unlock the blessing
        religion.UnlockedBlessings[blessingId] = true;
        _sapi.Logger.Notification($"[PantheonWars] Religion {religion.ReligionName} unlocked blessing: {blessingId}");

        // Trigger blessing effect refresh for all members
        TriggerBlessingEffectRefresh(religionUID);

        return true;
    }

    /// <summary>
    ///     Triggers blessing effect refresh for all members
    /// </summary>
    private void TriggerBlessingEffectRefresh(string religionUID)
    {
        if (_blessingEffectSystem == null)
        {
            _sapi.Logger.Debug("[PantheonWars] Blessing effect system not yet initialized, skipping blessing refresh");
            return;
        }

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        _sapi.Logger.Debug($"[PantheonWars] Triggering blessing effect refresh for religion {religion.ReligionName}");

        // Refresh blessing effects for all members
        _blessingEffectSystem.RefreshReligionBlessings(religionUID);
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
            PrestigeRank.Fledgling => ESTABLISHED_THRESHOLD,
            PrestigeRank.Established => RENOWNED_THRESHOLD,
            PrestigeRank.Renowned => LEGENDARY_THRESHOLD,
            PrestigeRank.Legendary => MYTHIC_THRESHOLD,
            PrestigeRank.Mythic => MYTHIC_THRESHOLD, // Max rank
            _ => ESTABLISHED_THRESHOLD
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
}