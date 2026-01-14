using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Tracks agricultural activities and awards favor to Aethra followers
///     Activities: crop harvesting, planting, and cooking meals
/// </summary>
public class HarvestFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    // Favor values
    private const float FavorPerCropHarvest = 1f;
    private const float FavorPerPlanting = 0.5f;
    private const float FavorSimpleMeal = 1f;
    private const float FavorComplexMeal = 5f;
    private const float FavorGourmetMeal = 10f;
    private static readonly TimeSpan CookingAwardCooldown = TimeSpan.FromSeconds(5);
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly HashSet<string> _harvestFollowers = new();

    // Event-based: no polling or container tracking needed

    // Simple per-player cooking award rate limit (to prevent rapid repeats)
    private readonly Dictionary<string, DateTime> _lastCookingAwardUtc = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        BlockCropPatches.OnCropHarvested -= OnCropHarvested;
        ScythePatches.OnScytheHarvest -= OnScytheHarvest;
        CropPlantingPatches.OnCropPlanted -= HandleCropPlanted;
        CookingPatches.OnMealCooked -= HandleMealCooked;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        _harvestFollowers.Clear();
        _lastCookingAwardUtc.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Harvest;

    public void Initialize()
    {
        // Subscribe to crop harvesting via typed BlockCrop patch
        BlockCropPatches.OnCropHarvested += OnCropHarvested;

        // Subscribe to scythe harvesting events for crops cut with scythe
        ScythePatches.OnScytheHarvest += OnScytheHarvest;

        // Subscribe to crop planting patch events (uses Harmony patch on BlockEntityFarmland.TryPlant)
        CropPlantingPatches.OnCropPlanted += HandleCropPlanted;

        // Subscribe to cooking completion events (firepit/crock). Attribution must
        // be the last interacting player; patches will ensure correct uid.
        CookingPatches.OnMealCooked += HandleMealCooked;

        // Build initial cache of Aethra followers
        RefreshFollowerCache();

        // Listen for religion changes to update cache
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;

        // Clean up cooking cooldown cache on player disconnect
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;

        _sapi.Logger.Notification("[DivineAscension] AethraFavorTracker initialized");
    }

    #region Planting Detection

    private void HandleCropPlanted(IServerPlayer player, Block cropBlock)
    {
        if (!_harvestFollowers.Contains(player.PlayerUID)) return;

        // Award favor for planting crops
        _favorSystem.AwardFavorForAction(player, "planting " + GetCropName(cropBlock), FavorPerPlanting);
    }

    #endregion

    #region Follower Cache Management

    private void RefreshFollowerCache()
    {
        _harvestFollowers.Clear();

        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            if (_playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID) == DeityDomain)
                _harvestFollowers.Add(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerUID)
    {
        if (_playerProgressionDataManager.GetPlayerDeityType(playerUID) == DeityDomain)
            _harvestFollowers.Add(playerUID);
        else
            _harvestFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        _harvestFollowers.Remove(player.PlayerUID);
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        _lastCookingAwardUtc.Remove(player.PlayerUID);
    }

    #endregion

    #region Crop Harvesting Detection

    /// <summary>
    ///     Handles crop harvesting via BlockCropPatches (typed BlockCrop).
    /// </summary>
    private void OnCropHarvested(IServerPlayer player, BlockCrop crop)
    {
        if (!_harvestFollowers.Contains(player.PlayerUID)) return;
        if (!IsMatureCrop(crop)) return;

        _favorSystem.AwardFavorForAction(player, "harvesting " + GetCropName(crop), FavorPerCropHarvest);
    }

    /// <summary>
    ///     Handles scythe/shears harvesting to detect crop cutting.
    ///     Awards favor for mature crops cut with scythe.
    /// </summary>
    private void OnScytheHarvest(IServerPlayer player, Block block)
    {
        if (!_harvestFollowers.Contains(player.PlayerUID)) return;
        if (block is not BlockCrop crop) return;
        if (!IsMatureCrop(crop)) return;

        _favorSystem.AwardFavorForAction(player, "harvesting " + GetCropName(crop), FavorPerCropHarvest);
    }

    /// <summary>
    ///     Checks if a crop is mature (fully grown).
    /// </summary>
    private bool IsMatureCrop(BlockCrop crop)
    {
        if (crop.Code == null) return false;

        // Check if it's already harvested
        if (crop.Variant.TryGetValue("state", out var state) && state == "harvested")
            return false;

        var currentStage = crop.CurrentCropStage;

        if (crop.CropProps != null)
        {
            var maxStages = crop.CropProps.GrowthStages;
            _sapi.Logger.Debug(
                $"[AethraFavorTracker] Crop: {crop.Code.Path}, Current: {currentStage}, Max: {maxStages}");
            return currentStage >= maxStages;
        }

        _sapi.Logger.Warning($"[AethraFavorTracker] Could not determine max stages for crop: {crop.Code.Path}");
        return false;
    }

    /// <summary>
    ///     Gets a display name for the crop from its variant type.
    /// </summary>
    private static string GetCropName(Block block)
    {
        if (block.Variant.TryGetValue("type", out var cropType))
            return cropType;

        return "crops";
    }

    #endregion

    #region Cooking (Event-Driven)

    private void HandleMealCooked(IServerPlayer player, ItemStack cookedStack, BlockPos pos)
    {
        if (!_harvestFollowers.Contains(player.PlayerUID)) return;

        // Rate limit per player
        var now = DateTime.UtcNow;
        if (_lastCookingAwardUtc.TryGetValue(player.PlayerUID, out var last) &&
            now - last < CookingAwardCooldown) return;

        var favor = CalculateMealFavor(cookedStack);
        if (favor <= 0) return;

        _favorSystem.AwardFavorForAction(player, "cooking " + GetMealName(cookedStack), favor);
        _lastCookingAwardUtc[player.PlayerUID] = now;

        _sapi.Logger.Debug(
            $"[AethraFavorTracker] Awarded {favor} favor to {player.PlayerName} for cooking {GetMealName(cookedStack)} at {pos}");
    }

    private float CalculateMealFavor(ItemStack meal)
    {
        if (meal == null) return 0;

        var path = meal.Collectible?.Code?.Path ?? string.Empty;

        if (IsGourmetMeal(path)) return FavorGourmetMeal;
        if (IsComplexMeal(path)) return FavorComplexMeal;
        return FavorSimpleMeal;
    }

    private bool IsGourmetMeal(string path)
    {
        path = path.ToLowerInvariant();
        return path.Contains("stew") ||
               path.Contains("pie") ||
               path.Contains("soup") ||
               path.Contains("porridge");
    }

    private bool IsComplexMeal(string path)
    {
        path = path.ToLowerInvariant();
        // Bread should be Complex (per requirements)
        return path.Contains("cooked") ||
               path.Contains("bread") ||
               path.Contains("roast") ||
               path.Contains("baked");
    }

    private string GetMealName(ItemStack meal)
    {
        if (meal?.Collectible?.Code == null) return "a meal";
        var path = meal.Collectible.Code.Path.ToLowerInvariant();

        if (path.Contains("bread")) return "bread";
        if (path.Contains("stew")) return "stew";
        if (path.Contains("soup")) return "soup";
        if (path.Contains("pie")) return "pie";
        if (path.Contains("porridge")) return "porridge";
        if (path.Contains("meat")) return "cooked meat";
        return "a meal";
    }

    #endregion
}