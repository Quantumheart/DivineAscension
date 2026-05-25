using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Core unlearn logic for an owned personal blessing and its prerequisite cascade
///     (epic #425, slices 1 &amp; 2 — #459, #460). Kept separate from the network handler so the
///     cascade/refund/strip behaviour is unit-testable without a live server. Unlearning a parent
///     strips every dependent unlocked child (see <see cref="BlessingCascadeResolver" />) and
///     refunds 50% of each blessing's cost. The unrefunded remainder is the only cost — no cooldown.
/// </summary>
public class BlessingUnlearnService : IBlessingUnlearnService
{
    private readonly BlessingRegistry _blessingRegistry;
    private readonly IBlessingEffectSystem _blessingEffectSystem;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly GameBalanceConfig _config;
    private readonly IFreeRespecWindow _freeRespecWindow;

    // Serializes unlearn ops per player so concurrent requests for the same player can't race on
    // the strip/refund of the same blessing.
    private readonly ConcurrentDictionary<string, object> _playerLocks = new();

    public BlessingUnlearnService(
        BlessingRegistry blessingRegistry,
        IBlessingEffectSystem blessingEffectSystem,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        GameBalanceConfig config,
        IFreeRespecWindow freeRespecWindow)
    {
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _freeRespecWindow = freeRespecWindow ?? throw new ArgumentNullException(nameof(freeRespecWindow));
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

            // Resolve the full cascade (target + dependent unlocked children) and strip it
            // atomically. Refund 50% of the cost the player actually paid for each blessing —
            // patron multiplier applied per blessing, refund credited to that blessing's domain.
            var cascade = ResolveCascade(playerData, blessingId);
            var religion = _religionManager.GetPlayerReligion(playerUID);

            // During an admin-opened free-respec window, refund the full cost (100%); otherwise the
            // normal partial refund applies (locked decision 7, #462).
            var refundPercent = _freeRespecWindow.IsActive ? 1f : _config.UnlearnRefundPercent;

            var totalRefund = 0;
            foreach (var id in cascade)
            {
                var member = _blessingRegistry.GetBlessing(id);
                if (member == null)
                    continue;

                var paidCost = religion != null
                    ? BlessingRegistry.AdjustedCost(member, religion)
                    : member.Cost;
                var refund = (int)(paidCost * refundPercent);

                // Strip first so BlessingEffectSystem recomputes from the reduced unlocked set.
                playerData.LockBlessing(id);

                // Refund to spendable favor only — lifetime is untouched, so rank cannot flicker.
                if (refund > 0)
                {
                    playerData.AddSpendableFavor(member.Domain, refund);
                    totalRefund += refund;
                }
            }

            _blessingEffectSystem.RefreshPlayerBlessings(playerUID);
            _playerProgressionDataManager.NotifyPlayerDataChanged(playerUID);

            return new UnlearnResult(UnlearnOutcome.Success, totalRefund, cascade);
        }
    }

    /// <summary>
    ///     Subscribes the apostasy penalty to the player-leaves-religion event. Wired once at
    ///     startup; the strip runs server-side whenever a player departs a religion.
    /// </summary>
    public void Initialize()
    {
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
    }

    private void OnPlayerLeavesReligion(Vintagestory.API.Server.IServerPlayer player, string religionUID)
    {
        if (player != null)
            StripDomainLockedForApostasy(player.PlayerUID);
    }

    public IReadOnlyList<string> StripDomainLockedForApostasy(string playerUID)
    {
        var playerLock = _playerLocks.GetOrAdd(playerUID, _ => new object());
        lock (playerLock)
        {
            var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
            var working = new HashSet<string>(playerData.UnlockedBlessings);

            // Domain-locked (RequiresPatron) blessings are forfeit on apostasy. Resolve each in
            // a deterministic order; an earlier cascade may already have taken a later one.
            var domainLocked = working
                .Where(id => _blessingRegistry.GetBlessing(id)?.RequiresPatron == true)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            var stripped = new List<string>();
            foreach (var id in domainLocked)
            {
                if (!working.Contains(id))
                    continue;

                var cascade = BlessingCascadeResolver.Resolve(id, working, _blessingRegistry.GetBlessing);
                foreach (var victim in cascade)
                    if (working.Remove(victim))
                    {
                        // Zero refund — the apostasy penalty is forfeiture, not a refunded unlearn.
                        playerData.LockBlessing(victim);
                        stripped.Add(victim);
                    }
            }

            if (stripped.Count > 0)
            {
                _blessingEffectSystem.RefreshPlayerBlessings(playerUID);
                _playerProgressionDataManager.NotifyPlayerDataChanged(playerUID);
            }

            return stripped;
        }
    }

    public IReadOnlyList<string> ResolveUnlearnCascade(string playerUID, string blessingId)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
        if (!playerData.IsBlessingUnlocked(blessingId))
            return Array.Empty<string>();

        return ResolveCascade(playerData, blessingId);
    }

    private List<string> ResolveCascade(PlayerProgressionData playerData, string blessingId)
    {
        var unlocked = new HashSet<string>(playerData.UnlockedBlessings);
        return BlessingCascadeResolver.Resolve(blessingId, unlocked, _blessingRegistry.GetBlessing);
    }
}
