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
/// Tracks agricultural activities and awards favor to Aethra followers
/// Activities: crop harvesting, planting, and cooking meals
/// </summary>
public class AethraFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Aethra;

    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Cache of active Aethra followers for fast lookup
    private readonly HashSet<string> _aethraFollowers = new();

    // Favor values
    private const float FavorPerCropHarvest = 1f;
    private const float FavorPerPlanting = 0.5f;
    private const int FavorSimpleMeal = 3;
    private const int FavorComplexMeal = 5;
    private const int FavorGourmetMeal = 8;

    // Cooking tracking
    private readonly Dictionary<BlockPos, CookingTrackingState> _trackedCookingContainers = new();
    private const int TickIntervalMs = 1000;
    private long _tickCounter;
    private const int DiscoveryInterval = 30; // Check for cooking containers every 30 seconds
    private int _lastPlayerScanIndex;
    private const int MaxPlayersPerScan = 2;
    private const int ScanRadiusXz = 8;
    private const int ScanRadiusY = 4;

    public void Initialize()
    {
        // Hook into block break event for crop harvesting
        _sapi.Event.BreakBlock += OnBlockBroken;

        // Hook into block placement for planting detection
        _sapi.Event.DidPlaceBlock += OnBlockPlaced;

        // Register game tick for cooking detection
        _sapi.Event.RegisterGameTickListener(OnGameTick, TickIntervalMs);

        // Build initial cache of Aethra followers
        RefreshFollowerCache();

        // Listen for religion changes to update cache
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;

        _sapi.Logger.Notification("[PantheonWars] AethraFavorTracker initialized");
    }

    #region Follower Cache Management

    private void RefreshFollowerCache()
    {
        _aethraFollowers.Clear();

        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData?.ActiveDeity == DeityType)
            {
                _aethraFollowers.Add(player.PlayerUID);
            }
        }
    }

    private void OnPlayerDataChanged(string playerUID)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (religionData?.ActiveDeity == DeityType)
        {
            _aethraFollowers.Add(playerUID);
        }
        else
        {
            _aethraFollowers.Remove(playerUID);
        }
    }

    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        _aethraFollowers.Remove(player.PlayerUID);
    }

    #endregion

    #region Crop Harvesting Detection

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        if (!_aethraFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsCropBlock(block))
        {
            // Award favor for harvesting crops
            _favorSystem.AwardFavorForAction(player, "harvesting " + GetCropName(block), FavorPerCropHarvest);
        }
    }

    /// <summary>
    /// Checks if a block is a harvestable crop
    /// </summary>
    private bool IsCropBlock(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Vintage Story crops are typically in "ripe" state when harvestable
        // Common crops: wheat, flax, onion, carrot, parsnip, turnip, cabbage, pumpkin, etc.
        return (path.Contains("crop") || path.Contains("vegetable")) &&
               (path.Contains("ripe") || path.Contains("harvested"));
    }

    private string GetCropName(Block block)
    {
        if (block?.Code == null) return "crops";

        string path = block.Code.Path;

        // Extract crop type from path
        if (path.Contains("wheat")) return "wheat";
        if (path.Contains("flax")) return "flax";
        if (path.Contains("onion")) return "onions";
        if (path.Contains("carrot")) return "carrots";
        if (path.Contains("parsnip")) return "parsnips";
        if (path.Contains("turnip")) return "turnips";
        if (path.Contains("cabbage")) return "cabbage";
        if (path.Contains("pumpkin")) return "pumpkins";
        if (path.Contains("rice")) return "rice";
        if (path.Contains("rye")) return "rye";
        if (path.Contains("spelt")) return "spelt";

        return "crops";
    }

    #endregion

    #region Planting Detection

    private void OnBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        if (!_aethraFollowers.Contains(byPlayer.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsPlantedCrop(block))
        {
            // Award favor for planting crops
            _favorSystem.AwardFavorForAction(byPlayer, "planting " + GetCropName(block), FavorPerPlanting);
        }
    }

    /// <summary>
    /// Checks if a block is a newly planted crop
    /// </summary>
    private bool IsPlantedCrop(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Newly planted crops are typically in early growth stages
        return (path.Contains("crop") || path.Contains("vegetable")) &&
               (path.Contains("1") || path.Contains("seed") || path.StartsWith("crop-"));
    }

    #endregion

    #region Cooking Detection

    private void OnGameTick(float dt)
    {
        _tickCounter++;

        // Periodically scan for active cooking containers
        if (_tickCounter % DiscoveryInterval == 0)
        {
            DiscoverActiveCookingContainers();
        }

        // Process all tracked cooking containers
        var positionsToCheck = new List<BlockPos>(_trackedCookingContainers.Keys);

        foreach (var pos in positionsToCheck)
        {
            var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(pos);

            // Handle different cooking container types
            if (blockEntity is BlockEntityFirepit firepit)
            {
                ProcessFirepit(pos, firepit);
            }
            else if (blockEntity is BlockEntityCrock crock)
            {
                ProcessCrock(pos, crock);
            }
        }

        // Clean up unloaded containers
        CleanupUnloadedContainers();
    }

    private void DiscoverActiveCookingContainers()
    {
        // Get all Aethra followers
        var aethraFollowers = new List<IServerPlayer>();
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            if (player is not IServerPlayer serverPlayer) continue;

            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(serverPlayer.PlayerUID);
            if (religionData.ActiveDeity == DeityType.Aethra)
            {
                aethraFollowers.Add(serverPlayer);
            }
        }

        if (aethraFollowers.Count == 0) return;

        // Only scan a subset of players per tick (round-robin)
        int playersToScan = Math.Min(MaxPlayersPerScan, aethraFollowers.Count);

        for (int i = 0; i < playersToScan; i++)
        {
            int playerIndex = (_lastPlayerScanIndex + i) % aethraFollowers.Count;
            var serverPlayer = aethraFollowers[playerIndex];

            ScanNearbyCookingContainers(serverPlayer);
        }

        // Update index for next scan
        _lastPlayerScanIndex = (_lastPlayerScanIndex + playersToScan) % Math.Max(1, aethraFollowers.Count);
    }

    private void ScanNearbyCookingContainers(IServerPlayer serverPlayer)
    {
        var playerPos = serverPlayer.Entity?.Pos?.AsBlockPos;
        if (playerPos == null) return;

        int containersFound = 0;
        const int maxContainersPerScan = 5;

        for (int x = -ScanRadiusXz; x <= ScanRadiusXz && containersFound < maxContainersPerScan; x++)
        {
            for (int y = -ScanRadiusY; y <= ScanRadiusY && containersFound < maxContainersPerScan; y++)
            {
                for (int z = -ScanRadiusXz; z <= ScanRadiusXz && containersFound < maxContainersPerScan; z++)
                {
                    var checkPos = playerPos.AddCopy(x, y, z);

                    // Skip if already tracking this position
                    if (_trackedCookingContainers.ContainsKey(checkPos))
                        continue;

                    var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(checkPos);

                    // Check for cooking containers with active contents
                    if ((blockEntity is BlockEntityFirepit firepit && HasCookingContent(firepit)) ||
                        (blockEntity is BlockEntityCrock crock && HasCookingContent(crock)))
                    {
                        // Start tracking this container
                        var state = new CookingTrackingState
                        {
                            CraftingPlayer = serverPlayer.PlayerUID
                        };
                        _trackedCookingContainers[checkPos] = state;

                        _sapi.Logger.Debug($"[AethraFavorTracker] Discovered cooking container at {checkPos} for player {serverPlayer.PlayerName}");
                        containersFound++;
                    }
                }
            }
        }
    }

    private bool HasCookingContent(BlockEntityFirepit firepit)
    {
        // Check if firepit has items cooking (use inventory slots)
        // Firepit has multiple slots for cooking items
        return firepit.Inventory?.Count > 0;
    }

    private bool HasCookingContent(BlockEntityCrock crock)
    {
        // Check if crock has contents
        return crock.Inventory?.Count > 0;
    }

    private void ProcessFirepit(BlockPos pos, BlockEntityFirepit firepit)
    {
        if (!_trackedCookingContainers.TryGetValue(pos, out var state))
        {
            state = new CookingTrackingState();
            _trackedCookingContainers[pos] = state;
        }

        // Track inventory changes to detect when cooking completes
        // Firepit slots: typically slot 0-3 for cooking items
        int currentItemCount = 0;
        ItemStack? cookedItem = null;

        if (firepit.Inventory != null)
        {
            for (int i = 0; i < firepit.Inventory.Count; i++)
            {
                var slot = firepit.Inventory[i];
                if (slot?.Itemstack != null)
                {
                    currentItemCount++;
                    // Track the last non-empty slot as potential output
                    cookedItem = slot.Itemstack;
                }
            }
        }

        // Check if item count decreased (cooking completed and item was removed)
        // Or track when items transition from raw to cooked
        if (state.LastItemCount > 0 && currentItemCount < state.LastItemCount && cookedItem != null)
        {
            // Something was removed - likely a cooked item
            HandleCookingCompletion(state, cookedItem);
        }

        // Update state for next tick
        state.LastItemCount = currentItemCount;
    }

    private void ProcessCrock(BlockPos pos, BlockEntityCrock crock)
    {
        if (!_trackedCookingContainers.TryGetValue(pos, out var state))
        {
            state = new CookingTrackingState();
            _trackedCookingContainers[pos] = state;
        }

        // Crocks work differently - they create meals when sealed and time passes
        // For now, we'll track based on inventory changes
        int currentItemCount = crock.Inventory?.Count ?? 0;

        // If crock now has items and didn't before, someone is preparing a meal
        if (currentItemCount > 0 && state.LastItemCount == 0)
        {
            // Meal preparation started - store player context
            // (In a real implementation, we'd need to track who last interacted with the crock)
        }

        state.LastItemCount = currentItemCount;
    }

    private void HandleCookingCompletion(CookingTrackingState state, ItemStack cookedItem)
    {
        if (string.IsNullOrEmpty(state.CraftingPlayer))
            return;

        var player = _sapi.World.PlayerByUid(state.CraftingPlayer) as IServerPlayer;
        if (player == null)
            return;

        // Check if player follows Aethra
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Aethra)
            return;

        // Calculate favor based on meal complexity
        int favor = CalculateMealFavor(cookedItem);

        _favorSystem.AwardFavorForAction(player, "cooking " + GetMealName(cookedItem), favor);

        _sapi.Logger.Debug($"[AethraFavorTracker] Awarded {favor} favor to {player.PlayerName} for cooking");

        // Reset state after completion
        state.Reset();
    }

    private int CalculateMealFavor(ItemStack meal)
    {
        if (meal == null) return FavorSimpleMeal;

        string path = meal.Collectible?.Code?.Path ?? "";

        // Gourmet meals (complex multi-ingredient dishes)
        if (IsGourmetMeal(path)) return FavorGourmetMeal;

        // Complex meals (cooked dishes with multiple ingredients)
        if (IsComplexMeal(path)) return FavorComplexMeal;

        // Simple meals (basic cooked items)
        return FavorSimpleMeal;
    }

    private bool IsGourmetMeal(string path)
    {
        // High-tier meals: stews, pies, complex dishes
        return path.Contains("stew") ||
               path.Contains("pie") ||
               path.Contains("soup") ||
               path.Contains("porridge");
    }

    private bool IsComplexMeal(string path)
    {
        // Mid-tier meals: cooked meats, bread, simple prepared foods
        return path.Contains("cooked") ||
               path.Contains("bread") ||
               path.Contains("roast") ||
               path.Contains("baked");
    }

    private string GetMealName(ItemStack meal)
    {
        if (meal?.Collectible?.Code == null) return "a meal";

        string path = meal.Collectible.Code.Path;

        // Try to extract a friendly name
        if (path.Contains("bread")) return "bread";
        if (path.Contains("stew")) return "stew";
        if (path.Contains("soup")) return "soup";
        if (path.Contains("pie")) return "pie";
        if (path.Contains("porridge")) return "porridge";
        if (path.Contains("meat")) return "cooked meat";

        return "a meal";
    }

    private void CleanupUnloadedContainers()
    {
        var positionsToRemove = new List<BlockPos>();

        foreach (var (pos, state) in _trackedCookingContainers)
        {
            // Remove if chunk is unloaded
            var chunk = _sapi.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null)
            {
                positionsToRemove.Add(pos);
                continue;
            }

            // Remove if container no longer exists or has been inactive
            var blockEntity = _sapi.World.BlockAccessor.GetBlockEntity(pos);
            if (blockEntity == null ||
                (blockEntity is not BlockEntityFirepit && blockEntity is not BlockEntityCrock))
            {
                positionsToRemove.Add(pos);
            }
        }

        foreach (var pos in positionsToRemove)
        {
            _trackedCookingContainers.Remove(pos);
        }

        if (positionsToRemove.Count > 0)
        {
            _sapi.Logger.Debug($"[AethraFavorTracker] Cleaned up {positionsToRemove.Count} inactive cooking containers");
        }
    }

    #endregion

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _sapi.Event.DidPlaceBlock -= OnBlockPlaced;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _aethraFollowers.Clear();
        _trackedCookingContainers.Clear();
    }

    /// <summary>
    /// Tracks the state of a cooking container between game ticks
    /// </summary>
    private class CookingTrackingState
    {
        public string? CraftingPlayer { get; set; }
        public bool HadOutputLastTick { get; set; }
        public int LastItemCount { get; set; }

        public void Reset()
        {
            HadOutputLastTick = false;
            LastItemCount = 0;
        }
    }
}