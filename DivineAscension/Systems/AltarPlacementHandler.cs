using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Detects altar placement and automatically creates holy sites from land claims.
/// Any religion member can place altars - they automatically consecrate the claim as a holy site.
/// Subscribes to AltarEventEmitter.OnAltarPlaced event for efficient altar-specific detection.
/// </summary>
public class AltarPlacementHandler : IDisposable
{
    private readonly IHolySiteManager _holySiteManager;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;
    private readonly IPlayerMessengerService _messenger;
    private readonly ILogger _logger;
    private readonly AltarEventEmitter _altarEventEmitter;

    public AltarPlacementHandler(
        ILogger logger,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IWorldService worldService,
        IPlayerMessengerService messenger,
        AltarEventEmitter altarEventEmitter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _altarEventEmitter = altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Placement Handler...");
        _altarEventEmitter.OnAltarPlaced += OnAltarPlaced;
        _logger.Notification("[DivineAscension] Altar Placement Handler initialized");
    }

    public void Dispose()
    {
        _altarEventEmitter.OnAltarPlaced -= OnAltarPlaced;
    }

    [ExcludeFromCodeCoverage]
    private void OnAltarPlaced(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        try
        {
            // Validate the ItemStack being placed (block hasn't been set in world yet during DoPlaceBlock)
            if (withItemStack?.Collectible?.Code == null)
                return;

            if (!IsAltarItem(withItemStack))
                return;

            _logger.Debug($"[DivineAscension] Player {player.PlayerName} placing an altar at {blockSel.Position}");

            // Check religion membership
            var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null)
            {
                _messenger.SendMessage(player, "You must be in a religion to create holy sites with altars.", EnumChatType.CommandError);
                return; // Allow altar placement but don't create holy site
            }

            // Get land claims at altar position
            var landClaims = _worldService.World.Claims.Get(blockSel.Position);
            if (landClaims == null || landClaims.Length == 0)
            {
                _messenger.SendMessage(player, "Altar placed, but no land claim detected. Holy site not created.", EnumChatType.CommandError);
                return; // Allow placement but warn
            }

            // Find player-owned claim
            var playerClaim = FindPlayerClaim(landClaims, player.PlayerUID);
            if (playerClaim == null)
            {
                _messenger.SendMessage(player, "You can only create holy sites on your own land claims.", EnumChatType.CommandError);
                return;
            }

            // Validate claim has areas
            if (playerClaim.Areas == null || playerClaim.Areas.Count == 0)
            {
                _messenger.SendMessage(player, "Land claim has no valid areas. Holy site not created.", EnumChatType.CommandError);
                return;
            }

            // Auto-generate site name
            string siteName = GenerateSiteName(religion, player);

            // Debug: Log land claim area coordinates and player position
            _logger.Debug($"[DivineAscension] Altar block position: {blockSel.Position}");
            _logger.Debug($"[DivineAscension] Player entity position: {player.Entity.ServerPos.AsBlockPos}");
            _logger.Debug($"[DivineAscension] Land claim has {playerClaim.Areas.Count} areas:");
            foreach (var area in playerClaim.Areas)
            {
                _logger.Debug($"[DivineAscension]   Area: ({area.X1},{area.Y1},{area.Z1}) to ({area.X2},{area.Y2},{area.Z2})");
            }

            // Create holy site with altar
            var holySite = _holySiteManager.ConsecrateHolySiteWithAltar(
                religion.ReligionUID,
                siteName,
                playerClaim.Areas,
                player.PlayerUID,
                player.PlayerName,
                blockSel.Position
            );

            if (holySite != null)
            {
                var tier = holySite.GetTier();
                var prayerMult = holySite.GetPrayerMultiplier();
                _messenger.SendMessage(player,
                    $"Holy site '{siteName}' consecrated! Tier {tier}, Prayer bonus: {prayerMult:F1}x",
                    EnumChatType.CommandSuccess);

                _logger.Notification($"[DivineAscension] Holy site '{siteName}' created by {player.PlayerName} via altar placement");
            }
            else
            {
                _messenger.SendMessage(player,
                    "Failed to create holy site. Check prestige limits or overlapping sites.",
                    EnumChatType.CommandError);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarPlacementHandler: {ex.Message}");
        }
    }

    [ExcludeFromCodeCoverage]
    private bool IsAltarItem(ItemStack itemStack)
    {
        // Match any item/block with code path starting with "altar"
        return itemStack?.Collectible?.Code?.Path?.StartsWith("altar") ?? false;
    }

    private bool IsAltarBlock(Block block)
    {
        // Match any block with code path starting with "altar"
        return block?.Code?.Path?.StartsWith("altar") ?? false;
    }

    private LandClaim? FindPlayerClaim(LandClaim[] claims, string playerUID)
    {
        foreach (var claim in claims)
        {
            // Check if this claim belongs to the player
            if (claim.OwnedByPlayerUid == playerUID)
                return claim;
        }
        return null;
    }

    private string GenerateSiteName(Data.ReligionData religion, IServerPlayer player)
    {
        var existingCount = _holySiteManager.GetReligionHolySites(religion.ReligionUID).Count;
        return $"{religion.ReligionName} - Site {existingCount + 1}";
    }
}
