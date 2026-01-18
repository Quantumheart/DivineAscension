using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Tracks ruin discovery and awards favor to Conquest domain followers.
///     Awards favor for discovering ancient ruins, temporal machinery, and other structures.
/// </summary>
public class RuinDiscoveryFavorTracker(
    ILogger logger,
    IEventService eventService,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private const int SCAN_INTERVAL_MS = 500; // Scan every 500ms
    private const int SCAN_RADIUS = 50; // 50 blocks
    private const int SCAN_STEP = 5; // Check every 5 blocks (sparse scanning)

    private readonly HashSet<string> _conquestFollowers = new();

    private readonly IEventService
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    private long _callbackId;

    public void Dispose()
    {
        _eventService.UnregisterCallback(_callbackId);
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _conquestFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Conquest;

    public void Initialize()
    {
        // Register periodic callback for ruin scanning
        _callbackId = _eventService.RegisterCallback(OnTick, SCAN_INTERVAL_MS);

        // Build initial cache of Conquest followers
        RefreshFollowerCache();

        // Listen for religion changes to update cache
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;

        _logger.Debug($"{SystemConstants.LogPrefix} Initialized RuinDiscoveryFavorTracker");
    }

    /// <summary>
    ///     Periodic callback to scan for ruins near Conquest followers
    /// </summary>
    private void OnTick(float deltaTime)
    {
        foreach (var playerUID in _conquestFollowers.ToList())
        {
            var player = _worldService.GetPlayerByUID(playerUID) as IServerPlayer;
            if (player?.Entity == null) continue;

            ScanForRuins(player);
        }
    }

    /// <summary>
    ///     Scan blocks in radius around player for ruin structures
    /// </summary>
    private void ScanForRuins(IServerPlayer player)
    {
        var playerPos = player.Entity.Pos.AsBlockPos;
        var blockAccessor = _worldService.GetBlockAccessor(false, false);

        // Sparse scan to reduce performance impact (check every 5 blocks)
        for (int dx = -SCAN_RADIUS; dx <= SCAN_RADIUS; dx += SCAN_STEP)
        {
            for (int dy = -SCAN_RADIUS; dy <= SCAN_RADIUS; dy += SCAN_STEP)
            {
                for (int dz = -SCAN_RADIUS; dz <= SCAN_RADIUS; dz += SCAN_STEP)
                {
                    var checkPos = playerPos.AddCopy(dx, dy, dz);
                    var block = blockAccessor.GetBlock(checkPos);

                    if (IsRuinBlock(block, out var type, out var favor))
                    {
                        // Check if already discovered
                        var posKey = $"{checkPos.X}_{checkPos.Y}_{checkPos.Z}";

                        // Use TryGetPlayerData - player should already have data if they're in follower cache
                        if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var playerData))
                        {
                            _logger.Warning(
                                $"{SystemConstants.LogPrefix} Player {player.PlayerName} in follower cache but has no data - skipping discovery");
                            continue;
                        }

                        if (playerData!.DiscoveredRuins.Contains(posKey))
                            continue; // Already discovered

                        // New discovery!
                        RecordDiscovery(player, checkPos, type, favor);
                        return; // Only award one discovery per tick (prevent spam)
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Check if block is a ruin structure and determine type/favor
    /// </summary>
    internal bool IsRuinBlock(Block block, out RuinType type, out int favor)
    {
        if (block?.Code?.Path == null)
        {
            type = default;
            favor = 0;
            return false;
        }

        var path = block.Code.Path;

        // Priority 1: Devastation structures (100 favor - rarest)
        if (path.StartsWith("devastation") ||
            path.StartsWith("drock") ||
            path.StartsWith("clutter-devastation") ||
            path.Contains("devgrowth") ||
            path.Contains("devplate"))
        {
            type = RuinType.Devastation;
            favor = 100;
            return true;
        }

        // Priority 2: Temporal machinery (75 favor - rare)
        if (path.Contains("statictranslocator") ||
            path.Contains("resonator") ||
            path.Contains("riftward"))
        {
            type = RuinType.Temporal;
            favor = 75;
            return true;
        }

        // Priority 3: Locust structures (25 favor - common)
        if (path.StartsWith("locustnest-"))
        {
            type = RuinType.Locust;
            favor = 25;
            return true;
        }

        // Priority 4: Brick ruins (20 favor - very common)
        if (path.StartsWith("brickruin-"))
        {
            type = RuinType.Brick;
            favor = 20;
            return true;
        }

        type = default;
        favor = 0;
        return false;
    }

    /// <summary>
    ///     Record discovery and award favor
    /// </summary>
    private void RecordDiscovery(IServerPlayer player, BlockPos pos, RuinType type, int favor)
    {
        var posKey = $"{pos.X}_{pos.Y}_{pos.Z}";

        // Use TryGetPlayerData for safety - should exist since we checked in ScanForRuins
        if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var playerData))
        {
            _logger.Error(
                $"{SystemConstants.LogPrefix} Failed to get player data for {player.PlayerName} during discovery recording");
            return;
        }

        // Add to discovered set (data is automatically marked dirty)
        playerData!.DiscoveredRuins.Add(posKey);

        // Award favor
        _favorSystem.AwardFavorForAction(player, $"discovered {type} ruin", favor);

        // Send notification
        player.SendMessage(GlobalConstants.GeneralChatGroup,
            $"Discovered {type} ruins! (+{favor} favor)",
            EnumChatType.Notification);

        _logger.Debug(
            $"{SystemConstants.LogPrefix} Player {player.PlayerName} discovered {type} ruin at {posKey} (+{favor} favor)");
    }

    /// <summary>
    ///     Rebuild cache of active Conquest followers
    /// </summary>
    private void RefreshFollowerCache()
    {
        _conquestFollowers.Clear();

        var onlinePlayers = _worldService.GetAllOnlinePlayers();
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            if (_playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID) == DeityDomain)
                _conquestFollowers.Add(player.PlayerUID);
        }
    }

    /// <summary>
    ///     Update cache when player data changes (e.g., joins a religion)
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        if (_playerProgressionDataManager.GetPlayerDeityType(playerUID) == DeityDomain)
            _conquestFollowers.Add(playerUID);
        else
            _conquestFollowers.Remove(playerUID);
    }

    /// <summary>
    ///     Update cache when a player leaves a religion
    /// </summary>
    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        _conquestFollowers.Remove(player.PlayerUID);
    }

    /// <summary>
    ///     Types of ruins that can be discovered
    /// </summary>
    internal enum RuinType
    {
        Devastation,
        Temporal,
        Locust,
        Brick
    }
}