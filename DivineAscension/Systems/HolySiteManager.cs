using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Services;
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

    /// <inheritdoc />
    public event Action<string, string>? OnHolySiteCreated;

    // Dependencies (API wrappers)
    private readonly ILoggerWrapper _logger;
    private readonly IPersistenceService _persistenceService;
    private readonly IReligionManager _religionManager;
    private readonly ConcurrentDictionary<string, HashSet<string>> _sitesByReligion = new();

    // Two indexes for O(1) lookups
    private readonly ConcurrentDictionary<string, HolySiteData> _sitesByUID = new();

    // Chunk-based spatial index for O(1) position lookups
    // Maps chunk coordinates to the set of holy site UIDs that overlap that chunk
    // Uses ConcurrentDictionary for thread-safe reads during position checks
    private readonly ConcurrentDictionary<(int chunkX, int chunkZ), HashSet<string>> _sitesByChunk = new();
    private readonly IWorldService _worldService;

    // Optional dependency for civilization bonus slots (set via SetCivilizationBonusSystem)
    private ICivilizationBonusSystem? _civilizationBonusSystem;

    /// <summary>
    /// Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    private object? _lock;

    /// <summary>
    /// Constructs a new HolySiteManager with required dependencies.
    /// </summary>
    public HolySiteManager(
        ILoggerWrapper logger,
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
        _religionManager.OnReligionDeleted -= HandleReligionDeleted;
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
    }

    /// <summary>
    /// Sets the civilization bonus system for bonus holy site slots.
    /// Must be called after CivilizationBonusSystem is initialized.
    /// </summary>
    public void SetCivilizationBonusSystem(ICivilizationBonusSystem civilizationBonusSystem)
    {
        _civilizationBonusSystem = civilizationBonusSystem ?? throw new ArgumentNullException(nameof(civilizationBonusSystem));
    }

    #region Prestige-Based Limits

    /// <summary>
    /// Gets the maximum number of holy sites a religion can have based on prestige tier
    /// plus any bonus slots from civilization milestones.
    /// Tier is calculated as PrestigeRank + 1 (1-5 range), with bonus slots added on top.
    /// </summary>
    public int GetMaxSitesForReligion(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
            return 1; // Default to tier 1 if religion not found

        // Convert PrestigeRank (0-4) to tier (1-5)
        int tier = (int)religion.PrestigeRank + 1;
        int baseSites = Math.Min(tier, MAX_SITES_PER_TIER);

        // Add bonus slots from civilization milestones
        int bonusSlots = 0;
        if (_civilizationBonusSystem != null)
        {
            bonusSlots = _civilizationBonusSystem.GetBonusHolySiteSlotsForReligion(religionUID);
        }

        return baseSites + bonusSlots;
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
        // Get founder info
        var founder = _worldService.GetPlayerByUID(founderUID);
        var founderName = founder?.PlayerName ?? founderUID;

        return ConsecrateHolySiteInternal(religionUID, siteName, claimAreas, founderUID, founderName, null);
    }

    /// <summary>
    /// Consecrates a land claim as a holy site with an altar at the specified position.
    /// Returns null if validation fails (same as ConsecrateHolySite).
    /// </summary>
    public HolySiteData? ConsecrateHolySiteWithAltar(
        string religionUID,
        string siteName,
        List<Cuboidi> claimAreas,
        string founderUID,
        string founderName,
        BlockPos altarPosition)
    {
        return ConsecrateHolySiteInternal(religionUID, siteName, claimAreas, founderUID, founderName, altarPosition);
    }

    /// <summary>
    /// Internal implementation for holy site consecration.
    /// Separated for testability - contains all validation and creation logic.
    /// </summary>
    internal HolySiteData? ConsecrateHolySiteInternal(
        string religionUID,
        string siteName,
        List<Cuboidi>? claimAreas,
        string founderUID,
        string founderName,
        BlockPos? altarPosition)
    {
        lock (Lock)
        {
            try
            {
                // Validate inputs
                var validationError = ValidateHolySiteCreationInputs(siteName, claimAreas);
                if (validationError != null)
                {
                    _logger.Warning($"[DivineAscension] {validationError}");
                    return null;
                }

                // Check prestige limit
                if (!CanCreateHolySite(religionUID))
                {
                    _logger.Warning($"[DivineAscension] Religion {religionUID} has reached maximum holy site limit");
                    return null;
                }

                // Convert Cuboidi to SerializableCuboidi
                var serializableAreas = claimAreas!
                    .Select(area => new SerializableCuboidi(area))
                    .ToList();

                // Check for overlap with existing holy sites
                var overlappingSiteName = FindOverlappingHolySite(serializableAreas);
                if (overlappingSiteName != null)
                {
                    _logger.Warning($"[DivineAscension] Holy site overlaps with existing site {overlappingSiteName}");
                    return null;
                }

                // Create final site
                var siteUID = Guid.NewGuid().ToString();
                var site = new HolySiteData(
                    siteUID,
                    religionUID,
                    siteName,
                    serializableAreas,
                    founderUID,
                    founderName);

                // Set altar position if provided
                if (altarPosition != null)
                {
                    site.AltarPosition = SerializableBlockPos.FromBlockPos(altarPosition);
                }

                // Register the site in indexes
                RegisterNewHolySite(site, religionUID);

                // Log creation
                var altarInfo = altarPosition != null ? $" with altar at {altarPosition}" : $" with {site.Areas.Count} area(s)";
                _logger.Notification($"[DivineAscension] Holy site '{siteName}' consecrated{altarInfo}, tier {site.GetTier()}");

                SaveHolySites();

                // Fire event for milestone tracking
                OnHolySiteCreated?.Invoke(religionUID, site.SiteUID);

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
    /// Validates inputs for holy site creation.
    /// Returns an error message if validation fails, null if inputs are valid.
    /// </summary>
    internal static string? ValidateHolySiteCreationInputs(string? siteName, List<Cuboidi>? claimAreas)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            return "Holy site name cannot be empty";
        }

        if (claimAreas == null || claimAreas.Count == 0)
        {
            return "Claim areas cannot be empty";
        }

        return null;
    }

    /// <summary>
    /// Finds the first existing holy site that overlaps with the given areas.
    /// Returns the overlapping site's name, or null if no overlap.
    /// </summary>
    internal string? FindOverlappingHolySite(List<SerializableCuboidi> areas)
    {
        // Create temporary site for intersection checking
        var tempSite = new HolySiteData(
            "temp",
            "temp",
            "temp",
            areas,
            "temp",
            "temp");

        foreach (var existingSite in _sitesByUID.Values)
        {
            if (tempSite.Intersects(existingSite))
            {
                return existingSite.SiteName;
            }
        }

        return null;
    }

    /// <summary>
    /// Registers a new holy site in all indexes.
    /// Does not save to persistence - caller is responsible for calling SaveHolySites().
    /// </summary>
    internal void RegisterNewHolySite(HolySiteData site, string religionUID)
    {
        _sitesByUID[site.SiteUID] = site;
        IndexSiteChunks(site);

        if (!_sitesByReligion.ContainsKey(religionUID))
            _sitesByReligion[religionUID] = new HashSet<string>();
        _sitesByReligion[religionUID].Add(site.SiteUID);
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
                if (!_sitesByUID.TryGetValue(siteUID, out var site))
                {
                    _logger.Warning($"[DivineAscension] Holy site {siteUID} not found");
                    return false;
                }

                // Remove from chunk index before removing from main index
                RemoveSiteFromChunkIndex(site);
                _sitesByUID.TryRemove(siteUID, out _);

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

    /// <summary>
    /// Renames a holy site.
    /// Returns false if site not found or name is invalid.
    /// </summary>
    public bool RenameHolySite(string siteUID, string newName)
    {
        lock (Lock)
        {
            try
            {
                if (!_sitesByUID.TryGetValue(siteUID, out var site))
                {
                    _logger.Warning($"[DivineAscension] Holy site {siteUID} not found for rename");
                    return false;
                }

                var validationError = ValidateHolySiteName(newName);
                if (validationError != null)
                {
                    _logger.Warning($"[DivineAscension] {validationError}");
                    return false;
                }

                var oldName = site.SiteName;
                ApplyHolySiteRename(site, newName);

                _logger.Notification($"[DivineAscension] Holy site renamed from '{oldName}' to '{newName}'");

                SaveHolySites();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error renaming holy site: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Updates the description of a holy site.
    /// Returns false if site not found or description is invalid.
    /// </summary>
    public bool UpdateDescription(string siteUID, string description)
    {
        lock (Lock)
        {
            try
            {
                if (!_sitesByUID.TryGetValue(siteUID, out var site))
                {
                    _logger.Warning($"[DivineAscension] Holy site {siteUID} not found for description update");
                    return false;
                }

                var validationError = ValidateHolySiteDescription(description);
                if (validationError != null)
                {
                    _logger.Warning($"[DivineAscension] {validationError}");
                    return false;
                }

                ApplyHolySiteDescriptionUpdate(site, description);

                _logger.Notification($"[DivineAscension] Holy site '{site.SiteName}' description updated");

                SaveHolySites();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error updating holy site description: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Validates a holy site name.
    /// Returns an error message if invalid, null if valid.
    /// </summary>
    internal static string? ValidateHolySiteName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Holy site name cannot be empty";
        }

        if (name.Length > 50)
        {
            return "Holy site name cannot exceed 50 characters";
        }

        return null;
    }

    /// <summary>
    /// Validates a holy site description.
    /// Returns an error message if invalid, null if valid.
    /// </summary>
    internal static string? ValidateHolySiteDescription(string? description)
    {
        // Null/empty description is allowed (clears the description)
        if (description != null && description.Length > 200)
        {
            return "Holy site description cannot exceed 200 characters";
        }

        return null;
    }

    /// <summary>
    /// Applies a rename to a holy site.
    /// Does not save to persistence - caller is responsible for calling SaveHolySites().
    /// </summary>
    internal static void ApplyHolySiteRename(HolySiteData site, string newName)
    {
        site.SiteName = newName;
    }

    /// <summary>
    /// Applies a description update to a holy site.
    /// Does not save to persistence - caller is responsible for calling SaveHolySites().
    /// </summary>
    internal static void ApplyHolySiteDescriptionUpdate(HolySiteData site, string description)
    {
        site.Description = description ?? string.Empty;
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
    /// Uses chunk-based spatial index for O(1) lookup when player is far from all sites.
    /// </summary>
    public HolySiteData? GetHolySiteAtPosition(BlockPos pos)
    {
        var chunkKey = GetChunkKey(pos);

        // O(1) lookup: check if any sites overlap this chunk
        if (!_sitesByChunk.TryGetValue(chunkKey, out var candidateSites))
        {
            return null; // No sites in this chunk - early exit
        }

        // O(k) where k is typically 0-2 sites: verify exact containment
        foreach (var siteUID in candidateSites)
        {
            if (_sitesByUID.TryGetValue(siteUID, out var site) && site.ContainsPosition(pos))
            {
                return site;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the holy site that has an altar at the specified position.
    /// Returns null if no altar-based holy site exists at that position.
    /// </summary>
    public HolySiteData? GetHolySiteByAltarPosition(BlockPos altarPos)
    {
        return _sitesByUID.Values
            .FirstOrDefault(site => site.IsAtAltarPosition(altarPos));
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

    #region Chunk Index Methods

    /// <summary>
    /// Gets the chunk key for a block position.
    /// Vintage Story chunks are 32x32, so we use bit shift for speed.
    /// </summary>
    private static (int chunkX, int chunkZ) GetChunkKey(BlockPos pos) => (pos.X >> 5, pos.Z >> 5);

    /// <summary>
    /// Gets the chunk key for raw coordinates.
    /// </summary>
    private static (int chunkX, int chunkZ) GetChunkKey(int x, int z) => (x >> 5, z >> 5);

    /// <summary>
    /// Indexes a holy site's areas into the chunk map.
    /// Must be called within a lock context.
    /// </summary>
    private void IndexSiteChunks(HolySiteData site)
    {
        foreach (var area in site.Areas)
        {
            // Use Min/Max because X1/X2 and Z1/Z2 aren't guaranteed to be ordered
            int minCX = Math.Min(area.X1, area.X2) >> 5;
            int maxCX = Math.Max(area.X1, area.X2) >> 5;
            int minCZ = Math.Min(area.Z1, area.Z2) >> 5;
            int maxCZ = Math.Max(area.Z1, area.Z2) >> 5;

            for (int cx = minCX; cx <= maxCX; cx++)
            {
                for (int cz = minCZ; cz <= maxCZ; cz++)
                {
                    var key = (cx, cz);
                    if (!_sitesByChunk.TryGetValue(key, out var sites))
                    {
                        _sitesByChunk[key] = sites = new HashSet<string>();
                    }

                    sites.Add(site.SiteUID);
                }
            }
        }
    }

    /// <summary>
    /// Removes a holy site from the chunk index.
    /// Must be called within a lock context.
    /// </summary>
    private void RemoveSiteFromChunkIndex(HolySiteData site)
    {
        foreach (var area in site.Areas)
        {
            // Use Min/Max because X1/X2 and Z1/Z2 aren't guaranteed to be ordered
            int minCX = Math.Min(area.X1, area.X2) >> 5;
            int maxCX = Math.Max(area.X1, area.X2) >> 5;
            int minCZ = Math.Min(area.Z1, area.Z2) >> 5;
            int maxCZ = Math.Max(area.Z1, area.Z2) >> 5;

            for (int cx = minCX; cx <= maxCX; cx++)
            {
                for (int cz = minCZ; cz <= maxCZ; cz++)
                {
                    var key = (cx, cz);
                    if (_sitesByChunk.TryGetValue(key, out var sites))
                    {
                        sites.Remove(site.SiteUID);
                        if (sites.Count == 0)
                        {
                            _sitesByChunk.TryRemove(key, out _);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Rebuilds the entire chunk index from scratch.
    /// Must be called within a lock context.
    /// </summary>
    private void RebuildChunkIndex()
    {
        _sitesByChunk.Clear();
        foreach (var site in _sitesByUID.Values)
        {
            IndexSiteChunks(site);
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
            if (!_sitesByUID.TryGetValue(siteUID, out var site))
            {
                return false;
            }

            // Remove from chunk index before removing from main index
            RemoveSiteFromChunkIndex(site);
            _sitesByUID.TryRemove(siteUID, out _);

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
                    _sitesByChunk.Clear();

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

                    // Rebuild chunk index after loading all sites
                    RebuildChunkIndex();

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