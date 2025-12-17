using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class GaiaFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    // --- Brick Placement Tracking (Part B requirement) ---
    private const int FavorPerBrickPlacement = 2;
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly Guid _instanceId = Guid.NewGuid();

    private readonly IPlayerReligionDataManager _playerReligionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        ClayFormingPatches.OnClayFormingFinished -= HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired -= HandlePitKilnFired;
        _sapi.Event.DidPlaceBlock -= OnBlockPlaced;
        _sapi.Logger.Debug($"[DivineAscension] GaiaFavorTracker disposed (ID: {_instanceId})");
    }

    public DeityType DeityType { get; } = DeityType.Gaia;

    public void Initialize()
    {
        ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired += HandlePitKilnFired;
        // Track clay brick placements
        _sapi.Event.DidPlaceBlock += OnBlockPlaced;
        _sapi.Logger.Notification($"[DivineAscension] GaiaFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleClayFormingFinished(IServerPlayer player, ItemStack stack)
    {
        // Verify religion
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Gaia) return;

        var favor = CalculateFavor(stack);
        if (favor > 0) _favorSystem.AwardFavorForAction(player, "Pottery Crafting", favor);
    }

    private void OnBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        // Verify religion
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(byPlayer.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Gaia) return;

        var placedBlock = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsBrickBlock(placedBlock))
            _favorSystem.AwardFavorForAction(byPlayer, "placing clay bricks", FavorPerBrickPlacement);
    }

    private static bool IsBrickBlock(Block block)
    {
        if (block?.Code == null) return false;
        var path = block.Code.Path.ToLowerInvariant();

        // Fired and raw brick blocks typically contain "brick" in the code path
        return path.Contains("brick");
    }

    private int CalculateFavor(ItemStack stack)
    {
        if (stack?.Collectible?.Code == null) return 0;
        var path = stack.Collectible.Code.Path?.ToLowerInvariant() ?? string.Empty;

        // Specific mappings first
        if (path.Contains("rawbrick") || path.Contains("brick")) return 1;
        if (path.Contains("mold") || path.Contains("crucible")) return 2;
        if (path.Contains("planter") || path.Contains("flowerpot")) return 4;
        if (path.Contains("storagevessel") || path.Contains("vessel")) return 5;

        // Broad inclusiveness for clay/ceramic items
        var isClayLike =
            path.Contains("clay") ||
            path.Contains("clayform") ||
            path.Contains("clayforming") ||
            path.Contains("ceramic") ||
            path.Contains("pottery") ||
            path.Contains("bowl") ||
            path.Contains("pot") ||
            path.Contains("vase") ||
            path.Contains("jar") ||
            path.Contains("jug") ||
            path.Contains("amphora") ||
            path.Contains("urn") ||
            path.Contains("tile") ||
            path.Contains("crock") ||
            path.Contains("fireclay");

        if (isClayLike) return 3; // Default for other pottery (bowls, pots, vases, crocks, etc.)

        // Not a clay/ceramic item
        return 0;
    }

    private void HandlePitKilnFired(string playerUid, List<ItemStack> firedItems)
    {
        _sapi.Logger.Debug(
            $"[DivineAscension] GaiaFavorTracker ({_instanceId}): Handling PitKilnFired for {playerUid}");

        if (string.IsNullOrEmpty(playerUid)) return;

        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);
        if (religionData.ActiveDeity != DeityType.Gaia) return;

        float totalFavor = 0;
        foreach (var stack in firedItems) totalFavor += CalculateFavor(stack);

        if (totalFavor > 0) _favorSystem.AwardFavorForAction(playerUid, "Pottery firing", totalFavor);
    }
}