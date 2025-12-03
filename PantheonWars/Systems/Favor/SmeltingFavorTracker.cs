using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using PantheonWars.Systems.Patches;

namespace PantheonWars.Systems.Favor;

/// <summary>
/// Tracks metal pouring into molds and awards favor to Khoras followers
/// </summary>
public class SmeltingFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;

    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Favor values
    private const float BaseFavorPerUnit = 0.01f;
    private const float IngotMoldMultiplier = 0.4f; // 60% reduction for ingot molds

    private readonly Guid _instanceId = Guid.NewGuid();

    public void Initialize()
    {
        MoldPourPatches.OnMoldPoured += HandleMoldPoured;
        _sapi.Logger.Notification($"[PantheonWars] SmeltingFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleMoldPoured(string? playerUid, BlockPos pos, int deltaUnits, bool isToolMold)
    {
        // If player uid not supplied (rare), attribute to nearest player within 8 blocks
        string? effectiveUid = playerUid ?? FindNearestPlayerUid(pos, 8);
        if (effectiveUid == null) return;
        AwardFavorForPouring(effectiveUid, deltaUnits, isToolMold);
        _sapi.Logger.Debug($"[SmeltingFavorTracker:{_instanceId}] Mold pour detected at {pos}, +{deltaUnits} into {(isToolMold ? "tool" : "ingot")} mold. Player: {effectiveUid ?? "unknown"}");
    }

    private string? FindNearestPlayerUid(BlockPos pos, int radius)
    {
        double bestDistSq = (radius + 0.5) * (radius + 0.5);
        string? bestUid = null;

        foreach (var p in _sapi.World.AllOnlinePlayers)
        {
            if (p is not IServerPlayer sp) continue;
            var epos = sp.Entity?.Pos?.AsBlockPos;
            if (epos == null) continue;

            int dx = epos.X - pos.X;
            int dy = epos.Y - pos.Y;
            int dz = epos.Z - pos.Z;
            double distSq = dx * (double)dx + dy * (double)dy + dz * (double)dz;
            if (distSq <= bestDistSq)
            {
                bestDistSq = distSq;
                bestUid = sp.PlayerUID;
            }
        }

        return bestUid;
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

    public void Dispose()
    {
        MoldPourPatches.OnMoldPoured -= HandleMoldPoured;
        _sapi.Logger.Debug($"[PantheonWars] SmeltingFavorTracker disposed (ID: {_instanceId})");
    }
}
