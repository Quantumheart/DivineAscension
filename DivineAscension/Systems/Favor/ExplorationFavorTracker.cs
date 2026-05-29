using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Caravan Wayfaring favor source. Awards favor when a player transitions into a
///     previously-unvisited chunk, and a one-time bonus for the first encounter with each
///     trader entity. Standing still in a chunk grants nothing.
/// </summary>
public class ExplorationFavorTracker(
    ILoggerWrapper logger,
    IEventService eventService,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem,
    GameBalanceConfig config) : IFavorTracker, IDisposable
{
    private readonly GameBalanceConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    internal const int TICK_INTERVAL_MS = 2000;
    internal const float BASE_CHUNK_FAVOR = 1.0f;
    internal const float TRADER_BONUS_FAVOR = 10.0f;
    internal const float TRADER_SCAN_RADIUS = 8.0f;

    private readonly IEventService
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

    private readonly IFavorSystem
        _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly ILoggerWrapper
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    private readonly Dictionary<string, long> _lastChunkKeyByPlayer = new();

    private long _callbackId;

    public DeityDomain DeityDomain { get; } = DeityDomain.Caravan;

    public void Initialize()
    {
        _callbackId = _eventService.RegisterCallback(OnTick, TICK_INTERVAL_MS);
        _logger.Debug($"{SystemConstants.LogPrefix} Initialized ExplorationFavorTracker");
    }

    public void Dispose()
    {
        _eventService.UnregisterCallback(_callbackId);
        _lastChunkKeyByPlayer.Clear();
    }

    /// <summary>
    ///     Packs 2D chunk coordinates into a single long. Y is intentionally ignored so vertical
    ///     traversal doesn't multiply favor.
    /// </summary>
    internal static long PackChunkKey(int chunkX, int chunkZ) =>
        ((long)chunkX << 32) | (uint)chunkZ;

    private void OnTick(float deltaTime)
    {
        foreach (var p in _worldService.GetAllOnlinePlayers())
        {
            if (p is not IServerPlayer player || player.Entity == null) continue;
            ProcessPlayer(player);
        }
    }

    private void ProcessPlayer(IServerPlayer player)
    {
        var pos = player.Entity.Pos.AsBlockPos;
        var chunkSize = GlobalConstants.ChunkSize;
        var chunkX = pos.X / chunkSize;
        var chunkZ = pos.Z / chunkSize;
        var chunkKey = PackChunkKey(chunkX, chunkZ);

        // Only act on a transition; standing still grants nothing.
        if (_lastChunkKeyByPlayer.TryGetValue(player.PlayerUID, out var lastKey) && lastKey == chunkKey)
        {
            ScanForTrader(player);
            return;
        }

        _lastChunkKeyByPlayer[player.PlayerUID] = chunkKey;
        var multiplier = player.Entity?.Stats?.GetBlended(
            VintageStoryStats.ChunkDiscoveryFavorMultiplier) ?? 1.0f;
        TryAwardChunkFavor(player, chunkKey, multiplier);
        ScanForTrader(player);
    }

    /// <summary>
    ///     Credits chunk-discovery favor if this chunk is new to the player. Returns true when
    ///     favor was awarded. Multiplier is taken from the player's blended
    ///     <see cref="VintageStoryStats.ChunkDiscoveryFavorMultiplier"/> stat in the live path;
    ///     tests inject it directly.
    /// </summary>
    internal bool TryAwardChunkFavor(IServerPlayer player, long chunkKey, float multiplier)
    {
        if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var data) || data == null)
            return false;

        if (!data.TryAddDiscoveredChunk(chunkKey))
            return false;

        _favorSystem.AwardFavorForAction(player, "discovered chunk",
            BASE_CHUNK_FAVOR * multiplier * _config.CaravanExplorationFavorMultiplier,
            DeityDomain.Caravan);
        return true;
    }

    private void ScanForTrader(IServerPlayer player)
    {
        var pos = player.Entity.Pos.XYZ;
        Entity? nearest = _worldService.World?.GetNearestEntity(pos, TRADER_SCAN_RADIUS, TRADER_SCAN_RADIUS,
            e => e is EntityTrader);

        if (nearest is EntityTrader trader)
        {
            TryAwardTraderBonus(player, trader.EntityId);
        }
    }

    /// <summary>
    ///     Credits the trader first-encounter bonus if this entity ID hasn't been credited yet.
    ///     Returns true when favor was awarded.
    /// </summary>
    internal bool TryAwardTraderBonus(IServerPlayer player, long traderEntityId)
    {
        if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var data) || data == null)
            return false;

        if (!data.TryAddDiscoveredTrader(traderEntityId))
            return false;

        _favorSystem.AwardFavorForAction(player, "encountered trader",
            TRADER_BONUS_FAVOR * _config.CaravanExplorationFavorMultiplier,
            DeityDomain.Caravan);
        return true;
    }
}
