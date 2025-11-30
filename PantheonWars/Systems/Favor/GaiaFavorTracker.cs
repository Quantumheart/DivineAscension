using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class GaiaFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Gaia;

    private readonly IPlayerReligionDataManager _playerReligionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly Guid _instanceId = Guid.NewGuid();

    public void Initialize()
    {
        ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired += HandlePitKilnFired;
        _sapi.Logger.Notification($"[PantheonWars] GaiaFavorTracker initialized (ID: {_instanceId})");
    }

    public void Dispose()
    {
        ClayFormingPatches.OnClayFormingFinished -= HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired -= HandlePitKilnFired;
        _sapi.Logger.Debug($"[PantheonWars] GaiaFavorTracker disposed (ID: {_instanceId})");
    }

    private void HandleClayFormingFinished(IServerPlayer player, ItemStack stack)
    {
        // Verify religion
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Gaia) return;

        int favor = CalculateFavor(stack);
        if (favor > 0)
        {
            _favorSystem.AwardFavorForAction(player, "Pottery Crafting", favor);
        }
    }

    private int CalculateFavor(ItemStack stack)
    {
        if (stack?.Collectible?.Code == null) return 0;
        string path = stack.Collectible.Code.Path;
        
        if (path.Contains("rawbrick")) return 1;
        if (path.Contains("mold") || path.Contains("crucible")) return 2;
        if (path.Contains("planter") || path.Contains("flowerpot")) return 4;
        if (path.Contains("storagevessel")) return 5;
        
        return 3; // Default for other pottery (bowls, pots, etc)
    }
    
    private void HandlePitKilnFired(string playerUid, List<ItemStack> firedItems)
    {
        _sapi.Logger.Debug($"[PantheonWars] GaiaFavorTracker ({_instanceId}): Handling PitKilnFired for {playerUid}");

        if (string.IsNullOrEmpty(playerUid)) return;

        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);
        if (religionData.ActiveDeity != DeityType.Gaia) return;

        float totalFavor = 0;
        foreach (var stack in firedItems)
        {
            totalFavor += CalculateFavor(stack);
        }

        if (totalFavor > 0)
        {
            _favorSystem.AwardFavorForAction(playerUid, "Pottery firing", totalFavor);
        }
    }
}