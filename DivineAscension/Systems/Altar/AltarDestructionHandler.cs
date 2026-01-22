using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Altar;

/// <summary>
/// Detects altar destruction and automatically deconsecrates associated holy sites.
/// When an altar block that created a holy site is destroyed, the holy site is removed.
/// Subscribes to the AltarEventEmitter.OnAltarBroken event for efficient altar-specific detection.
/// </summary>
public class AltarDestructionHandler : IDisposable
{
    private readonly AltarEventEmitter _altarEventEmitter;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILogger _logger;
    private readonly IPlayerMessengerService _messenger;

    public AltarDestructionHandler(
        ILogger logger,
        IHolySiteManager holySiteManager,
        IPlayerMessengerService messenger,
        AltarEventEmitter altarEventEmitter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _altarEventEmitter = altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));
    }

    public void Dispose()
    {
        _altarEventEmitter.OnAltarBroken -= OnAltarBroken;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Destruction Handler...");
        _altarEventEmitter.OnAltarBroken += OnAltarBroken;
        _logger.Notification("[DivineAscension] Altar Destruction Handler initialized");
    }

    [ExcludeFromCodeCoverage]
    private void OnAltarBroken(IServerPlayer player, BlockPos pos)
    {
        try
        {
            _logger.Debug($"[DivineAscension] Player {player.PlayerName} broke an altar at {pos}");

            // Find holy site at this altar position
            var holySite = _holySiteManager.GetHolySiteByAltarPosition(pos);
            if (holySite == null)
            {
                _logger.Debug($"[DivineAscension] No holy site found at altar position {pos}");
                return; // Altar exists but no holy site (or legacy site without altar position)
            }

            // Deconsecrate the holy site
            DeconsecrateHolySiteAfterAltarDestruction(player, holySite);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarDestructionHandler: {ex.Message}");
        }
    }

    internal void DeconsecrateHolySiteAfterAltarDestruction(IServerPlayer player, HolySiteData holySite)
    {
        bool success = _holySiteManager.DeconsacrateHolySite(holySite.SiteUID);
        if (success)
        {
            _messenger.SendMessage(player,
                $"Holy site '{holySite.SiteName}' has been deconsecrated. The altar has been destroyed.",
                EnumChatType.Notification);

            _logger.Notification(
                $"[DivineAscension] Holy site '{holySite.SiteName}' deconsecrated due to altar destruction by {player.PlayerName}");
        }
        else
        {
            _logger.Warning(
                $"[DivineAscension] Failed to deconsecrate holy site {holySite.SiteUID} after altar destruction");
        }
    }
}