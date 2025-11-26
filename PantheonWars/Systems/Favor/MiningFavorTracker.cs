using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class MiningFavorTracker(IPlayerReligionDataManager playerReligionDataManager, ICoreServerAPI sapi, FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly HashSet<string> _oreBlocks =
    [
        "ore-poor", "ore-medium", "ore-rich", "ore-bountiful"
    ];
    
    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;
    }
    
    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData?.ActiveDeity != DeityType) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsOreBlock(block))
        {
            _favorSystem.AwardFavorForAction(player, "mining ore", 2);
        }
    }

    private bool IsOreBlock(Block block)
    {
        if (block?.Code is null) return false;
        foreach (var oreBlock in _oreBlocks)
        {
            if (block.Code.Path.Contains(oreBlock))
            {
                return true;
            }
        }

        return false;
    }
    
    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
    }
}