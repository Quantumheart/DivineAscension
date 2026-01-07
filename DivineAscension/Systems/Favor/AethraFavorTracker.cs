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
public class AethraFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    // Favor values
    private const float FavorPerCropHarvest = 1f;
    private const float FavorPerPlanting = 0.5f;
    private const float FavorSimpleMeal = 1f;
    private const float FavorComplexMeal = 5f;
    private const float FavorGourmetMeal = 10f;
    private static readonly TimeSpan CookingAwardCooldown = TimeSpan.FromSeconds(5);

    // Cache of active Aethra followers for fast lookup
    private readonly HashSet<string> _aethraFollowers = new();
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Event-based: no polling or container tracking needed

    // Simple per-player cooking award rate limit (to prevent rapid repeats)
    private readonly Dictionary<string, DateTime> _lastCookingAwardUtc = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        CropPlantingPatches.OnCropPlanted -= HandleCropPlanted;
        CookingPatches.OnMealCooked -= HandleMealCooked;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _aethraFollowers.Clear();
    }

    public DeityType DeityType { get; } = DeityType.Aethra;

    public void Initialize()
    {
        // Hook into block break event for crop harvesting
        _sapi.Event.BreakBlock += OnBlockBroken;

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

        _sapi.Logger.Notification("[DivineAscension] AethraFavorTracker initialized");
    }

    #region Planting Detection

    private void HandleCropPlanted(IServerPlayer player, Block cropBlock)
    {
        if (!_aethraFollowers.Contains(player.PlayerUID)) return;

        // Award favor for planting crops
        _favorSystem.AwardFavorForAction(player, "planting " + GetCropName(cropBlock), FavorPerPlanting);
    }

    #endregion

    #region Follower Cache Management

    private void RefreshFollowerCache()
    {
        _aethraFollowers.Clear();

        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            if (_playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID) == DeityType)
                _aethraFollowers.Add(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerUID)
    {
        if (_playerProgressionDataManager.GetPlayerDeityType(playerUID) == DeityType)
            _aethraFollowers.Add(playerUID);
        else
            _aethraFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        _aethraFollowers.Remove(player.PlayerUID);
    }

    #endregion

    #region Crop Harvesting Detection

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
        ref EnumHandling handling)
    {
        if (!_aethraFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsCropBlock(block))
            // Award favor for harvesting crops
            _favorSystem.AwardFavorForAction(player, "harvesting " + GetCropName(block), FavorPerCropHarvest);
    }

    /// <summary>
    ///     Checks if a block is a harvestable crop (fully grown only)
    /// </summary>
    private bool IsCropBlock(Block block)
    {
        if (block is not BlockCrop cropBlock) return false;
        if (block?.Code == null) return false;

        var path = block.Code.Path;

        // Check if it's harvested
        if (path.Contains("harvested")) return false;

        // Extract current growth stage from block code (e.g., "crop-carrot-7" → 7)
        var parts = path.Split('-');
        if (parts.Length < 3 || !int.TryParse(parts[^1], out int currentStage))
        {
            _sapi.Logger.Debug($"[AethraFavorTracker] Could not parse growth stage from: {path}");
            return false;
        }

        // Try to get max growth stages from CropProps
        if (cropBlock.CropProps != null)
        {
            int maxStages = cropBlock.CropProps.GrowthStages;
            _sapi.Logger.Debug($"[AethraFavorTracker] Crop: {path}, Current: {currentStage}, Max: {maxStages}");
            return currentStage >= maxStages;
        }

        // Fallback: Check block attributes
        var cropProps = block.Attributes?["cropProps"];
        if (cropProps != null)
        {
            int maxStages = cropProps["growthStages"]?.AsInt() ?? 0;
            if (maxStages > 0)
            {
                _sapi.Logger.Debug(
                    $"[AethraFavorTracker] Crop (attrs): {path}, Current: {currentStage}, Max: {maxStages}");
                return currentStage >= maxStages;
            }
        }

        _sapi.Logger.Warning($"[AethraFavorTracker] Could not determine max stages for crop: {path}");
        return false;
    }

    private string GetCropName(Block block)
    {
        if (block?.Code == null) return "crops";

        var path = block.Code.Path;

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

    #region Cooking (Event-Driven)

    private void HandleMealCooked(string playerUid, ItemStack cookedStack, BlockPos pos)
    {
        // Ensure valid player and follower of Aethra
        var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
        if (player == null) return;
        if (!_aethraFollowers.Contains(player.PlayerUID)) return;

        // Rate limit per player
        var now = DateTime.UtcNow;
        if (_lastCookingAwardUtc.TryGetValue(playerUid, out var last) && now - last < CookingAwardCooldown) return;

        var favor = CalculateMealFavor(cookedStack);
        if (favor <= 0) return;

        _favorSystem.AwardFavorForAction(player, "cooking " + GetMealName(cookedStack), favor);
        _lastCookingAwardUtc[playerUid] = now;

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