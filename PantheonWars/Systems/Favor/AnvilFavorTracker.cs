using System;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

/// <summary>
/// Awards favor to Khoras followers when an anvil recipe is completed (event-driven)
/// </summary>
public class AnvilFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem)
    : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;

    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly Guid _instanceId = Guid.NewGuid();

    // Favor values per tier
    private const int FavorLowTier = 5;    // Copper
    private const int FavorMidTier = 10;   // Bronze, gold 
    private const int FavorHighTier = 15;  // special alloys, iron
    private const int FavorEliteTier = 20; // Steel

    // Automation penalty
    private const float HelveHammerPenalty = 0.65f; // 35% reduction

    public void Initialize()
    {
        AnvilPatches.OnAnvilRecipeCompleted += HandleAnvilRecipeCompleted;
        _sapi.Logger.Notification($"[PantheonWars] AnvilFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleAnvilRecipeCompleted(string? playerUid, BlockPos pos, ItemStack? outputPreview)
    {
        if (string.IsNullOrEmpty(playerUid)) return;

        var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
        if (player == null) return;

        // Verify religion
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (religionData.ActiveDeity != DeityType.Khoras) return;

        // Compute favor from output preview (fallback to mid tier)
        int baseFavor = outputPreview != null ? CalculateBaseFavor(outputPreview) : FavorMidTier;

        // Detect helve hammer automation by adjacency at completion time
        bool usedHelve = CheckHelveHammerUsage(pos);
        int finalFavor = ApplyAutomationPenalty(baseFavor, usedHelve);

        _favorSystem.AwardFavorForAction(player, "smithing", finalFavor);
        _sapi.Logger.Debug($"[AnvilFavorTracker:{_instanceId}] Awarded {finalFavor} favor to {player.PlayerName} for smithing (base {baseFavor}, helve:{usedHelve}) at {pos}");
    }

    // Removed tick scanning and state tracking in favor of event-driven completion handling


    #region Favor Calculation

    private int CalculateBaseFavor(ItemStack? outputItem)
    {
        if (outputItem == null) return FavorLowTier;

        if (IsEliteTier(outputItem)) return FavorEliteTier;
        if (IsHighTier(outputItem)) return FavorHighTier;
        if (IsMidTier(outputItem)) return FavorMidTier;
        if (IsLowTier(outputItem)) return FavorLowTier;

        return FavorLowTier; // Default
    }

    private bool IsLowTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("copper");
    }

    private bool IsMidTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("bronze") ||  path.Contains("gold") || path.Contains("silver") || path.Contains("tinbronze") || path.Contains("black") && path.Contains("bronze");
    }

    private bool IsHighTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("meteoric") || path.Contains("iron");
    }

    private bool IsEliteTier(ItemStack item)
    {
        var path = item.Collectible?.Code?.Path ?? "";
        return path.Contains("steel");
    }

    private int ApplyAutomationPenalty(int baseFavor, bool wasHelveHammered)
    {
        if (wasHelveHammered)
        {
            return (int)(baseFavor * HelveHammerPenalty);
        }
        return baseFavor;
    }

    #endregion

    #region Helve Hammer Detection

    private bool CheckHelveHammerUsage(BlockPos anvilPos)
    {
        // Check if helve hammer is adjacent to this anvil
        // Helve hammer block code: "helvehammer"
        foreach (var face in BlockFacing.ALLFACES)
        {
            var adjacentPos = anvilPos.AddCopy(face);
            var block = _sapi.World.BlockAccessor.GetBlock(adjacentPos);

            if (block?.Code?.Path.Contains("helvehammer") == true)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public void Dispose()
    {
        AnvilPatches.OnAnvilRecipeCompleted -= HandleAnvilRecipeCompleted;
        _sapi.Logger.Debug($"[PantheonWars] AnvilFavorTracker disposed (ID: {_instanceId})");
    }
}
