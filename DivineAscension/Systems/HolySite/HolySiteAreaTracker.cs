using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.HolySite;

/// <summary>
/// Tracks player positions and emits events when players enter or exit holy site areas.
/// Uses periodic position checks (1 second interval) combined with the optimized
/// chunk-based spatial index in HolySiteManager for efficient lookups.
/// </summary>
public class HolySiteAreaTracker : IHolySiteAreaTracker
{
    private const int TICK_INTERVAL_MS = 1000; // 1 second position checks

    private readonly IEventService _eventService;
    private readonly IWorldService _worldService;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILoggerWrapper _logger;

    // Maps player UID to their current holy site UID (null if not in any site)
    private readonly Dictionary<string, string?> _playerCurrentSites = new();

    private long _callbackId;
    private bool _disposed;
    private bool _isReady; // True after SaveGameLoaded fires and holy site data is available

    public HolySiteAreaTracker(
        IEventService eventService,
        IWorldService worldService,
        IHolySiteManager holySiteManager,
        ILoggerWrapper logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event Action<IServerPlayer, HolySiteData>? OnPlayerEnteredHolySite;

    /// <inheritdoc />
    public event Action<IServerPlayer, HolySiteData>? OnPlayerExitedHolySite;

    /// <inheritdoc />
    public void Initialize()
    {
        _logger.Debug("[DivineAscension] Initializing HolySiteAreaTracker...");

        // Subscribe to player disconnect to clean up tracking state
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);

        // Wait for save game to load before starting position tracking
        // This ensures holy site data is available before we start checking
        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);

        _logger.Debug("[DivineAscension] HolySiteAreaTracker initialized (waiting for save game load)");
    }

    /// <summary>
    /// Called when save game is loaded - starts the periodic position tracking.
    /// </summary>
    private void OnSaveGameLoaded()
    {
        if (_isReady) return; // Already initialized

        _logger.Debug("[DivineAscension] HolySiteAreaTracker: Save game loaded, starting position tracking");
        _callbackId = _eventService.RegisterGameTickListener(OnTick, TICK_INTERVAL_MS);
        _isReady = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);

        if (_isReady)
        {
            _eventService.UnregisterCallback(_callbackId);
        }

        _playerCurrentSites.Clear();

        OnPlayerEnteredHolySite = null;
        OnPlayerExitedHolySite = null;
    }

    /// <inheritdoc />
    public HolySiteData? GetPlayerCurrentSite(string playerUID)
    {
        if (_playerCurrentSites.TryGetValue(playerUID, out var siteUID) && siteUID != null)
        {
            return _holySiteManager.GetHolySite(siteUID);
        }

        return null;
    }

    /// <summary>
    /// Periodic tick handler that checks all online player positions.
    /// </summary>
    private void OnTick(float deltaTime)
    {
        var onlinePlayers = _worldService.GetAllOnlinePlayers();
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            if (player is IServerPlayer serverPlayer)
            {
                CheckPlayerPosition(serverPlayer);
            }
        }
    }

    /// <summary>
    /// Checks a player's position and emits enter/exit events as needed.
    /// Extracts position from entity and delegates to CheckPlayerPositionAt.
    /// </summary>
    internal void CheckPlayerPosition(IServerPlayer player)
    {
        if (player.Entity == null) return;

        var currentPos = player.Entity.Pos.AsBlockPos;
        CheckPlayerPositionAt(player, currentPos);
    }

    /// <summary>
    /// Checks if a player at the given position should trigger enter/exit events.
    /// Separated from CheckPlayerPosition for testability (Entity.Pos is not mockable).
    /// </summary>
    internal void CheckPlayerPositionAt(IServerPlayer player, BlockPos currentPos)
    {
        var playerUID = player.PlayerUID;

        // Use the optimized chunk-based lookup
        var currentSite = _holySiteManager.GetHolySiteAtPosition(currentPos);
        var currentSiteUID = currentSite?.SiteUID;

        // Get previously tracked site
        _playerCurrentSites.TryGetValue(playerUID, out var previousSiteUID);

        // No change - player stayed in same site (or stayed outside)
        if (currentSiteUID == previousSiteUID)
        {
            return;
        }

        // Player exited a site
        if (previousSiteUID != null && currentSiteUID != previousSiteUID)
        {
            var previousSite = _holySiteManager.GetHolySite(previousSiteUID);
            if (previousSite != null)
            {
                _logger.Debug($"[DivineAscension] Player {player.PlayerName} exited holy site '{previousSite.SiteName}'");
                OnPlayerExitedHolySite?.Invoke(player, previousSite);
            }
        }

        // Player entered a site
        if (currentSite != null && currentSiteUID != previousSiteUID)
        {
            _logger.Debug($"[DivineAscension] Player {player.PlayerName} entered holy site '{currentSite.SiteName}'");
            OnPlayerEnteredHolySite?.Invoke(player, currentSite);
        }

        // Update tracking state
        _playerCurrentSites[playerUID] = currentSiteUID;
    }

    /// <summary>
    /// Handles player disconnect by cleaning up tracking state.
    /// </summary>
    private void OnPlayerDisconnect(IServerPlayer player)
    {
        HandlePlayerExitFromHolySite(player);
    }

    internal void HandlePlayerExitFromHolySite(IServerPlayer player)
    {
        var playerUID = player.PlayerUID;

        // If player was in a holy site, emit exit event before cleanup
        if (_playerCurrentSites.TryGetValue(playerUID, out var siteUID) && siteUID != null)
        {
            var site = _holySiteManager.GetHolySite(siteUID);
            if (site != null)
            {
                _logger.Debug($"[DivineAscension] Player {player.PlayerName} disconnected while in holy site '{site.SiteName}'");
                OnPlayerExitedHolySite?.Invoke(player, site);
            }
        }

        _playerCurrentSites.Remove(playerUID);
    }
}
