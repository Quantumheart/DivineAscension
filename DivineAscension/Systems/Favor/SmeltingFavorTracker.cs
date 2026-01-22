using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Tracks metal pouring into molds and awards favor to Craft followers
/// </summary>
public class SmeltingFavorTracker(
    ILoggerWrapper logger,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    // Favor values
    private const float BaseFavorPerUnit = 0.01f;
    private const float IngotMoldMultiplier = 0.4f; // 60% reduction for ingot molds
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public void Dispose()
    {
        MoldPourPatches.OnMoldPoured -= HandleMoldPoured;
        _logger.Debug($"[DivineAscension] SmeltingFavorTracker disposed (ID: {_instanceId})");
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Craft;

    public void Initialize()
    {
        MoldPourPatches.OnMoldPoured += HandleMoldPoured;
        _logger.Notification($"[DivineAscension] SmeltingFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleMoldPoured(string? playerUid, BlockPos pos, int deltaUnits, bool isToolMold)
    {
        // If player uid not supplied (rare), attribute to nearest player within 8 blocks
        var effectiveUid = playerUid ?? FindNearestPlayerUid(pos, 8);
        if (effectiveUid == null) return;
        AwardFavorForPouring(effectiveUid, deltaUnits, isToolMold);
        _logger.Debug(
            $"[SmeltingFavorTracker:{_instanceId}] Mold pour detected at {pos}, +{deltaUnits} into {(isToolMold ? "tool" : "ingot")} mold. Player: {effectiveUid ?? "unknown"}");
    }

    private string? FindNearestPlayerUid(BlockPos pos, int radius)
    {
        var bestDistSq = (radius + 0.5) * (radius + 0.5);
        string? bestUid = null;

        foreach (var p in _worldService.GetAllOnlinePlayers())
        {
            if (p is not IServerPlayer sp) continue;
            var epos = sp.Entity?.Pos?.AsBlockPos;
            if (epos == null) continue;

            var dx = epos.X - pos.X;
            var dy = epos.Y - pos.Y;
            var dz = epos.Z - pos.Z;
            var distSq = dx * (double)dx + dy * (double)dy + dz * (double)dz;
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

        var player = _worldService.GetPlayerByUID(playerId) as IServerPlayer;
        if (player == null)
            return;

        // Check if player follows Craft domain
        if (_playerProgressionDataManager.GetPlayerDeityType(playerId) != DeityDomain.Craft)
            return;

        // Calculate favor
        var favor = CalculatePouringFavor(unitsPoured, isToolMold);

        if (favor >= 0.01f) // Only award if meaningful amount
        {
            // Use fractional favor accumulation
            _favorSystem.AwardFavorForAction(player, "smelting", favor);

            _logger.Debug(
                $"[SmeltingFavorTracker] Awarded {favor:F2} favor to {player.PlayerName} for pouring {unitsPoured} units into {(isToolMold ? "tool" : "ingot")} mold");
        }
    }

    private float CalculatePouringFavor(int unitsPoured, bool isToolMold)
    {
        if (isToolMold)
            // Tool/weapon molds: Full favor
            return unitsPoured * BaseFavorPerUnit;

        // Ingot molds: Reduced favor
        return unitsPoured * BaseFavorPerUnit * IngotMoldMultiplier;
    }
}