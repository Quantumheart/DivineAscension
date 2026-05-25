using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Core strike (unlearn) logic for an inscribed religion blessing and its prerequisite cascade
///     (epic #479, slice 5 — #484). Mirrors <see cref="BlessingUnlearnService" /> on the religion
///     side: striking a parent strips every dependent unlocked child (see
///     <see cref="BlessingCascadeResolver" />) and refunds a configured fraction of each blessing's
///     prestige cost to the religion's spendable prestige. Lifetime prestige is untouched, so the
///     prestige rank cannot flicker. Kept separate from the network handler so the
///     cascade/refund/strip behaviour is unit-testable without a live server. The founder-only check
///     lives in the handler.
/// </summary>
public class ReligionBlessingUnlearnService : IReligionBlessingUnlearnService
{
    private readonly BlessingRegistry _blessingRegistry;
    private readonly IBlessingEffectSystem _blessingEffectSystem;
    private readonly IReligionManager _religionManager;
    private readonly GameBalanceConfig _config;
    private readonly IFreeRespecWindow _freeRespecWindow;

    // Serializes strike ops per religion so concurrent founder requests can't race on the
    // strip/refund of the same blessing.
    private readonly ConcurrentDictionary<string, object> _religionLocks = new();

    public ReligionBlessingUnlearnService(
        BlessingRegistry blessingRegistry,
        IBlessingEffectSystem blessingEffectSystem,
        IReligionManager religionManager,
        GameBalanceConfig config,
        IFreeRespecWindow freeRespecWindow)
    {
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _freeRespecWindow = freeRespecWindow ?? throw new ArgumentNullException(nameof(freeRespecWindow));
    }

    public ReligionUnlearnResult UnlearnReligionBlessing(string religionUID, string blessingId)
    {
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return new ReligionUnlearnResult(ReligionUnlearnOutcome.BlessingNotFound, 0);

        // This service covers religion blessings only; personal blessings unlearn elsewhere.
        if (blessing.Kind != BlessingKind.Religion)
            return new ReligionUnlearnResult(ReligionUnlearnOutcome.NotReligionBlessing, 0);

        var religionLock = _religionLocks.GetOrAdd(religionUID, _ => new object());
        lock (religionLock)
        {
            var religion = _religionManager.GetReligion(religionUID);
            if (religion == null)
                return new ReligionUnlearnResult(ReligionUnlearnOutcome.ReligionNotFound, 0);

            if (!IsUnlocked(religion, blessingId))
                return new ReligionUnlearnResult(ReligionUnlearnOutcome.NotOwned, 0);

            // Resolve the full cascade (target + dependent unlocked children) and strip it
            // atomically. Refund the configured fraction of the prestige the religion actually paid
            // for each blessing — patron multiplier applied per blessing.
            var cascade = ResolveCascade(religion, blessingId);

            // During an admin-opened free-respec window, refund the full cost (100%); otherwise the
            // normal partial refund applies (mirrors the personal side, #462).
            var refundPercent = _freeRespecWindow.IsActive ? 1f : _config.UnlearnRefundPercent;

            var totalRefund = 0;
            foreach (var id in cascade)
            {
                var member = _blessingRegistry.GetBlessing(id);
                if (member == null)
                    continue;

                var paidCost = BlessingRegistry.AdjustedCost(member, religion);
                var refund = (int)(paidCost * refundPercent);

                // Strip first so BlessingEffectSystem recomputes from the reduced unlocked set.
                religion.LockBlessing(id);

                // Refund to spendable prestige only — lifetime is untouched, so rank cannot flicker.
                if (refund > 0)
                {
                    religion.RefundPrestige(refund);
                    totalRefund += refund;
                }
            }

            _blessingEffectSystem.RefreshReligionBlessings(religion.ReligionUID);

            return new ReligionUnlearnResult(ReligionUnlearnOutcome.Success, totalRefund, cascade);
        }
    }

    public IReadOnlyList<string> ResolveUnlearnCascade(string religionUID, string blessingId)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null || !IsUnlocked(religion, blessingId))
            return Array.Empty<string>();

        return ResolveCascade(religion, blessingId);
    }

    private static bool IsUnlocked(Data.ReligionData religion, string blessingId) =>
        religion.UnlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;

    private List<string> ResolveCascade(Data.ReligionData religion, string blessingId)
    {
        var unlocked = new HashSet<string>(
            religion.UnlockedBlessings.Where(kv => kv.Value).Select(kv => kv.Key));
        return BlessingCascadeResolver.Resolve(blessingId, unlocked, _blessingRegistry.GetBlessing);
    }
}
