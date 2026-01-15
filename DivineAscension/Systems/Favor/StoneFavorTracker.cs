using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class StoneFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    // --- Stone Gathering Tracking ---
    private const int FavorPerStoneBlock = 1; // Base stone blocks (granite, andesite, etc.)
    private const int FavorPerValuableStone = 2; // Valuable stones (marble, obsidian)

    // --- Construction Tracking ---
    private const int FavorPerBrickPlacement = 5; // Increased from 2 to 5
    private const int FavorPerConstructionBlock = 1; // Stone bricks, slabs, stairs
    private const long ConstructionCooldownMs = 1000; // 1 second cooldown (reduced from 5s)

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly Dictionary<string, long> _lastConstructionTime = new(); // Renamed for clarity

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        ClayFormingPatches.OnClayFormingFinished -= HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired -= HandlePitKilnFired;
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _sapi.Event.DidPlaceBlock -= OnBlockPlaced;
        _sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        _lastConstructionTime.Clear();
        _sapi.Logger.Debug($"[DivineAscension] StoneFavorTracker disposed (ID: {_instanceId})");
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Stone;

    public void Initialize()
    {
        ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired += HandlePitKilnFired;
        // Track stone gathering
        _sapi.Event.BreakBlock += OnBlockBroken;
        // Track construction (brick/stone placement)
        _sapi.Event.DidPlaceBlock += OnBlockPlaced;
        // Clean up cooldown data on player disconnect
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
        _sapi.Logger.Notification($"[DivineAscension] StoneFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleClayFormingFinished(IServerPlayer player, ItemStack stack, int clayConsumed)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        if (clayConsumed > 0)
        {
            // Clay consumed already accounts for the total batch (don't multiply by stack size)
            // Phase 1 improvement: Double pottery favor rewards
            var favorAmount = clayConsumed * 2;
            _favorSystem.AwardFavorForAction(player, "Pottery Crafting", favorAmount);

            _sapi.Logger.Debug(
                $"[StoneFavorTracker] Awarded {favorAmount} favor for clay forming " +
                $"(clay consumed: {clayConsumed}, stack size: {stack?.StackSize ?? 1}, item: {stack?.Collectible?.Code?.Path ?? "unknown"})");
        }
    }

    private void OnBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(byPlayer.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        var placedBlock = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!IsConstructionBlock(placedBlock, out var favor)) return;

        // Debounce check - limit favor awards to one per cooldown period
        var currentTime = _sapi.World.ElapsedMilliseconds;
        if (_lastConstructionTime.TryGetValue(byPlayer.PlayerUID, out var lastTime))
        {
            if (currentTime - lastTime < ConstructionCooldownMs)
                return; // Still in cooldown
        }

        _favorSystem.AwardFavorForAction(byPlayer, "construction", favor);
        _lastConstructionTime[byPlayer.PlayerUID] = currentTime;

        _sapi.Logger.Debug(
            $"[StoneFavorTracker] Awarded {favor} favor to {byPlayer.PlayerName} for placing {placedBlock.Code.Path}");
    }

    /// <summary>
    /// Checks if a block is a construction block and returns the favor amount
    /// </summary>
    private static bool IsConstructionBlock(Block block, out float favor)
    {
        favor = 0;
        if (block?.Code == null) return false;

        var path = block.Code.Path.ToLowerInvariant();

        // Bricks (raw and fired) - highest priority for Stone domain
        if (path.Contains("brick"))
        {
            favor = FavorPerBrickPlacement;
            return true;
        }

        // Stone construction blocks (slabs, stairs, walls, etc.)
        if (IsStoneConstructionBlock(path))
        {
            favor = FavorPerConstructionBlock;
            return true;
        }

        // Clay/ceramic construction blocks
        if (path.Contains("clay") || path.Contains("ceramic"))
        {
            favor = FavorPerConstructionBlock;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a block is a stone-based construction block
    /// </summary>
    private static bool IsStoneConstructionBlock(string path)
    {
        // Common stone materials
        var stoneTypes = new[]
        {
            "granite", "andesite", "basalt", "peridotite", "limestone",
            "sandstone", "claystone", "chalk", "chert", "marble"
        };

        foreach (var stoneType in stoneTypes)
        {
            if (!path.Contains(stoneType)) continue;

            // Construction block types (stairs, slabs, walls, etc.)
            if (path.Contains("stairs") || path.Contains("slab") || path.Contains("wall") ||
                path.Contains("pillar") || path.Contains("column") || path.Contains("hewn"))
            {
                return true;
            }
        }

        return false;
    }

    private int EstimateClayFromFiredItem(ItemStack stack)
    {
        if (stack?.Collectible?.Code == null) return 0;

        var path = stack.Collectible.Code.Path?.ToLowerInvariant() ?? string.Empty;

        // Known clay amounts from game data (based on screenshot)
        if (path.Contains("storagevessel") || path.Contains("vessel")) return 35;
        if (path.Contains("planter")) return 18;
        if (path.Contains("anvil") && path.Contains("mold")) return 28;
        if (path.Contains("prospecting") && path.Contains("mold")) return 13;
        if (path.Contains("pickaxe") && path.Contains("mold")) return 12;
        if (path.Contains("hammer") && path.Contains("mold")) return 12;
        if (path.Contains("blade") && path.Contains("mold")) return 12;
        if (path.Contains("lamel") && path.Contains("mold")) return 11;
        if (path.Contains("axe") && path.Contains("mold")) return 11;
        if (path.Contains("shovel") && path.Contains("mold")) return 11;
        if (path.Contains("hoe") && path.Contains("mold")) return 12;
        if (path.Contains("helve") && path.Contains("mold")) return 6;
        if (path.Contains("ingot") && path.Contains("mold")) return 2; // 2 or 5
        if (path.Contains("wateringcan")) return 10;
        if (path.Contains("jug")) return 5;
        if (path.Contains("shingles")) return 4;
        if (path.Contains("flowerpot")) return 4; // 4 or 23
        if (path.Contains("cookingpot") || path.Contains("pot")) return 4; // 4 or 24
        if (path.Contains("bowl")) return 4; // Conservative estimate (1 or 4)
        if (path.Contains("crucible")) return 2; // 2 or 13
        if (path.Contains("crock")) return 2; // 2 or 14
        if (path.Contains("brick")) return 1;

        // Default for other ceramic items
        return path.Contains("clay") || path.Contains("ceramic") ? 3 : 0;
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
        ref EnumHandling handling)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!IsStoneBlock(block, out var favor)) return;

        _favorSystem.AwardFavorForAction(player, "gathering stone", favor);

        _sapi.Logger.Debug(
            $"[StoneFavorTracker] Awarded {favor} favor to {player.PlayerName} for mining {block.Code.Path}");
    }

    /// <summary>
    /// Checks if a block is a stone block and returns the favor amount
    /// </summary>
    private static bool IsStoneBlock(Block block, out float favor)
    {
        favor = 0;
        if (block?.Code == null) return false;

        var path = block.Code.Path.ToLowerInvariant();

        // Valuable stones - higher favor
        if (IsValuableStone(path))
        {
            favor = FavorPerValuableStone;
            return true;
        }

        // Regular stone blocks
        if (IsRegularStone(path))
        {
            favor = FavorPerStoneBlock;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a block is valuable stone (marble, obsidian, etc.)
    /// </summary>
    private static bool IsValuableStone(string path)
    {
        // Valuable decorative/rare stones
        return path.Contains("marble") || path.Contains("obsidian");
    }

    /// <summary>
    /// Checks if a block is regular stone
    /// </summary>
    private static bool IsRegularStone(string path)
    {
        // Check if it's a rock/stone block but NOT ore
        if (path.StartsWith("ore-", StringComparison.Ordinal))
            return false; // Ore blocks are handled by MiningFavorTracker (Craft domain)

        // Common stone types
        var stoneTypes = new[]
        {
            "granite", "andesite", "basalt", "peridotite", "limestone",
            "sandstone", "claystone", "chalk", "chert", "suevite",
            "phyllite", "slate", "conglomerate", "shale"
        };

        foreach (var stoneType in stoneTypes)
        {
            if (path.Contains(stoneType))
                return true;
        }

        // Generic "rock" or "stone" blocks
        if (path.StartsWith("rock-") || path.StartsWith("stone-") || path.Contains("-rock-") ||
            path.Contains("-stone-"))
            return true;

        return false;
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking data to prevent memory leaks
        _lastConstructionTime.Remove(player.PlayerUID);
    }

    private void HandlePitKilnFired(string playerUid, List<ItemStack> firedItems)
    {
        _sapi.Logger.Debug(
            $"[DivineAscension] StoneFavorTracker ({_instanceId}): Handling PitKilnFired for {playerUid}");

        if (string.IsNullOrEmpty(playerUid)) return;

        var deityType = _playerProgressionDataManager.GetPlayerDeityType(playerUid);
        if (deityType != DeityDomain.Stone) return;

        float totalFavor = 0;
        foreach (var stack in firedItems)
        {
            // For pit kiln, estimate per-item clay and multiply by stack size
            // (unlike clay forming where the recipe gives us the total)
            int clayEstimate = EstimateClayFromFiredItem(stack);
            int stackSize = stack?.StackSize ?? 1;
            // Phase 1 improvement: Double pottery favor rewards
            totalFavor += (clayEstimate * stackSize * 2);

            _sapi.Logger.Debug(
                $"[StoneFavorTracker] Pit kiln item: {stack?.Collectible?.Code?.Path ?? "unknown"}, " +
                $"clay estimate: {clayEstimate}, stack: {stackSize}, favor: {clayEstimate * stackSize * 2}");
        }

        if (totalFavor > 0)
        {
            _favorSystem.AwardFavorForAction(playerUid, "Pottery firing", totalFavor, deityType);
            _sapi.Logger.Debug($"[StoneFavorTracker] Total pit kiln favor awarded: {totalFavor}");
        }
    }
}