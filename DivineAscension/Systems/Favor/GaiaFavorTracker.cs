using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class GaiaFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    // --- Brick Placement Tracking (Part B requirement) ---
    private const int FavorPerBrickPlacement = 2;
    private const long BrickPlacementCooldownMs = 5000; // 5 seconds between brick placement favor awards
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly Dictionary<string, long> _lastBrickPlacementTime = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        ClayFormingPatches.OnClayFormingFinished -= HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired -= HandlePitKilnFired;
        _sapi.Event.DidPlaceBlock -= OnBlockPlaced;
        _sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        _lastBrickPlacementTime.Clear();
        _sapi.Logger.Debug($"[DivineAscension] GaiaFavorTracker disposed (ID: {_instanceId})");
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Stone;

    public void Initialize()
    {
        ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired += HandlePitKilnFired;
        // Track clay brick placements
        _sapi.Event.DidPlaceBlock += OnBlockPlaced;
        // Clean up cooldown data on player disconnect
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
        _sapi.Logger.Notification($"[DivineAscension] GaiaFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleClayFormingFinished(IServerPlayer player, ItemStack stack, int clayConsumed)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        if (clayConsumed > 0)
        {
            // Clay consumed already accounts for the total batch (don't multiply by stack size)
            _favorSystem.AwardFavorForAction(player, "Pottery Crafting", clayConsumed);

            _sapi.Logger.Debug(
                $"[GaiaFavorTracker] Awarded {clayConsumed} favor for clay forming " +
                $"(stack size: {stack?.StackSize ?? 1}, item: {stack?.Collectible?.Code?.Path ?? "unknown"})");
        }
    }

    private void OnBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(byPlayer.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        var placedBlock = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!IsBrickBlock(placedBlock)) return;

        // Debounce check - limit favor awards to one per cooldown period
        var currentTime = _sapi.World.ElapsedMilliseconds;
        if (_lastBrickPlacementTime.TryGetValue(byPlayer.PlayerUID, out var lastTime))
        {
            if (currentTime - lastTime < BrickPlacementCooldownMs)
                return; // Still in cooldown
        }

        _favorSystem.AwardFavorForAction(byPlayer, "placing clay bricks", FavorPerBrickPlacement);
        _lastBrickPlacementTime[byPlayer.PlayerUID] = currentTime;
    }

    private static bool IsBrickBlock(Block block)
    {
        if (block?.Code == null) return false;
        var path = block.Code.Path.ToLowerInvariant();

        // Fired and raw brick blocks typically contain "brick" in the code path
        return path.Contains("brick");
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

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking data to prevent memory leaks
        _lastBrickPlacementTime.Remove(player.PlayerUID);
    }

    private void HandlePitKilnFired(string playerUid, List<ItemStack> firedItems)
    {
        _sapi.Logger.Debug(
            $"[DivineAscension] GaiaFavorTracker ({_instanceId}): Handling PitKilnFired for {playerUid}");

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
            totalFavor += clayEstimate * stackSize;

            _sapi.Logger.Debug(
                $"[GaiaFavorTracker] Pit kiln item: {stack?.Collectible?.Code?.Path ?? "unknown"}, " +
                $"clay estimate: {clayEstimate}, stack: {stackSize}, favor: {clayEstimate * stackSize}");
        }

        if (totalFavor > 0)
        {
            _favorSystem.AwardFavorForAction(playerUid, "Pottery firing", totalFavor, deityType);
            _sapi.Logger.Debug($"[GaiaFavorTracker] Total pit kiln favor awarded: {totalFavor}");
        }
    }
}