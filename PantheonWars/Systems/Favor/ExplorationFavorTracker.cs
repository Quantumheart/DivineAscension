using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

/// <summary>
///     Awards favor for exploring new chunks while following Lysa
///     Rules: 2 favor the first time a player enters a chunk during the current session
/// </summary>
public class ExplorationFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Lysa;

    private readonly IPlayerReligionDataManager _playerReligionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Track current followers for early exit
    private readonly HashSet<string> _lysaFollowers = new();

    // Track visited chunks per player (session-scoped). Key format: "cx,cz"
    private readonly Dictionary<string, HashSet<string>> _visitedChunks = new();

    // Cache last chunk per player to avoid redundant work between ticks
    private readonly Dictionary<string, (int cx, int cz)> _lastChunk = new();

    private long _tickListenerId = -1;

    public void Initialize()
    {
        // Build initial follower cache
        RefreshFollowerCache();

        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;

        // Poll player positions every 2 seconds (lightweight)
        _tickListenerId = _sapi.Event.RegisterGameTickListener(OnTick, 2000);
    }

    private void RefreshFollowerCache()
    {
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            UpdateFollower(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerUid) => UpdateFollower(playerUid);

    private void UpdateFollower(string playerUid)
    {
        var data = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);
        if (data?.ActiveDeity == DeityType)
        {
            _lysaFollowers.Add(playerUid);
        }
        else
        {
            _lysaFollowers.Remove(playerUid);
        }
    }

    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUid)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnTick(float dt)
    {
        if (_lysaFollowers.Count == 0) return;

        foreach (var online in _sapi.World.AllOnlinePlayers)
        {
            var player = online as IServerPlayer;
            if (player?.Entity == null) continue;
            if (!_lysaFollowers.Contains(player.PlayerUID)) continue;

            // Compute chunk coordinates (32x32 blocks per chunk)
            int x = (int)player.Entity.ServerPos.X;
            int z = (int)player.Entity.ServerPos.Z;
            int cx = x >> 5; // divide by 32
            int cz = z >> 5; // divide by 32

            // Skip if we haven't changed chunks
            if (_lastChunk.TryGetValue(player.PlayerUID, out var prev) && prev.cx == cx && prev.cz == cz)
                continue;

            _lastChunk[player.PlayerUID] = (cx, cz);

            // Check if this chunk was already visited this session
            if (!_visitedChunks.TryGetValue(player.PlayerUID, out var set))
            {
                set = new HashSet<string>();
                _visitedChunks[player.PlayerUID] = set;
            }

            string key = cx + "," + cz;
            if (set.Add(key))
            {
                // First time entering this chunk this session → award favor
                _favorSystem.AwardFavorForAction(player, $"exploring", 2f);
            }
        }
    }

    public void Dispose()
    {
        if (_tickListenerId != -1)
        {
            _sapi.Event.UnregisterGameTickListener(_tickListenerId);
            _tickListenerId = -1;
        }

        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;

        _lysaFollowers.Clear();
        _visitedChunks.Clear();
        _lastChunk.Clear();
    }
}
