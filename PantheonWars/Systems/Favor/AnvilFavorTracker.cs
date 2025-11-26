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
/// Tracks anvil smithing activities and awards favor to Khoras followers on recipe completion
/// </summary>
public class AnvilFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;

    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Track anvil states between ticks
    private readonly Dictionary<BlockPos, AnvilTrackingState> _trackedAnvils = new();

    // NBT attribute keys for persistent tracking
    private const string NbtCraftingPlayer = "khorasCraftingPlayer";
    private const string NbtCraftingStartTime = "khorasCraftingStartTime";
    private const string NbtHelveHammered = "khorasHelveHammered";
    private const string NbtRecipeTracked = "khorasRecipeTracked";

    // Favor values per tier
    private const int FavorLowTier = 5;    // Copper
    private const int FavorMidTier = 10;   // Bronze, gold 
    private const int FavorHighTier = 15;  // Steel, iron
    private const int FavorEliteTier = 20; // Special alloys

    // Automation penalty
    private const float HelveHammerPenalty = 0.65f; // 35% reduction

    // Tick interval (1 second)
    private const int TickIntervalMs = 1000;

    private long _tickCounter;
    private const int DiscoveryInterval = 20; // Check for new anvils every 20 ticks (20 seconds)
    private int _lastPlayerScanIndex;
    private const int MaxPlayersPerScan = 2; // Only scan 2 players per discovery interval
    private const int ScanRadiusXz = 6; // Reduced from 10 to 6 blocks horizontally
    private const int ScanRadiusY = 3; // Reduced from 5 to 3 blocks vertically

    public void Initialize()
    {
        // Register game tick to monitor anvils
        _sapi.Event.RegisterGameTickListener(OnGameTick, TickIntervalMs);

        _sapi.Logger.Notification("[PantheonWars] AnvilFavorTracker initialized");
    }

    private void OnGameTick(float dt)
    {
        _tickCounter++;

        // Periodically scan for new anvils with work items
        if (_tickCounter % DiscoveryInterval == 0)
        {
            DiscoverActiveAnvils();
        }

        // Process all tracked anvils
        var positionsToCheck = new List<BlockPos>(_trackedAnvils.Keys);

        foreach (var pos in positionsToCheck)
        {
            if (_sapi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityAnvil anvil)
            {
                ProcessAnvil(pos, anvil);
            }
        }

        // Clean up unloaded chunks and inactive anvils
        CleanupUnloadedAnvils();
    }

    private void DiscoverActiveAnvils()
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

        if (khorasFollowers.Count == 0) return;

        // Only scan a subset of players per tick (round-robin)
        int playersToScan = Math.Min(MaxPlayersPerScan, khorasFollowers.Count);

        for (int i = 0; i < playersToScan; i++)
        {
            int playerIndex = (_lastPlayerScanIndex + i) % khorasFollowers.Count;
            var serverPlayer = khorasFollowers[playerIndex];

            ScanNearbyAnvils(serverPlayer);
        }

        // Update index for next scan
        _lastPlayerScanIndex = (_lastPlayerScanIndex + playersToScan) % Math.Max(1, khorasFollowers.Count);
    }

    private void ScanNearbyAnvils(IServerPlayer serverPlayer)
    {
        var playerPos = serverPlayer.Entity?.Pos?.AsBlockPos;
        if (playerPos == null) return;

        // Track anvils found this scan to avoid excessive searching
        int anvilsFound = 0;
        const int maxAnvilsPerScan = 5; // Don't scan forever if player is in anvil factory

        // Scan in smaller radius with early exits
        for (int x = -ScanRadiusXz; x <= ScanRadiusXz && anvilsFound < maxAnvilsPerScan; x++)
        {
            for (int y = -ScanRadiusY; y <= ScanRadiusY && anvilsFound < maxAnvilsPerScan; y++)
            {
                for (int z = -ScanRadiusXz; z <= ScanRadiusXz && anvilsFound < maxAnvilsPerScan; z++)
                {
                    var checkPos = playerPos.AddCopy(x, y, z);

                    // Skip if already tracking this position
                    if (_trackedAnvils.ContainsKey(checkPos))
                        continue;

                    var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(checkPos);

                    if (blockEntity is BlockEntityAnvil { WorkItemStack: not null } anvil)
                    {
                        // Start tracking this anvil
                        _trackedAnvils[checkPos] = new AnvilTrackingState();

                        // Set crafting player if not already set
                        if (string.IsNullOrEmpty(GetCraftingPlayer(anvil)))
                        {
                            SetCraftingPlayer(anvil, serverPlayer.PlayerUID);
                            _sapi.Logger.Debug($"[AnvilFavorTracker] Discovered anvil at {checkPos} for player {serverPlayer.PlayerName}");
                        }

                        anvilsFound++;
                    }
                }
            }
        }
    }

    private void ProcessAnvil(BlockPos pos, BlockEntityAnvil anvil)
    {
        // Get or create tracking state for this anvil
        if (!_trackedAnvils.TryGetValue(pos, out var state))
        {
            state = new AnvilTrackingState();
            _trackedAnvils[pos] = state;
        }

        bool currentlyHasWorkItem = anvil.WorkItemStack != null;

        // Check if recipe just completed
        if (!currentlyHasWorkItem && state.HadWorkItemLastTick)
        {
            // Recipe completed! Award favor
            HandleRecipeCompletion(state);

            // Reset state after completion
            state.Reset();
        }

        // Update state for next tick
        if (currentlyHasWorkItem)
        {
            state.HadWorkItemLastTick = true;
            state.LastPlayerId = GetCraftingPlayer(anvil);

            // Try to get the expected output (recipe output)
            if (anvil.SelectedRecipe != null && state.LastOutputPreview == null)
            {
                state.LastOutputPreview = anvil.SelectedRecipe.Output?.ResolvedItemstack;
            }

            // Check for helve hammer usage
            bool isHelveHammered = GetHelveHammered(anvil);
            if (!isHelveHammered)
            {
                // Check if helve hammer is adjacent
                isHelveHammered = CheckHelveHammerUsage(pos);
                if (isHelveHammered)
                {
                    SetHelveHammered(anvil, true);
                }
            }

            state.WasHelveHammered = isHelveHammered;
        }
        else
        {
            state.HadWorkItemLastTick = false;
        }
    }

    private void HandleRecipeCompletion(AnvilTrackingState state)
    {
        if (string.IsNullOrEmpty(state.LastPlayerId))
            return;

        var player = _sapi.World.PlayerByUid(state.LastPlayerId) as IServerPlayer;
        if (player == null)
            return;

        // Check if player follows Khoras
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Khoras)
            return;

        // Calculate favor based on output item (if we stored it)
        int baseFavor = FavorMidTier; // Default favor

        if (state.LastOutputPreview != null)
        {
            baseFavor = CalculateBaseFavor(state.LastOutputPreview);
        }

        // Apply automation penalty if helve hammered
        int finalFavor = ApplyAutomationPenalty(baseFavor, state.WasHelveHammered);

        _favorSystem.AwardFavorForAction(player, "smithing", finalFavor);

        _sapi.Logger.Debug($"[AnvilFavorTracker] Awarded {finalFavor} favor to {player.PlayerName} for smithing (base: {baseFavor}, automated: {state.WasHelveHammered})");
    }


    private void CleanupUnloadedAnvils()
    {
        var positionsToRemove = new List<BlockPos>();

        foreach (var (pos, state) in _trackedAnvils)
        {
            // Remove if chunk is unloaded or anvil has been inactive
            var chunk = _sapi.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null)
            {
                positionsToRemove.Add(pos);
                continue;
            }

            // Remove if anvil no longer exists or has been inactive for a while
            var anvil = _sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAnvil;
            if (anvil == null || (!state.HadWorkItemLastTick && anvil.WorkItemStack == null))
            {
                // Only remove if it's been inactive for multiple ticks
                if (!state.HadWorkItemLastTick)
                {
                    positionsToRemove.Add(pos);
                }
            }
        }

        foreach (var pos in positionsToRemove)
        {
            _trackedAnvils.Remove(pos);
        }

        if (positionsToRemove.Count > 0)
        {
            _sapi.Logger.Debug($"[AnvilFavorTracker] Cleaned up {positionsToRemove.Count} inactive anvils");
        }
    }

    #region NBT Attribute Helpers

    private string? GetCraftingPlayer(BlockEntityAnvil? anvil)
    {
        return anvil != null ? anvil.WorkItemStack?.Attributes?.GetString(NbtCraftingPlayer) : string.Empty;
    }

    private void SetCraftingPlayer(BlockEntityAnvil anvil, string playerId)
    {
        if (anvil.WorkItemStack?.Attributes != null)
        {
            anvil.WorkItemStack.Attributes.SetString(NbtCraftingPlayer, playerId);
        }
    }

    private bool GetHelveHammered(BlockEntityAnvil anvil)
    {
        return anvil.WorkItemStack?.Attributes?.GetBool(NbtHelveHammered) ?? false;
    }

    private void SetHelveHammered(BlockEntityAnvil anvil, bool value)
    {
        if (anvil.WorkItemStack?.Attributes != null)
        {
            anvil.WorkItemStack.Attributes.SetBool(NbtHelveHammered, value);
        }
    }

    private bool GetRecipeTracked(BlockEntityAnvil anvil)
    {
        return anvil.WorkItemStack?.Attributes?.GetBool(NbtRecipeTracked) ?? false;
    }

    private void SetRecipeTracked(BlockEntityAnvil anvil, bool value)
    {
        if (anvil.WorkItemStack?.Attributes != null)
        {
            anvil.WorkItemStack.Attributes.SetBool(NbtRecipeTracked, value);
        }
    }

    #endregion

    #region Favor Calculation

    private int CalculateBaseFavor(ItemStack? outputItem)
    {
        if (outputItem == null) return FavorLowTier;

        if (IsEliteTier(outputItem)) return FavorEliteTier;
        if (IsHighTier(outputItem)) return FavorHighTier;
        if (IsMidTier(outputItem)) return FavorMidTier;
        if (IsLowTier(outputItem)) return FavorLowTier;

        return FavorLowTier; // Default
    }

    private bool IsLowTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("copper");
    }

    private bool IsMidTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("bronze") ||  path.Contains("gold") || path.Contains("silver") || path.Contains("tinbronze") || path.Contains("black") && path.Contains("bronze");
    }

    private bool IsHighTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("meteoric") || path.Contains("iron");
    }

    private bool IsEliteTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("steel");
    }

    private int ApplyAutomationPenalty(int baseFavor, bool wasHelveHammered)
    {
        if (wasHelveHammered)
        {
            return (int)(baseFavor * HelveHammerPenalty);
        }
        return baseFavor;
    }

    #endregion

    #region Helve Hammer Detection

    private bool CheckHelveHammerUsage(BlockPos anvilPos)
    {
        // Check if helve hammer is adjacent to this anvil
        // Helve hammer block code: "helvehammer"
        foreach (var face in BlockFacing.ALLFACES)
        {
            var adjacentPos = anvilPos.AddCopy(face);
            var block = _sapi.World.BlockAccessor.GetBlock(adjacentPos);

            if (block?.Code?.Path.Contains("helvehammer") == true)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public void Dispose()
    {
        _trackedAnvils.Clear();
        _sapi.Logger.Notification("[PantheonWars] AnvilFavorTracker disposed");
    }

    /// <summary>
    /// Tracks the state of an anvil between game ticks
    /// </summary>
    private class AnvilTrackingState
    {
        public bool HadWorkItemLastTick { get; set; }
        public string? LastPlayerId { get; set; }
        public ItemStack? LastOutputPreview { get; set; }
        public bool WasHelveHammered { get; set; }

        public void Reset()
        {
            HadWorkItemLastTick = false;
            LastPlayerId = null;
            LastOutputPreview = null;
            WasHelveHammered = false;
        }
    }
}
