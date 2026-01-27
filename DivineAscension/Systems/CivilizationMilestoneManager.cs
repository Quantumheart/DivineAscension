using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Manages civilization milestone progression and rewards
/// </summary>
public class CivilizationMilestoneManager : ICivilizationMilestoneManager
{
    private readonly ILoggerWrapper _logger;
    private readonly ICivilizationManager _civilizationManager;
    private readonly IReligionManager _religionManager;
    private readonly IHolySiteManager _holySiteManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IMilestoneDefinitionLoader _milestoneLoader;

    private IRitualProgressManager? _ritualProgressManager;
    private IPvPManager? _pvpManager;
    private readonly Dictionary<string, CivilizationBonuses> _bonusCache = new();
    private object? _lock;

    public CivilizationMilestoneManager(
        ILoggerWrapper logger,
        ICivilizationManager civilizationManager,
        IReligionManager religionManager,
        IHolySiteManager holySiteManager,
        IReligionPrestigeManager prestigeManager,
        IMilestoneDefinitionLoader milestoneLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _prestigeManager = prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));
        _milestoneLoader = milestoneLoader ?? throw new ArgumentNullException(nameof(milestoneLoader));
    }

    private object Lock
    {
        get
        {
            if (_lock == null)
                Interlocked.CompareExchange(ref _lock, new object(), null);
            return _lock;
        }
    }

    /// <inheritdoc />
    public event Action<string, string>? OnMilestoneUnlocked;

    /// <inheritdoc />
    public event Action<string, int>? OnRankIncreased;

    /// <inheritdoc />
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Civilization Milestone Manager...");

        // Subscribe to civilization events for milestone detection
        _civilizationManager.OnReligionAdded += HandleReligionAdded;
        _civilizationManager.OnReligionRemoved += HandleReligionRemoved;
        _civilizationManager.OnCivilizationDisbanded += HandleCivilizationDisbanded;

        // Subscribe to holy site events for milestone detection
        _holySiteManager.OnHolySiteCreated += HandleHolySiteCreated;

        // Subscribe to religion membership events for member count tracking
        _religionManager.OnMemberAdded += HandleMemberAdded;
        _religionManager.OnMemberRemoved += HandleMemberRemoved;

        _logger.Notification("[DivineAscension] Civilization Milestone Manager initialized");
    }

    /// <summary>
    /// Sets the ritual progress manager dependency.
    /// Must be called after RitualProgressManager is initialized.
    /// </summary>
    public void SetRitualProgressManager(IRitualProgressManager ritualProgressManager)
    {
        _ritualProgressManager = ritualProgressManager ?? throw new ArgumentNullException(nameof(ritualProgressManager));
        _ritualProgressManager.OnRitualCompleted += HandleRitualCompleted;
        _logger.Debug("[DivineAscension MilestoneManager] Subscribed to ritual completion events");
    }

    /// <summary>
    /// Sets the PvP manager dependency.
    /// Must be called after PvPManager is initialized.
    /// </summary>
    public void SetPvPManager(IPvPManager pvpManager)
    {
        _pvpManager = pvpManager ?? throw new ArgumentNullException(nameof(pvpManager));
        _pvpManager.OnWarKill += HandleWarKill;
        _logger.Debug("[DivineAscension MilestoneManager] Subscribed to war kill events");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _civilizationManager.OnReligionAdded -= HandleReligionAdded;
        _civilizationManager.OnReligionRemoved -= HandleReligionRemoved;
        _civilizationManager.OnCivilizationDisbanded -= HandleCivilizationDisbanded;
        _holySiteManager.OnHolySiteCreated -= HandleHolySiteCreated;
        _religionManager.OnMemberAdded -= HandleMemberAdded;
        _religionManager.OnMemberRemoved -= HandleMemberRemoved;

        if (_ritualProgressManager != null)
            _ritualProgressManager.OnRitualCompleted -= HandleRitualCompleted;

        if (_pvpManager != null)
            _pvpManager.OnWarKill -= HandleWarKill;

        OnMilestoneUnlocked = null;
        OnRankIncreased = null;

        lock (Lock)
        {
            _bonusCache.Clear();
        }
    }

    #region Public API

    /// <inheritdoc />
    public bool IsMilestoneCompleted(string civId, string milestoneId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        return civ?.CompletedMilestones.Contains(milestoneId) ?? false;
    }

    /// <inheritdoc />
    public int GetCivilizationRank(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        return civ?.Rank ?? 0;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetCompletedMilestones(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return new HashSet<string>();

        lock (Lock)
        {
            return new HashSet<string>(civ.CompletedMilestones);
        }
    }

    /// <inheritdoc />
    public CivilizationBonuses GetActiveBonuses(string civId)
    {
        lock (Lock)
        {
            if (_bonusCache.TryGetValue(civId, out var cached))
                return cached;

            var bonuses = ComputeBonuses(civId);
            _bonusCache[civId] = bonuses;
            return bonuses;
        }
    }

    /// <inheritdoc />
    public MilestoneProgress? GetMilestoneProgress(string civId, string milestoneId)
    {
        var milestone = _milestoneLoader.GetMilestone(milestoneId);
        if (milestone == null)
            return null;

        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return null;

        var isCompleted = civ.CompletedMilestones.Contains(milestoneId);
        var currentValue = GetCurrentValueForTrigger(civId, civ, milestone.Trigger);

        return new MilestoneProgress(
            milestoneId,
            milestone.Name,
            currentValue,
            milestone.Trigger.Threshold,
            isCompleted
        );
    }

    /// <inheritdoc />
    public Dictionary<string, MilestoneProgress> GetAllMilestoneProgress(string civId)
    {
        var result = new Dictionary<string, MilestoneProgress>();
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return result;

        foreach (var milestone in _milestoneLoader.GetAllMilestones())
        {
            var isCompleted = civ.CompletedMilestones.Contains(milestone.MilestoneId);
            var currentValue = GetCurrentValueForTrigger(civId, civ, milestone.Trigger);

            result[milestone.MilestoneId] = new MilestoneProgress(
                milestone.MilestoneId,
                milestone.Name,
                currentValue,
                milestone.Trigger.Threshold,
                isCompleted
            );
        }

        return result;
    }

    /// <inheritdoc />
    public void CheckMilestones(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
        {
            _logger.Debug($"[DivineAscension MilestoneManager] Civilization '{civId}' not found for milestone check");
            return;
        }

        var milestonesUnlocked = new List<MilestoneDefinition>();

        foreach (var milestone in _milestoneLoader.GetAllMilestones())
        {
            // Skip already completed milestones
            if (civ.CompletedMilestones.Contains(milestone.MilestoneId))
                continue;

            // Check if trigger condition is met
            if (IsTriggerMet(civId, civ, milestone.Trigger))
            {
                milestonesUnlocked.Add(milestone);
            }
        }

        // Process unlocked milestones
        foreach (var milestone in milestonesUnlocked)
        {
            UnlockMilestone(civ, milestone);
        }
    }

    /// <inheritdoc />
    public void RecordWarKill(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return;

        civ.WarKillCount++;
        _logger.Debug($"[DivineAscension MilestoneManager] War kill recorded for {civ.Name}: {civ.WarKillCount} total");

        // Check milestones after recording kill
        CheckMilestones(civId);
    }

    /// <inheritdoc />
    public int GetBonusHolySiteSlots(string civId)
    {
        return GetActiveBonuses(civId).BonusHolySiteSlots;
    }

    #endregion

    #region Event Handlers

    private void HandleReligionAdded(string civId, string religionId)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] Religion {religionId} added to civilization {civId}");
        InvalidateBonusCache(civId);
        RecalculateMemberCount(civId);
        CheckMilestones(civId);
    }

    private void HandleReligionRemoved(string civId, string religionId)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] Religion {religionId} removed from civilization {civId}");
        InvalidateBonusCache(civId);
        RecalculateMemberCount(civId);
        // Note: We don't revoke completed milestones when religions leave
    }

    private void HandleCivilizationDisbanded(string civId)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] Civilization {civId} disbanded, cleaning up");
        InvalidateBonusCache(civId);
    }

    private void HandleHolySiteCreated(string religionUID, string siteUID)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] Holy site {siteUID} created for religion {religionUID}");

        // Find the civilization for this religion
        var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
        if (civ != null)
        {
            CheckMilestones(civ.CivId);
        }
    }

    private void HandleRitualCompleted(string religionUID, string siteUID, int newTier)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] Ritual completed at site {siteUID}, new tier {newTier}");

        // Find the civilization for this religion
        var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
        if (civ != null)
        {
            CheckMilestones(civ.CivId);
        }
    }

    private void HandleWarKill(string civId)
    {
        _logger.Debug($"[DivineAscension MilestoneManager] War kill event received for civilization {civId}");
        RecordWarKill(civId);
    }

    private void HandleMemberAdded(string religionUID, string playerUID)
    {
        // Find the civilization for this religion
        var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
        if (civ != null)
        {
            civ.MemberCount++;
            _logger.Debug($"[DivineAscension MilestoneManager] Member added to {civ.Name}, new count: {civ.MemberCount}");
            CheckMilestones(civ.CivId);
        }
    }

    private void HandleMemberRemoved(string religionUID, string playerUID)
    {
        // Find the civilization for this religion
        var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
        if (civ != null)
        {
            civ.MemberCount = Math.Max(0, civ.MemberCount - 1);
            _logger.Debug($"[DivineAscension MilestoneManager] Member removed from {civ.Name}, new count: {civ.MemberCount}");
            // Note: We don't revoke milestones when members leave
        }
    }

    #endregion

    #region Private Methods

    private void UnlockMilestone(Civilization civ, MilestoneDefinition milestone)
    {
        _logger.Notification(
            $"[DivineAscension MilestoneManager] Civilization '{civ.Name}' unlocked milestone: {milestone.Name}");

        // Mark as completed
        civ.CompletedMilestones.Add(milestone.MilestoneId);

        // Apply rank reward
        if (milestone.Type == MilestoneType.Major && milestone.RankReward > 0)
        {
            var oldRank = civ.Rank;
            civ.Rank += milestone.RankReward;
            _logger.Notification(
                $"[DivineAscension MilestoneManager] Civilization '{civ.Name}' rank increased: {oldRank} -> {civ.Rank}");
            OnRankIncreased?.Invoke(civ.CivId, civ.Rank);
        }

        // Apply prestige payout to founding religion
        if (milestone.PrestigePayout > 0)
        {
            _prestigeManager.AddPrestige(civ.FounderReligionUID, milestone.PrestigePayout, $"Milestone: {milestone.Name}");
            _logger.Debug(
                $"[DivineAscension MilestoneManager] Awarded {milestone.PrestigePayout} prestige to founding religion");
        }

        // Apply permanent benefit
        if (milestone.PermanentBenefit != null)
        {
            ApplyPermanentBenefit(civ, milestone.PermanentBenefit);
        }

        // Invalidate bonus cache to recalculate
        InvalidateBonusCache(civ.CivId);

        // Fire event
        OnMilestoneUnlocked?.Invoke(civ.CivId, milestone.MilestoneId);
    }

    private void ApplyPermanentBenefit(Civilization civ, MilestoneBenefit benefit)
    {
        switch (benefit.Type)
        {
            case MilestoneBenefitType.UnlockBlessing:
                if (!string.IsNullOrEmpty(benefit.BlessingId))
                {
                    civ.UnlockedBlessings.Add(benefit.BlessingId);
                    _logger.Debug(
                        $"[DivineAscension MilestoneManager] Unlocked civilization blessing: {benefit.BlessingId}");
                }
                break;

            // Other benefit types are handled via GetActiveBonuses() computation
            case MilestoneBenefitType.PrestigeMultiplier:
            case MilestoneBenefitType.FavorMultiplier:
            case MilestoneBenefitType.ConquestMultiplier:
            case MilestoneBenefitType.HolySiteSlot:
            case MilestoneBenefitType.AllRewardsMultiplier:
                // These are computed dynamically from completed milestones
                break;
        }
    }

    private bool IsTriggerMet(string civId, Civilization civ, MilestoneTrigger trigger)
    {
        var currentValue = GetCurrentValueForTrigger(civId, civ, trigger);
        return currentValue >= trigger.Threshold;
    }

    private int GetCurrentValueForTrigger(string civId, Civilization civ, MilestoneTrigger trigger)
    {
        return trigger.Type switch
        {
            MilestoneTriggerType.ReligionCount => civ.MemberReligionIds.Count,
            MilestoneTriggerType.DomainCount => _civilizationManager.GetCivDeityTypes(civId).Count,
            MilestoneTriggerType.HolySiteCount => GetTotalHolySiteCount(civ),
            MilestoneTriggerType.RitualCount => GetCompletedRitualCount(civ),
            MilestoneTriggerType.MemberCount => civ.MemberCount,
            MilestoneTriggerType.WarKillCount => civ.WarKillCount,
            MilestoneTriggerType.HolySiteTier => GetHighestHolySiteTier(civ),
            MilestoneTriggerType.DiplomaticRelationship => GetDiplomaticRelationshipCount(civId),
            MilestoneTriggerType.AllMajorMilestones => GetCompletedMajorMilestoneCount(civ),
            _ => 0
        };
    }

    private int GetTotalHolySiteCount(Civilization civ)
    {
        var count = 0;
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            count += _holySiteManager.GetReligionHolySites(religionId).Count;
        }
        return count;
    }

    private int GetCompletedRitualCount(Civilization civ)
    {
        // Count tier upgrades as completed rituals (tier - 1 = completed rituals per site)
        var count = 0;
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var sites = _holySiteManager.GetReligionHolySites(religionId);
            foreach (var site in sites)
            {
                count += Math.Max(0, site.RitualTier - 1);
            }
        }
        return count;
    }

    private int GetHighestHolySiteTier(Civilization civ)
    {
        var maxTier = 0;
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var sites = _holySiteManager.GetReligionHolySites(religionId);
            foreach (var site in sites)
            {
                if (site.RitualTier > maxTier)
                    maxTier = site.RitualTier;
            }
        }
        return maxTier;
    }

    private int GetDiplomaticRelationshipCount(string civId)
    {
        // This would need DiplomacyManager integration
        // For now, return 0 - will be implemented in Phase 2
        return 0;
    }

    private int GetCompletedMajorMilestoneCount(Civilization civ)
    {
        var majorMilestones = _milestoneLoader.GetMajorMilestones();
        var completedCount = 0;

        foreach (var milestone in majorMilestones)
        {
            // Don't count the "all_major_milestones" milestone itself
            if (milestone.Trigger.Type == MilestoneTriggerType.AllMajorMilestones)
                continue;

            if (civ.CompletedMilestones.Contains(milestone.MilestoneId))
                completedCount++;
        }

        return completedCount;
    }

    private CivilizationBonuses ComputeBonuses(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return CivilizationBonuses.None;

        float prestigeMultiplier = 1.0f;
        float favorMultiplier = 1.0f;
        float conquestMultiplier = 1.0f;
        int holySiteSlots = 0;

        foreach (var milestoneId in civ.CompletedMilestones)
        {
            var milestone = _milestoneLoader.GetMilestone(milestoneId);
            if (milestone?.PermanentBenefit == null)
                continue;

            var benefit = milestone.PermanentBenefit;
            switch (benefit.Type)
            {
                case MilestoneBenefitType.PrestigeMultiplier:
                    prestigeMultiplier += benefit.Amount;
                    break;
                case MilestoneBenefitType.FavorMultiplier:
                    favorMultiplier += benefit.Amount;
                    break;
                case MilestoneBenefitType.ConquestMultiplier:
                    conquestMultiplier += benefit.Amount;
                    break;
                case MilestoneBenefitType.HolySiteSlot:
                    holySiteSlots += (int)benefit.Amount;
                    break;
                case MilestoneBenefitType.AllRewardsMultiplier:
                    prestigeMultiplier += benefit.Amount;
                    favorMultiplier += benefit.Amount;
                    break;
            }
        }

        return new CivilizationBonuses
        {
            PrestigeMultiplier = prestigeMultiplier,
            FavorMultiplier = favorMultiplier,
            ConquestMultiplier = conquestMultiplier,
            BonusHolySiteSlots = holySiteSlots
        };
    }

    private void InvalidateBonusCache(string civId)
    {
        lock (Lock)
        {
            _bonusCache.Remove(civId);
        }
    }

    private void RecalculateMemberCount(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return;

        var totalMembers = 0;
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null)
            {
                totalMembers += religion.GetMemberCount();
            }
        }

        civ.MemberCount = totalMembers;
        _logger.Debug($"[DivineAscension MilestoneManager] Recalculated member count for {civ.Name}: {totalMembers}");
    }

    #endregion
}
