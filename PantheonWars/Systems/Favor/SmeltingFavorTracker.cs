using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Favor;

/// <summary>
/// Tracks metal pouring into molds and awards favor to Khoras followers
/// </summary>
public class SmeltingFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;

    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Track mold states between ticks
    private readonly Dictionary<BlockPos, MoldTrackingState> _trackedMolds = new();

    // Favor values
    private const float BaseFavorPerUnit = 0.01f;
    private const float IngotMoldMultiplier = 0.4f; // 60% reduction for ingot molds

    // Tick interval (1 second)
    private const int TickIntervalMs = 1000;

    // Discovery settings (same pattern as anvil tracker)
    private long _tickCounter;
    private const int DiscoveryInterval = 20; // Check for new molds every 20 ticks (20 seconds)
    private int _lastPlayerScanIndex;
    private const int MaxPlayersPerScan = 2; // Only scan 2 players per discovery interval
    private const int ScanRadiusXz = 6; // Scan 6 blocks horizontally
    private const int ScanRadiusY = 3; // Scan 3 blocks vertically

    // Cleanup interval (every 5 minutes)
    private const int CleanupIntervalMinutes = 5;

    public void Initialize()
    {
        _sapi.Event.RegisterGameTickListener(OnGameTick, TickIntervalMs);
        _sapi.Logger.Notification("[PantheonWars] SmeltingFavorTracker initialized");
    }

    private void OnGameTick(float dt)
    {
        _tickCounter++;

        // Periodically scan for new molds with metal
        if (_tickCounter % DiscoveryInterval == 0)
        {
            DiscoverActiveMolds();
        }

        // Process all tracked molds
        var positionsToCheck = new List<BlockPos>(_trackedMolds.Keys);

        foreach (var pos in positionsToCheck)
        {
            var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(pos);

            if (blockEntity is BlockEntityToolMold toolMold)
            {
                ProcessMold(pos, toolMold, true);
            }
            else if (blockEntity is BlockEntityIngotMold ingotMold)
            {
                ProcessMold(pos, ingotMold, false);
            }
        }

        // Periodically clean up old states
        if (_tickCounter % (CleanupIntervalMinutes * 60) == 0)
        {
            CleanupOldMoldStates();
        }
    }

    private void DiscoverActiveMolds()
    {
        // Get all Khoras followers
        var khorasFollowers = new List<IServerPlayer>();
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            if (player is not IServerPlayer serverPlayer) continue;

            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(serverPlayer.PlayerUID);
            if (religionData.ActiveDeity == DeityType.Khoras)
            {
                khorasFollowers.Add(serverPlayer);
            }
        }

        _sapi.Logger.Debug($"[SmeltingFavorTracker] DiscoverActiveMolds: Found {khorasFollowers.Count} Khoras followers");

        if (khorasFollowers.Count == 0) return;

        // Only scan a subset of players per tick (round-robin)
        int playersToScan = Math.Min(MaxPlayersPerScan, khorasFollowers.Count);

        for (int i = 0; i < playersToScan; i++)
        {
            int playerIndex = (_lastPlayerScanIndex + i) % khorasFollowers.Count;
            var serverPlayer = khorasFollowers[playerIndex];

            ScanNearbyMolds(serverPlayer);
        }

        // Update index for next scan
        _lastPlayerScanIndex = (_lastPlayerScanIndex + playersToScan) % Math.Max(1, khorasFollowers.Count);
    }

    private void ScanNearbyMolds(IServerPlayer serverPlayer)
    {
        var playerPos = serverPlayer.Entity?.Pos?.AsBlockPos;
        if (playerPos == null) return;

        _sapi.Logger.Debug($"[SmeltingFavorTracker] Scanning for molds near {serverPlayer.PlayerName} at {playerPos}");

        // Track molds found this scan
        int moldsFound = 0;
        int blockEntitiesChecked = 0;
        const int maxMoldsPerScan = 10; // Molds are often grouped together

        // Scan in smaller radius with early exits
        for (int x = -ScanRadiusXz; x <= ScanRadiusXz && moldsFound < maxMoldsPerScan; x++)
        {
            for (int y = -ScanRadiusY; y <= ScanRadiusY && moldsFound < maxMoldsPerScan; y++)
            {
                for (int z = -ScanRadiusXz; z <= ScanRadiusXz && moldsFound < maxMoldsPerScan; z++)
                {
                    var checkPos = playerPos.AddCopy(x, y, z);

                    // Skip if already tracking this position
                    if (_trackedMolds.ContainsKey(checkPos))
                        continue;

                    var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(checkPos);

                    if (blockEntity != null)
                    {
                        blockEntitiesChecked++;
                    }

                    // Check for tool molds or ingot molds (track even if empty!)
                    bool isToolMold = false;
                    int fillLevel = 0;
                    bool isMold = false;

                    if (blockEntity is BlockEntityToolMold toolMold)
                    {
                        isToolMold = true;
                        fillLevel = toolMold.FillLevel;
                        isMold = true;
                    }
                    else if (blockEntity is BlockEntityIngotMold ingotMold)
                    {
                        fillLevel = ingotMold.FillLevelLeft + ingotMold.FillLevelRight;
                        isMold = true;
                    }

                    // Track molds even if empty, so we can detect when they get filled
                    if (isMold)
                    {
                        // If mold already has metal, award favor for it (assume it was just poured)
                        if (fillLevel > 0)
                        {
                            AwardFavorForPouring(serverPlayer.PlayerUID, fillLevel, isToolMold);
                            _sapi.Logger.Debug($"[SmeltingFavorTracker] Awarded retroactive favor for pre-existing metal in {(isToolMold ? "tool" : "ingot")} mold at {checkPos}");
                        }

                        // Start tracking this mold
                        _trackedMolds[checkPos] = new MoldTrackingState
                        {
                            PreviousFillLevel = fillLevel,
                            LastPlayerId = serverPlayer.PlayerUID,
                            LastUpdateTime = DateTime.UtcNow,
                            IsToolMold = isToolMold
                        };

                        moldsFound++;
                    }
                }
            }
        }
    }

    private void ProcessMold(BlockPos pos, BlockEntity moldEntity, bool isToolMold)
    {
        int currentFillLevel = GetFillLevel(moldEntity, isToolMold);

        // Get or create tracking state
        if (!_trackedMolds.TryGetValue(pos, out var state))
        {
            // This shouldn't happen normally, but create state if needed
            state = new MoldTrackingState
            {
                PreviousFillLevel = currentFillLevel,
                LastUpdateTime = DateTime.UtcNow,
                IsToolMold = isToolMold
            };
            _trackedMolds[pos] = state;
            return;
        }

        // Check if fill level increased (metal poured)
        if (currentFillLevel > state.PreviousFillLevel)
        {
            int amountPoured = currentFillLevel - state.PreviousFillLevel;
            AwardFavorForPouring(state.LastPlayerId, amountPoured, state.IsToolMold);

            _sapi.Logger.Debug($"[SmeltingFavorTracker] Player poured {amountPoured} units into {(state.IsToolMold ? "tool" : "ingot")} mold at {pos}");
        }
        // Check if mold was emptied (remove from tracking)
        else if (currentFillLevel == 0)
        {
            _trackedMolds.Remove(pos);
            _sapi.Logger.Debug($"[SmeltingFavorTracker] Removed empty mold at {pos} from tracking");
            return;
        }

        // Update tracking state
        state.PreviousFillLevel = currentFillLevel;
        state.LastUpdateTime = DateTime.UtcNow;
    }

    private void AwardFavorForPouring(string? playerId, int unitsPoured, bool isToolMold)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        var player = _sapi.World.PlayerByUid(playerId) as IServerPlayer;
        if (player == null)
            return;

        // Check if player follows Khoras
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Khoras)
            return;

        // Calculate favor
        float favor = CalculatePouringFavor(unitsPoured, isToolMold);

        if (favor >= 0.01f) // Only award if meaningful amount
        {
            // Use fractional favor accumulation
            _favorSystem.AwardFavorForAction(player, "smelting", favor);

            _sapi.Logger.Debug($"[SmeltingFavorTracker] Awarded {favor:F2} favor to {player.PlayerName} for pouring {unitsPoured} units into {(isToolMold ? "tool" : "ingot")} mold");
        }
    }

    private float CalculatePouringFavor(int unitsPoured, bool isToolMold)
    {
        if (isToolMold)
        {
            // Tool/weapon molds: Full favor
            return unitsPoured * BaseFavorPerUnit;
        }
        else
        {
            // Ingot molds: Reduced favor
            return unitsPoured * BaseFavorPerUnit * IngotMoldMultiplier;
        }
    }

    private int GetFillLevel(BlockEntity moldEntity, bool isToolMold)
    {
        if (isToolMold && moldEntity is BlockEntityToolMold toolMold)
        {
            return toolMold.FillLevel;
        }
        else if (!isToolMold && moldEntity is BlockEntityIngotMold ingotMold)
        {
            // Ingot molds have two fill levels (left and right)
            return ingotMold.FillLevelLeft + ingotMold.FillLevelRight;
        }

        return 0;
    }


    private void CleanupOldMoldStates()
    {
        var positionsToRemove = new List<BlockPos>();

        foreach (var (pos, state) in _trackedMolds)
        {
            // Remove if mold is old or chunk is unloaded
            var chunk = _sapi.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null)
            {
                positionsToRemove.Add(pos);
                continue;
            }

            // Remove if mold hasn't been updated in a while (likely emptied or removed)
            if ((DateTime.UtcNow - state.LastUpdateTime).TotalMinutes > CleanupIntervalMinutes)
            {
                positionsToRemove.Add(pos);
            }
        }

        foreach (var pos in positionsToRemove)
        {
            _trackedMolds.Remove(pos);
        }

        if (positionsToRemove.Count > 0)
        {
            _sapi.Logger.Debug($"[SmeltingFavorTracker] Cleaned up {positionsToRemove.Count} expired mold states");
        }
    }

    public void Dispose()
    {
        _trackedMolds.Clear();
        _sapi.Logger.Notification("[PantheonWars] SmeltingFavorTracker disposed");
    }

    /// <summary>
    /// Tracks the state of a mold between game ticks
    /// </summary>
    private class MoldTrackingState
    {
        public int PreviousFillLevel { get; set; }
        public string? LastPlayerId { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool IsToolMold { get; set; }
    }
}
