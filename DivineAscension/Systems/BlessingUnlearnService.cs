using System;
using System.Collections.Concurrent;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Core unlearn logic for a single owned personal blessing (epic #425, slice 1 — #459).
///     Kept separate from the network handler so the refund/strip behaviour is unit-testable
///     without a live server. No prerequisite cascade yet (that arrives in slice 2).
///     The only cost of unlearning is the unrefunded portion of the favor paid — no cooldown.
/// </summary>
public class BlessingUnlearnService : IBlessingUnlearnService
{
    private readonly BlessingRegistry _blessingRegistry;
    private readonly IBlessingEffectSystem _blessingEffectSystem;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly GameBalanceConfig _config;

    // Serializes unlearn ops per player so concurrent requests for the same player can't race on
    // the strip/refund of the same blessing.
    private readonly ConcurrentDictionary<string, object> _playerLocks = new();

    public BlessingUnlearnService(
        BlessingRegistry blessingRegistry,
        IBlessingEffectSystem blessingEffectSystem,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        GameBalanceConfig config)
    {
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public UnlearnResult UnlearnBlessing(string playerUID, string blessingId)
    {
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return new UnlearnResult(UnlearnOutcome.BlessingNotFound, 0);

        // Slice 1 covers personal blessings only; religion vows are not unlearnable here.
        if (blessing.Kind != BlessingKind.Player)
            return new UnlearnResult(UnlearnOutcome.NotPlayerBlessing, 0);

        var playerLock = _playerLocks.GetOrAdd(playerUID, _ => new object());
        lock (playerLock)
        {
            var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);

            if (!playerData.IsBlessingUnlocked(blessingId))
                return new UnlearnResult(UnlearnOutcome.NotOwned, 0);

            // Refund is based on the cost the player actually paid (patron multiplier applied
            // when their religion's patron domain differs from the blessing's domain).
            var religion = _religionManager.GetPlayerReligion(playerUID);
            var paidCost = religion != null
                ? BlessingRegistry.AdjustedCost(blessing, religion)
                : blessing.Cost;
            var refund = (int)(paidCost * _config.UnlearnRefundPercent);

            // Strip first so BlessingEffectSystem recomputes from the reduced unlocked set.
            playerData.LockBlessing(blessingId);

            // Refund to spendable favor only — lifetime is untouched, so rank cannot flicker.
            if (refund > 0)
                playerData.AddSpendableFavor(blessing.Domain, refund);

            _blessingEffectSystem.RefreshPlayerBlessings(playerUID);
            _playerProgressionDataManager.NotifyPlayerDataChanged(playerUID);

            return new UnlearnResult(UnlearnOutcome.Success, refund);
        }
    }
}
