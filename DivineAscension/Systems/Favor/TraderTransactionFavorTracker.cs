using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Caravan Trade favor source. Awards Caravan favor on completed NPC trader transactions.
///     Per-trade favor follows <c>base + floor(value / DivisorGears)</c>, capped at
///     <see cref="MaxFavorPerTrade"/> so a single whale trade can't ladder a player to Avatar.
/// </summary>
public class TraderTransactionFavorTracker(
    ILoggerWrapper logger,
    IWorldService worldService,
    IFavorSystem favorSystem,
    GameBalanceConfig config) : IFavorTracker, IDisposable
{
    internal const int BaseFavor = 2;
    internal const int DivisorGears = 10;
    internal const int MaxFavorPerTrade = 20;

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly GameBalanceConfig _config = config ?? throw new ArgumentNullException(nameof(config));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public void Dispose()
    {
        TraderPatches.OnTraderTransaction -= HandleTraderTransaction;
        _logger.Debug("[DivineAscension] TraderTransactionFavorTracker disposed");
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Caravan;

    public void Initialize()
    {
        TraderPatches.OnTraderTransaction += HandleTraderTransaction;
        _logger.Notification("[DivineAscension] TraderTransactionFavorTracker initialized");
    }

    /// <summary>
    ///     Awards Caravan favor for a completed NPC trade. Exposed for tests so we don't have
    ///     to drive a Harmony patch in CI.
    /// </summary>
    internal void HandleTraderTransaction(IPlayer player, int valueInGears)
    {
        if (player is not IServerPlayer serverPlayer) return;
        if (valueInGears <= 0) return;

        var favor = ComputeFavor(valueInGears, _config.CaravanPerTradeFavorCap) *
                    _config.CaravanTradeFavorMultiplier;
        _favorSystem.AwardFavorForAction(serverPlayer, "trade", favor, DeityDomain.Caravan);
        _logger.Debug(
            $"[TraderTransactionFavorTracker] Awarded {favor} favor to {serverPlayer.PlayerName} for NPC trade worth {valueInGears} gears");
    }

    /// <summary>
    ///     <c>BaseFavor + floor(value / DivisorGears)</c>, clamped to <paramref name="cap"/>
    ///     (the live config cap). The clamp is the anti-whale guard — without it, a single
    ///     1000-gear swap would dwarf the favor income of a normal trader run.
    /// </summary>
    internal static int ComputeFavor(int valueInGears, int cap)
    {
        if (valueInGears <= 0) return 0;
        var raw = BaseFavor + valueInGears / DivisorGears;
        return raw > cap ? cap : raw;
    }

    /// <summary>
    ///     Back-compat overload using the compiled <see cref="MaxFavorPerTrade"/> default.
    /// </summary>
    internal static int ComputeFavor(int valueInGears) => ComputeFavor(valueInGears, MaxFavorPerTrade);
}
