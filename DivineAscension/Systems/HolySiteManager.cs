using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems;

/// <summary>
/// Manages holy site creation and queries.
/// Holy sites match land claim boundaries and provide territory and prayer bonuses based on tier.
/// Site limits are based on religion prestige tier (max 5 sites).
///
/// Thread Safety:
/// - Uses ConcurrentDictionary for all indexes (thread-safe reads)
/// - Uses lock for multi-step operations (create, delete, load, save)
/// - Query methods are safe without locks (acceptable stale reads)
/// - Follows the same pattern as DiplomacyManager and CivilizationManager
/// </summary>
public class HolySiteManager : IHolySiteManager
{
    // Persistence key
    private const string DATA_KEY = "divineascension_holy_sites";
    private const int MAX_SITES_PER_TIER = 5;
    private readonly IEventService _eventService;

    // Dependencies (API wrappers)
    private readonly ILogger _logger;
    private readonly IPersistenceService _persistenceService;
    private readonly IReligionManager _religionManager;
    private readonly ConcurrentDictionary<string, HashSet<string>> _sitesByReligion = new();

    // Two indexes for O(1) lookups
    private readonly ConcurrentDictionary<string, HolySiteData> _sitesByUID = new();
    private readonly IWorldService _worldService;

    /// <summary>
    /// Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    private object? _lock;

