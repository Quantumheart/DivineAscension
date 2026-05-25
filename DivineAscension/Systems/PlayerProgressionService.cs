using System;
using System.Collections.Concurrent;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
/// Facade service that coordinates player and religion progression rewards, plus the
/// blessing unlearn (respec) operation. Simplifies the common patterns of awarding favor,
/// prestige, logging activity, and refunding a blessing's cost.
/// </summary>
internal class PlayerProgressionService : IPlayerProgressionService
{
    private readonly IActivityLogManager _activityLogManager;
    private readonly IFavorSystem _favorSystem;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IPlayerProgressionDataManager _progressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly ICooldownManager _cooldownManager;
    private readonly GameBalanceConfig _config;
    private readonly ITimeService _timeService;

    // One unlearn op at a time per player (anti double-submit).
    private readonly ConcurrentDictionary<string, byte> _unlearnInProgress = new();

    // Late-bound: the blessing systems are constructed after this service.
    private IBlessingRegistry? _blessingRegistry;
    private IBlessingEffectSystem? _blessingEffectSystem;

    public PlayerProgressionService(
        IFavorSystem favorSystem,
        IReligionPrestigeManager prestigeManager,
        IActivityLogManager activityLogManager,
        IPlayerProgressionDataManager progressionDataManager,
        IReligionManager religionManager,
        ICooldownManager cooldownManager,
        GameBalanceConfig config,
        ITimeService timeService)
    {
        _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
        _prestigeManager = prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));
        _activityLogManager = activityLogManager ?? throw new ArgumentNullException(nameof(activityLogManager));
        _progressionDataManager =
            progressionDataManager ?? throw new ArgumentNullException(nameof(progressionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _cooldownManager = cooldownManager ?? throw new ArgumentNullException(nameof(cooldownManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
    }

    public void SetBlessingSystems(IBlessingRegistry blessingRegistry, IBlessingEffectSystem blessingEffectSystem)
    {
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
    }

    public void AwardProgressionForPrayer(
        string playerUID,
        string religionUID,
        int favor,
        int prestige,
        DeityDomain domain,
        string activityMessage)
    {
        // Award favor to player (player progression)
        _favorSystem.AwardFavorForAction(playerUID, "prayer", favor, domain);

        // Award prestige to religion (religion progression)
        _prestigeManager.AddPrestige(religionUID, prestige, "prayer");

        // Log the activity in the religion's feed
        _activityLogManager.LogActivity(
            religionUID,
            playerUID,
            activityMessage,
            favor,
            prestige,
            domain);
    }

    public UnlearnResult UnlearnBlessing(string playerUID, string blessingId)
    {
        if (_blessingRegistry == null || _blessingEffectSystem == null)
            return UnlearnResult.Fail(UnlearnFailureReason.NotConfigured);

        // Enforce a single in-flight unlearn per player.
        if (!_unlearnInProgress.TryAdd(playerUID, 0))
            return UnlearnResult.Fail(UnlearnFailureReason.InProgress);

        try
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing == null)
                return UnlearnResult.Fail(UnlearnFailureReason.BlessingNotFound);

            // Slice 1 scope: only personal (player-kind) blessings. Communal vows come later.
            if (blessing.Kind != BlessingKind.Player)
                return UnlearnResult.Fail(UnlearnFailureReason.NotPlayerBlessing);

            var data = _progressionDataManager.GetOrCreatePlayerData(playerUID);
            if (!data.IsBlessingUnlocked(blessingId))
                return UnlearnResult.Fail(UnlearnFailureReason.NotOwned);

            var religion = _religionManager.GetPlayerReligion(playerUID);
            if (religion == null)
                return UnlearnResult.Fail(UnlearnFailureReason.NotInReligion);

            // Reject while on cooldown. The persisted stamp survives restarts; the in-memory
            // CooldownManager stamp adds admin-bypass / global-disable within a session.
            var remaining = GetUnlearnCooldownRemainingSeconds(playerUID);
            var liveAllowed = _cooldownManager.CanPerformOperation(playerUID, CooldownType.BlessingUnlearn, out _);
            if (remaining > 0 || !liveAllowed)
                return UnlearnResult.Fail(UnlearnFailureReason.OnCooldown, remaining);

            // Strip from the unlocked set — frees the unlock slot directly (no cascade in slice 1).
            if (!data.LockBlessing(blessingId))
                return UnlearnResult.Fail(UnlearnFailureReason.NotOwned);

            // Refund a fraction of what was actually paid (adjusted cost) to SPENDABLE favor only,
            // so lifetime favor — and therefore favor rank — is untouched.
            var paidCost = BlessingRegistry.AdjustedCost(blessing, religion);
            var refund = (int)(paidCost * _config.UnlearnRefundPercent);
            if (refund > 0)
                data.AddSpendableFavor(blessing.Domain, refund);

            // Stamp the cooldown (both in-memory and persistent).
            _cooldownManager.RecordOperation(playerUID, CooldownType.BlessingUnlearn);
            data.NextUnlearnAllowedTimeUtc = _timeService.UtcNow.AddHours(_config.UnlearnCooldownHours);

            // Recompute effects from the now-smaller unlocked set, then push the update to the client.
            _blessingEffectSystem.RefreshPlayerBlessings(playerUID);
            _progressionDataManager.NotifyPlayerDataChanged(playerUID);

            return UnlearnResult.Ok(refund);
        }
        finally
        {
            _unlearnInProgress.TryRemove(playerUID, out _);
        }
    }

    public double GetUnlearnCooldownRemainingSeconds(string playerUID)
    {
        var live = _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.BlessingUnlearn);

        double persisted = 0;
        if (_progressionDataManager.TryGetPlayerData(playerUID, out var data)
            && data?.NextUnlearnAllowedTimeUtc is { } next)
        {
            var diff = (next - _timeService.UtcNow).TotalSeconds;
            if (diff > 0) persisted = diff;
        }

        return Math.Max(live, persisted);
    }
}
