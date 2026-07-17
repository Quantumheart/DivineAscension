using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Butchering;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Awards Wild-domain favor for Butchering mod workstation activities: skinning a
///     carcass on a skinning hook and butchering a carcass on a butcher table.
///     Subscribes to events raised by ButcheringPatches (via ButcheringEventEmitter).
///     Replaces the vanilla SkinPatches path for Butchering animals, which are picked up
///     rather than harvested and therefore never trigger EntityBehaviorHarvestable.SetHarvested.
/// </summary>
public class ButcheringFavorTracker(
    ILoggerWrapper logger,
    IEventService eventService,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem,
    ButcheringEventEmitter butcheringEventEmitter) : IFavorTracker, IDisposable
{
    private static readonly TimeSpan ButcheringAwardCooldown = TimeSpan.FromSeconds(5);

    private readonly IEventService
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ButcheringEventEmitter _emitter =
        butcheringEventEmitter ?? throw new ArgumentNullException(nameof(butcheringEventEmitter));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    // Per-player, per-workstation throttling to prevent duplicate awards from repeated
    // interactions. Key format: "playerUID:blockPos"
    private readonly Dictionary<string, DateTime> _lastButcheringAwardUtc = new();

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;

    public void Dispose()
    {
        _emitter.OnAnimalSkinned -= HandleSkinned;
        _emitter.OnAnimalButchered -= HandleButchered;
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
        _lastButcheringAwardUtc.Clear();
    }

    public void Initialize()
    {
        _emitter.OnAnimalSkinned += HandleSkinned;
        _emitter.OnAnimalButchered += HandleButchered;

        // Clean up throttle cache on player disconnect
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);

        _logger.Notification("[DivineAscension] ButcheringFavorTracker initialized");
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up throttle cache entries for disconnected player
        var keysToRemove = new List<string>();
        foreach (var key in _lastButcheringAwardUtc.Keys)
        {
            if (key.StartsWith(player.PlayerUID + ":"))
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
            _lastButcheringAwardUtc.Remove(key);
    }

    private void HandleSkinned(IServerPlayer? player, BlockPos? pos, ItemStack? stack, string workload)
    {
        AwardFavor(player, pos, stack, workload, isSkinning: true);
    }

    private void HandleButchered(IServerPlayer? player, BlockPos? pos, ItemStack? stack, string workload)
    {
        AwardFavor(player, pos, stack, workload, isSkinning: false);
    }

    private void AwardFavor(IServerPlayer? player, BlockPos? pos, ItemStack? stack, string workload,
        bool isSkinning)
    {
        if (player == null || pos == null || stack?.Collectible?.Code == null) return;

        // Rate limit per player per workstation to prevent duplicate awards
        var key = $"{player.PlayerUID}:{pos.X},{pos.Y},{pos.Z}";
        var now = DateTime.UtcNow;
        if (_lastButcheringAwardUtc.TryGetValue(key, out var last) &&
            now - last < ButcheringAwardCooldown)
            return;

        var favor = CalculateFavorByWorkload(workload);
        if (favor > 0)
        {
            var action = (isSkinning ? "skinning " : "butchering ") + stack.Collectible.Code.Path;
            _favorSystem.AwardFavorForAction(player, action, favor, DeityDomain.Wild);
            _lastButcheringAwardUtc[key] = now;
        }
    }

    /// <summary>
    ///     Calculates favor tier based on the ItemButcherable's butcheringWorkLoad attribute
    ///     ("small"/"medium"/"large"). Entity kg weight is unavailable at the workstation, so
    ///     the workload tier (present on every Butchering creature) is the cleanest signal.
    ///     Values track the existing skinning philosophy (~50% of hunting values).
    /// </summary>
    internal int CalculateFavorByWorkload(string workload)
    {
        return workload switch
        {
            "large" => 8, // Apex predators, massive herbivores (bears, elephants)
            "medium" => 5, // Medium prey (deer, sheep, boar)
            "small" => 3, // Small animals (chickens, rabbits) and babies
            _ => 2 // Unknown / fallback
        };
    }
}