    /// <summary>
    /// Constructs a new HolySiteManager with required dependencies.
    /// </summary>
    public HolySiteManager(
        ILogger logger,
        IEventService eventService,
        IPersistenceService persistenceService,
        IWorldService worldService,
        IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    }

    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }

            return _lock;
        }
    }

    /// <summary>
    /// Initializes the manager and registers event handlers.
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Holy Site Manager...");

        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _eventService.OnGameWorldSave(OnGameWorldSave);

        LoadHolySites();

        _logger.Notification("[DivineAscension] Holy Site Manager initialized");
    }

    /// <summary>
    /// Disposes resources and unregisters event handlers.
    /// </summary>
    public void Dispose()
    {
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
    }

    #region Prestige-Based Limits

    /// <summary>
    /// Gets the maximum number of holy sites a religion can have based on prestige tier.
    /// Tier is calculated as PrestigeRank + 1 (1-5 range), capped at 5 sites maximum.
    /// </summary>
    public int GetMaxSitesForReligion(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
            return 1; // Default to tier 1 if religion not found

        // Convert PrestigeRank (0-4) to tier (1-5)
        int tier = (int)religion.PrestigeRank + 1;
        return Math.Min(tier, MAX_SITES_PER_TIER);
    }

    /// <summary>
    /// Checks if a religion can create another holy site based on prestige limits.
    /// </summary>
    public bool CanCreateHolySite(string religionUID)
    {
        var currentSiteCount = _sitesByReligion.GetValueOrDefault(religionUID)?.Count ?? 0;
        var maxSites = GetMaxSitesForReligion(religionUID);
        return currentSiteCount < maxSites;
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Consecrates a land claim as a holy site.
    /// Returns null if validation fails (empty name, empty areas, prestige limit reached, or overlapping site).
    /// </summary>
    public HolySiteData? ConsecrateHolySite(string religionUID, string siteName,
        List<Cuboidi> claimAreas, string founderUID)
    {
        lock (Lock)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    _logger.Warning("[DivineAscension] Holy site name cannot be empty");
                    return null;
                }

                if (claimAreas == null || claimAreas.Count == 0)
                {
                    _logger.Warning("[DivineAscension] Claim areas cannot be empty");
                    return null;
                }

                // Check prestige limit
                if (!CanCreateHolySite(religionUID))
                {
                    _logger.Warning($"[DivineAscension] Religion {religionUID} has reached maximum holy site limit");
                    return null;
                }

                // Convert Cuboidi to SerializableCuboidi
                var serializableAreas = claimAreas
                    .Select(area => new SerializableCuboidi(area))
                    .ToList();

                // Create temporary site for overlap checking
                var tempSite = new HolySiteData(
                    Guid.NewGuid().ToString(),
                    religionUID,
                    siteName,
                    serializableAreas,
                    founderUID,
                    "");

                // Check for overlap with existing holy sites
                foreach (var existingSite in _sitesByUID.Values)
                {
                    if (tempSite.Intersects(existingSite))
                    {
                        _logger.Warning($"[DivineAscension] Holy site overlaps with existing site {existingSite.SiteName}");
                        return null;
                    }
                }

                // Get founder info
                var founder = _worldService.GetPlayerByUID(founderUID);
                var founderName = founder?.PlayerName ?? founderUID;

                // Create final site
                var siteUID = Guid.NewGuid().ToString();
                var site = new HolySiteData(
                    siteUID,
                    religionUID,
                    siteName,
                    serializableAreas,
                    founderUID,
                    founderName);

                // Update indexes
                _sitesByUID[siteUID] = site;

                if (!_sitesByReligion.ContainsKey(religionUID))
                    _sitesByReligion[religionUID] = new HashSet<string>();
                _sitesByReligion[religionUID].Add(siteUID);

                _logger.Notification($"[DivineAscension] Holy site '{siteName}' consecrated with {site.Areas.Count} area(s), tier {site.GetTier()}");

                SaveHolySites();

                return site;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error creating holy site: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Removes a holy site and all its areas.
    /// Returns false if site not found.
    /// </summary>
    public bool DeconsacrateHolySite(string siteUID)
    {
        lock (Lock)
        {
            try
            {
                if (!_sitesByUID.TryRemove(siteUID, out var site))
                {
                    _logger.Warning($"[DivineAscension] Holy site {siteUID} not found");
                    return false;
                }

                // Remove from religion index
                if (_sitesByReligion.TryGetValue(site.ReligionUID, out var sites))
                {
                    sites.Remove(siteUID);
                    if (sites.Count == 0)
                        _sitesByReligion.TryRemove(site.ReligionUID, out _);
                }

                _logger.Notification($"[DivineAscension] Holy site '{site.SiteName}' deconsecrated");

                SaveHolySites();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error deconsecrating holy site: {ex.Message}");
                return false;
            }
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets a holy site by its UID.
    /// </summary>
    public HolySiteData? GetHolySite(string siteUID)
    {
        lock (Lock)
        {
            return _sitesByUID.GetValueOrDefault(siteUID);
        }
    }

    /// <summary>
    /// Gets the holy site at a specific block position.
    /// </summary>
    public HolySiteData? GetHolySiteAtPosition(BlockPos pos)
    {
        foreach (var site in _sitesByUID.Values)
        {
            if (site.ContainsPosition(pos))
                return site;
        }
        return null;
    }

    /// <summary>
    /// Checks if a player is currently in a holy site.
    /// </summary>
    public bool IsPlayerInHolySite(string playerUID, out HolySiteData? site)
    {
        var player = _worldService.GetPlayerByUID(playerUID);
        if (player == null)
        {
            site = null;
            return false;
        }

        var pos = player.Entity.Pos.AsBlockPos;
        site = GetHolySiteAtPosition(pos);
        return site != null;
    }

    /// <summary>
    /// Gets all holy sites owned by a religion.
    /// </summary>
    public List<HolySiteData> GetReligionHolySites(string religionUID)
    {
        if (!_sitesByReligion.TryGetValue(religionUID, out var siteUIDs))
            return new List<HolySiteData>();

        lock (Lock)
        {
            return siteUIDs
                .Select(uid => _sitesByUID.GetValueOrDefault(uid))
                .Where(site => site != null)
                .ToList()!;
        }
    }

    /// <summary>
    /// Gets all holy sites in the world.
    /// </summary>
    public List<HolySiteData> GetAllHolySites()
    {
        lock (Lock)
        {
            return _sitesByUID.Values.ToList();
        }
    }

    #endregion

    #region Cascading Deletion

    /// <summary>
    /// Handles religion deletion by removing all associated holy sites.
    /// Called when a religion is deleted to maintain data consistency.
    /// </summary>
    public void HandleReligionDeleted(string religionUID)
    {
        lock (Lock)
        {
            try
            {
                // Get sites inside lock to ensure consistent view
                var sites = GetReligionHolySites_Unlocked(religionUID);
                foreach (var site in sites)
                {
                    DeconsacrateHolySite_Unlocked(site.SiteUID);
                }

                _logger.Debug($"[DivineAscension] Removed {sites.Count} holy sites for deleted religion {religionUID}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error handling religion deletion in HolySiteManager: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Internal unlocked version for use within locked contexts.
    /// </summary>
    private List<HolySiteData> GetReligionHolySites_Unlocked(string religionUID)
    {
        if (!_sitesByReligion.TryGetValue(religionUID, out var siteUIDs))
            return new List<HolySiteData>();

        return siteUIDs
            .Select(uid => _sitesByUID.GetValueOrDefault(uid))
            .Where(site => site != null)
            .ToList()!;
    }

    /// <summary>
    /// Internal unlocked version for use within locked contexts.
    /// </summary>
    private bool DeconsacrateHolySite_Unlocked(string siteUID)
    {
        try
        {
            if (!_sitesByUID.TryRemove(siteUID, out var site))
            {
                return false;
            }

            // Remove from religion index
            if (_sitesByReligion.TryGetValue(site.ReligionUID, out var sites))
            {
                sites.Remove(siteUID);
                if (sites.Count == 0)
                    _sitesByReligion.TryRemove(site.ReligionUID, out _);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Persistence

    private void LoadHolySites()
    {
        lock (Lock)
        {
            try
            {
                var data = _persistenceService.Load<HolySiteWorldData>(DATA_KEY);
                if (data?.HolySites != null)
                {
                    _sitesByUID.Clear();
                    _sitesByReligion.Clear();

                    foreach (var site in data.HolySites)
                    {
                        // Skip sites with no areas (corrupted data)
                        if (site.Areas == null || site.Areas.Count == 0)
                        {
                            _logger.Warning($"[DivineAscension] Skipping holy site {site.SiteUID} with no areas");
                            continue;
                        }

                        _sitesByUID[site.SiteUID] = site;

                        if (!_sitesByReligion.ContainsKey(site.ReligionUID))
                            _sitesByReligion[site.ReligionUID] = new HashSet<string>();
                        _sitesByReligion[site.ReligionUID].Add(site.SiteUID);
                    }

                    _logger.Notification($"[DivineAscension] Loaded {_sitesByUID.Count} holy sites");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Failed to load holy sites: {ex.Message}");
            }
        }
    }

    private void SaveHolySites()
    {
        lock (Lock)
        {
            try
            {
                var data = new HolySiteWorldData
                {
                    HolySites = _sitesByUID.Values.ToList()
                };
                _persistenceService.Save(DATA_KEY, data);
                _logger.Debug($"[DivineAscension] Saved {data.HolySites.Count} holy sites");
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Failed to save holy sites: {ex.Message}");
            }
        }
    }

    private void OnSaveGameLoaded()
    {
        LoadHolySites();
    }

    private void OnGameWorldSave()
    {
        SaveHolySites();
    }

    #endregion
}