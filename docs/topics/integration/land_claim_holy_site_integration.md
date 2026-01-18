# Land Claim Holy Site Integration

## Overview

Vintage Story has a robust built-in land claim system that we can extend to create temple/holy site mechanics. This document explains how to integrate with the existing system.

## Vintage Story Land Claim API

### Accessing Land Claims

```csharp
// Access the land claim system
var claimAPI = _sapi.World.Claims;

// Get claim at a position
BlockPos pos = player.Entity.ServerPos.AsBlockPos;
LandClaim claim = claimAPI.Get(pos.ToChunkPos());

// Check if player owns a claim
if (claim != null && claim.OwnedByPlayerUid.Contains(player.PlayerUID))
{
    // Player owns this claim
}

// Get all claims owned by a player
List<LandClaim> playerClaims = claimAPI.All
    .Where(c => c.OwnedByPlayerUid.Contains(player.PlayerUID))
    .ToList();
```

### Key Land Claim Properties

```csharp
public class LandClaim
{
    // Chunk coordinates (X, Z)
    public Vec2i ChunkPos { get; set; }

    // List of player UIDs who own/have access
    public List<string> OwnedByPlayerUid { get; set; }

    // Claim description
    public string Description { get; set; }

    // When claim was created
    public long LastKnownOwnerChangeMs { get; set; }

    // Claim areas (can have multiple rectangles)
    public List<Cuboidi> Areas { get; set; }
}
```

## Holy Site System Architecture

### Approach: Metadata Overlay System

Since we can't modify Vintage Story's core land claim data, we'll create an **overlay system** that maps land claims to holy site data.

### Tier System and Scale

Holy sites are tied to the land claim chunk system. Each chunk is **32×32 blocks** (1,024 blocks²), which is the minimum granularity. Individual holy sites are capped at **6 chunks** to encourage religions to establish multiple sites across the world.

| Tier | Name | Chunks | Block Area | Description |
|------|------|--------|------------|-------------|
| 1 | **Sacred Ground** | 1 | 32×32 | Consecrated land surrounding a religious structure |
| 2 | **Shrine** | 2-3 | Up to 96×96 | A modest worship site with surrounding grounds |
| 3 | **Temple** | 4-6 | Up to 192×192 | A significant religious complex (max size per site) |

**Note:** "Sacred Ground" emphasizes that Tier 1 represents consecrated territory, not a small physical structure. Players may build a small altar within their sacred ground, but the holy site itself encompasses the entire claimed area.

### Prestige-Based Site Slots

Religions unlock additional holy site slots as they gain prestige. This encourages spreading influence across the world rather than concentrating in one location.

| Prestige Rank | Prestige Required | Holy Site Slots | Max Total Chunks |
|---------------|-------------------|-----------------|------------------|
| Fledgling | 0 | 1 | 6 |
| Established | 500 | 2 | 12 |
| Renowned | 2,000 | 3 | 18 |
| Legendary | 5,000 | 4 | 24 |
| Mythic | 10,000 | 5 | 30 |

**Design rationale:**
- Encourages territorial spread rather than one mega-site
- Creates strategic decisions: upgrade existing sites or claim new locations
- Multiple sites create more points of interest for PvP/diplomacy
- Aligns holy site progression with existing prestige system

### Data Structure

**File:** `DivineAscension/Data/HolySiteData.cs`

```csharp
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Serializable chunk position for ProtoBuf compatibility.
/// Vec2i from Vintage Story may not serialize correctly.
/// </summary>
[ProtoContract]
public record SerializableChunkPos
{
    [ProtoMember(1)]
    public int X { get; set; }

    [ProtoMember(2)]
    public int Z { get; set; }

    public SerializableChunkPos() { }

    public SerializableChunkPos(int x, int z)
    {
        X = x;
        Z = z;
    }

    public override int GetHashCode() => HashCode.Combine(X, Z);
}

[ProtoContract]
public class HolySiteData
{
    /// <summary>
    /// Unique identifier for this holy site
    /// </summary>
    [ProtoMember(1)]
    public string HolySiteUID { get; set; } = string.Empty;

    /// <summary>
    /// Chunk coordinates of the claimed land (primary chunk)
    /// </summary>
    [ProtoMember(2)]
    public SerializableChunkPos ChunkPos { get; set; } = new();

    /// <summary>
    /// Religion that owns this holy site
    /// </summary>
    [ProtoMember(3)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the holy site
    /// </summary>
    [ProtoMember(4)]
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// Player who designated this site
    /// </summary>
    [ProtoMember(5)]
    public string DesignatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the site was consecrated (stored as UTC ticks for reliable serialization)
    /// </summary>
    [ProtoMember(6)]
    public long ConsecrationDateTicks { get; set; }

    /// <summary>
    /// Tier of the holy site (1-3)
    /// </summary>
    [ProtoMember(7)]
    public int Tier { get; set; } = 1;

    /// <summary>
    /// Current level of consecration (for upgrades)
    /// </summary>
    [ProtoMember(8)]
    public int ConsecrationLevel { get; set; } = 0;

    /// <summary>
    /// Whether the site is currently active
    /// </summary>
    [ProtoMember(9)]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Connected land claim chunks (for multi-chunk temples)
    /// </summary>
    [ProtoMember(10)]
    public List<SerializableChunkPos> ConnectedChunks { get; set; } = new();

    /// <summary>
    /// Gets the consecration date as DateTime (UTC)
    /// </summary>
    public DateTime ConsecrationDate => new DateTime(ConsecrationDateTicks, DateTimeKind.Utc);

    public HolySiteData() { }

    public HolySiteData(string holySiteUID, SerializableChunkPos chunkPos, string religionUID, string siteName, string designatedBy)
    {
        HolySiteUID = holySiteUID;
        ChunkPos = chunkPos;
        ReligionUID = religionUID;
        SiteName = siteName;
        DesignatedBy = designatedBy;
        ConsecrationDateTicks = DateTime.UtcNow.Ticks;
        ConnectedChunks = new List<SerializableChunkPos> { chunkPos };
    }

    /// <summary>
    /// Maximum chunks allowed per individual holy site
    /// </summary>
    public const int MaxChunksPerSite = 6;

    /// <summary>
    /// Gets the total number of chunks in this holy site
    /// </summary>
    public int GetTotalChunks()
    {
        return ConnectedChunks.Count;
    }

    /// <summary>
    /// Calculates the tier based on chunk count (capped at 6 chunks per site)
    /// </summary>
    public void UpdateTier()
    {
        int chunkCount = GetTotalChunks();
        Tier = chunkCount switch
        {
            1 => 1,              // Sacred Ground (1 chunk, 32×32 blocks)
            >= 2 and <= 3 => 2,  // Shrine (2-3 chunks, up to 96×96 blocks)
            >= 4 => 3            // Temple (4-6 chunks, up to 192×192 blocks)
        };
    }

    /// <summary>
    /// Checks if this site can accept more chunks
    /// </summary>
    public bool CanExpand() => GetTotalChunks() < MaxChunksPerSite;

    /// <summary>
    /// Gets the sacred territory multiplier for this holy site
    /// </summary>
    public float GetSacredTerritoryMultiplier()
    {
        return Tier switch
        {
            1 => 1.5f,  // Sacred Ground
            2 => 2.0f,  // Shrine
            3 => 2.5f,  // Temple
            _ => 1.0f
        };
    }

    /// <summary>
    /// Gets the prayer bonus multiplier for this holy site
    /// </summary>
    public float GetPrayerMultiplier()
    {
        return Tier switch
        {
            1 => 2.0f,  // Sacred Ground
            2 => 2.5f,  // Shrine
            3 => 3.0f,  // Temple
            _ => 1.5f
        };
    }
}
```

### Holy Site Manager

**File:** `DivineAscension/Systems/HolySiteManager.cs`

**Initialization Order:** This manager should be initialized after `PlayerProgressionDataManager` (step 5) in `DivineAscensionSystemInitializer.cs`. Suggested position: step 6.5 (after `ReligionPrestigeManager`, before `FavorSystem`), or as a new step 14.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension.Systems;

public class HolySiteManager
{
    private const string DATA_KEY = "divineascension_holysites";
    private readonly ICoreServerAPI _sapi;
    private readonly ReligionManager _religionManager;
    private readonly ReligionPrestigeManager _prestigeManager;
    private readonly PlayerProgressionDataManager _playerProgressionDataManager;
    private readonly ProfanityFilterService _profanityFilterService;
    private readonly LocalizationService _localizationService;

    // Map chunk coordinates to holy site data
    private readonly Dictionary<SerializableChunkPos, HolySiteData> _holySitesByChunk = new();

    // Map holy site UID to data
    private readonly Dictionary<string, HolySiteData> _holySitesById = new();

    // Index for quick lookup of sites by religion
    private readonly Dictionary<string, HashSet<string>> _sitesByReligion = new();

    public HolySiteManager(
        ICoreServerAPI sapi,
        ReligionManager religionManager,
        ReligionPrestigeManager prestigeManager,
        PlayerProgressionDataManager playerProgressionDataManager,
        ProfanityFilterService profanityFilterService,
        LocalizationService localizationService)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
        _playerProgressionDataManager = playerProgressionDataManager;
        _profanityFilterService = profanityFilterService;
        _localizationService = localizationService;
    }

    #region Prestige-Based Site Limits

    /// <summary>
    /// Gets the maximum number of holy sites allowed for a religion based on prestige rank
    /// </summary>
    public int GetMaxSitesForReligion(string religionUID)
    {
        var rank = _prestigeManager.GetPrestigeRank(religionUID);
        return rank switch
        {
            PrestigeRank.Fledgling => 1,
            PrestigeRank.Established => 2,
            PrestigeRank.Renowned => 3,
            PrestigeRank.Legendary => 4,
            PrestigeRank.Mythic => 5,
            _ => 1
        };
    }

    /// <summary>
    /// Gets the current number of holy sites for a religion
    /// </summary>
    public int GetSiteCountForReligion(string religionUID)
    {
        return _sitesByReligion.TryGetValue(religionUID, out var sites) ? sites.Count : 0;
    }

    /// <summary>
    /// Checks if a religion can create a new holy site
    /// </summary>
    public bool CanCreateNewSite(string religionUID)
    {
        return GetSiteCountForReligion(religionUID) < GetMaxSitesForReligion(religionUID);
    }

    #endregion

    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Holy Site Manager...");

        // Register persistence handlers
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        // Subscribe to religion deletion for cascade cleanup
        _religionManager.OnReligionDeleted += HandleReligionDeleted;

        _sapi.Logger.Notification("[DivineAscension] Holy Site Manager initialized");
    }

    public void Dispose()
    {
        _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
        _sapi.Event.GameWorldSave -= OnGameWorldSave;
        _religionManager.OnReligionDeleted -= HandleReligionDeleted;
    }

    #region Religion Deletion Cascade

    /// <summary>
    /// Handles cleanup when a religion is deleted - removes all associated holy sites
    /// </summary>
    private void HandleReligionDeleted(string religionUID)
    {
        var sitesToRemove = _holySitesById.Values
            .Where(hs => hs.ReligionUID == religionUID)
            .ToList();

        foreach (var site in sitesToRemove)
        {
            foreach (var chunk in site.ConnectedChunks)
            {
                _holySitesByChunk.Remove(chunk);
            }
            _holySitesById.Remove(site.HolySiteUID);
        }

        // Clean up religion index
        _sitesByReligion.Remove(religionUID);

        if (sitesToRemove.Count > 0)
        {
            _sapi.Logger.Notification($"[DivineAscension] Removed {sitesToRemove.Count} holy sites for deleted religion {religionUID}");
        }
    }

    #endregion

    #region Holy Site Creation

    /// <summary>
    /// Consecrates a land claim as a holy site
    /// </summary>
    public bool ConsecrateHolySite(IServerPlayer player, string siteName, out string message)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player);
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);

        // Validation
        if (religion == null)
        {
            message = _localizationService.Get("holysite-error-no-religion",
                "You must be in a religion to consecrate holy sites.");
            return false;
        }

        if (!religion.IsFounder(player.PlayerUID))
        {
            message = _localizationService.Get("holysite-error-not-founder",
                "Only the religion founder can consecrate holy sites.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(siteName) || siteName.Length < 3)
        {
            message = _localizationService.Get("holysite-error-name-too-short",
                "Holy site name must be at least 3 characters.");
            return false;
        }

        if (siteName.Length > 50)
        {
            message = _localizationService.Get("holysite-error-name-too-long",
                "Holy site name must be 50 characters or less.");
            return false;
        }

        // Profanity filter check
        if (_profanityFilterService.IsEnabled && _profanityFilterService.ContainsProfanity(siteName))
        {
            message = _localizationService.Get("holysite-error-profanity",
                "Holy site name contains inappropriate content.");
            return false;
        }

        // Check prestige-based site limit
        if (!CanCreateNewSite(religion.ReligionUID))
        {
            var max = GetMaxSitesForReligion(religion.ReligionUID);
            var rank = _prestigeManager.GetPrestigeRank(religion.ReligionUID);
            message = _localizationService.Get("holysite-error-site-limit",
                "Your religion has reached its holy site limit ({0} sites at {1} rank). Gain more prestige to unlock additional sites.",
                max, rank);
            return false;
        }

        // Get land claim at player's position
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var claim = _sapi.World.Claims.Get(pos);

        if (claim == null)
        {
            message = _localizationService.Get("holysite-error-no-claim",
                "You must be standing in a claimed area.");
            return false;
        }

        if (!claim.OwnedByPlayerUid.Contains(player.PlayerUID))
        {
            message = _localizationService.Get("holysite-error-not-owner",
                "You must own this land claim to consecrate it.");
            return false;
        }

        // Check if already consecrated
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);
        if (_holySitesByChunk.ContainsKey(chunkCoord))
        {
            message = _localizationService.Get("holysite-error-already-consecrated",
                "This land is already consecrated.");
            return false;
        }

        // Create holy site
        var holySiteUID = Guid.NewGuid().ToString();
        var holySite = new HolySiteData(
            holySiteUID,
            chunkCoord,
            religion.ReligionUID,
            siteName,
            player.PlayerUID
        );

        _holySitesById[holySiteUID] = holySite;
        _holySitesByChunk[chunkCoord] = holySite;

        // Add to religion index
        if (!_sitesByReligion.ContainsKey(religion.ReligionUID))
        {
            _sitesByReligion[religion.ReligionUID] = new HashSet<string>();
        }
        _sitesByReligion[religion.ReligionUID].Add(holySiteUID);

        var siteCount = GetSiteCountForReligion(religion.ReligionUID);
        var maxSites = GetMaxSitesForReligion(religion.ReligionUID);

        message = _localizationService.Get("holysite-consecrated",
            "You have consecrated {0} as a holy site for {1}! ({2}/{3} sites)",
            siteName, religion.ReligionName, siteCount, maxSites);

        _sapi.Logger.Notification($"[DivineAscension] Holy site '{siteName}' created by {player.PlayerName}");

        return true;
    }

    /// <summary>
    /// Expands a holy site to include adjacent land claims
    /// </summary>
    public bool ExpandHolySite(IServerPlayer player, out string message)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);

        // Check if standing in a holy site
        if (!_holySitesByChunk.TryGetValue(chunkCoord, out var adjacentSite))
        {
            message = _localizationService.Get("holysite-error-not-in-site",
                "You must be standing in a holy site to expand it.");
            return false;
        }

        var religion = _religionManager.GetReligion(adjacentSite.ReligionUID);
        if (religion == null || !religion.IsFounder(player.PlayerUID))
        {
            message = _localizationService.Get("holysite-error-expand-not-founder",
                "Only the religion founder can expand holy sites.");
            return false;
        }

        // Check max chunks per site limit
        var mainSite = _holySitesById[adjacentSite.HolySiteUID];
        if (!mainSite.CanExpand())
        {
            message = _localizationService.Get("holysite-error-max-chunks",
                "This holy site has reached its maximum size ({0} chunks). Consider creating a new holy site in another location.",
                HolySiteData.MaxChunksPerSite);
            return false;
        }

        // Get adjacent chunks
        var adjacentChunks = GetAdjacentChunks(chunkCoord);
        var expandableChunks = new List<SerializableChunkPos>();

        foreach (var adjChunk in adjacentChunks)
        {
            // Check if chunk is claimed by player using block position
            var blockPos = new BlockPos(
                adjChunk.X * GlobalConstants.ChunkSize,
                pos.Y,
                adjChunk.Z * GlobalConstants.ChunkSize
            );
            var claim = _sapi.World.Claims.Get(blockPos);
            if (claim != null && claim.OwnedByPlayerUid.Contains(player.PlayerUID))
            {
                // Check if not already part of a holy site
                if (!_holySitesByChunk.ContainsKey(adjChunk))
                {
                    expandableChunks.Add(adjChunk);
                }
            }
        }

        if (expandableChunks.Count == 0)
        {
            message = _localizationService.Get("holysite-error-no-adjacent",
                "No adjacent unconsecrated claims found.");
            return false;
        }

        // Limit expansion to max chunks per site
        var chunksToAdd = Math.Min(
            expandableChunks.Count,
            HolySiteData.MaxChunksPerSite - mainSite.GetTotalChunks()
        );

        if (chunksToAdd == 0)
        {
            message = _localizationService.Get("holysite-error-max-chunks",
                "This holy site has reached its maximum size ({0} chunks).",
                HolySiteData.MaxChunksPerSite);
            return false;
        }

        // Add chunks to the holy site (limited by max)
        var chunksAdded = 0;
        foreach (var chunk in expandableChunks.Take(chunksToAdd))
        {
            mainSite.ConnectedChunks.Add(chunk);
            _holySitesByChunk[chunk] = mainSite;
            chunksAdded++;
        }

        mainSite.UpdateTier();

        var remaining = HolySiteData.MaxChunksPerSite - mainSite.GetTotalChunks();
        message = _localizationService.Get("holysite-expanded",
            "{0} expanded by {1} chunk(s)! Now {2}/{3} chunks (Tier {4}: {5}).",
            mainSite.SiteName, chunksAdded, mainSite.GetTotalChunks(),
            HolySiteData.MaxChunksPerSite, mainSite.Tier, GetTierName(mainSite.Tier));

        return true;
    }

    /// <summary>
    /// Gets the display name for a tier
    /// </summary>
    private string GetTierName(int tier)
    {
        return tier switch
        {
            1 => _localizationService.Get("holysite-tier-sacred-ground", "Sacred Ground"),
            2 => _localizationService.Get("holysite-tier-shrine", "Shrine"),
            3 => _localizationService.Get("holysite-tier-temple", "Temple"),
            _ => _localizationService.Get("holysite-tier-unknown", "Unknown")
        };
    }

    /// <summary>
    /// Deconsecrates a holy site (removes consecration)
    /// </summary>
    public bool DeconsecateHolySite(IServerPlayer player, out string message)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);

        if (!_holySitesByChunk.TryGetValue(chunkCoord, out var holySite))
        {
            message = _localizationService.Get("holysite-error-not-standing-in",
                "You are not standing in a holy site.");
            return false;
        }

        var religion = _religionManager.GetReligion(holySite.ReligionUID);
        if (religion == null || !religion.IsFounder(player.PlayerUID))
        {
            message = _localizationService.Get("holysite-error-deconsecrate-not-founder",
                "Only the religion founder can deconsecrate holy sites.");
            return false;
        }

        // Remove from all mappings
        foreach (var chunk in holySite.ConnectedChunks)
        {
            _holySitesByChunk.Remove(chunk);
        }
        _holySitesById.Remove(holySite.HolySiteUID);

        // Remove from religion index
        if (_sitesByReligion.TryGetValue(holySite.ReligionUID, out var religionSites))
        {
            religionSites.Remove(holySite.HolySiteUID);
        }

        message = _localizationService.Get("holysite-deconsecrated",
            "{0} has been deconsecrated.", holySite.SiteName);

        _sapi.Logger.Notification($"[DivineAscension] Holy site '{holySite.SiteName}' deconsecrated by {player.PlayerName}");

        return true;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Checks if a player is currently in a holy site
    /// </summary>
    public bool IsPlayerInHolySite(IServerPlayer player, out HolySiteData? holySite)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);

        return _holySitesByChunk.TryGetValue(chunkCoord, out holySite);
    }

    /// <summary>
    /// Checks if a player is in their own religion's holy site
    /// </summary>
    public bool IsPlayerInOwnHolySite(IServerPlayer player, out HolySiteData? holySite)
    {
        if (IsPlayerInHolySite(player, out holySite) && holySite != null)
        {
            var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            return religion != null && religion.ReligionUID == holySite.ReligionUID;
        }

        holySite = null;
        return false;
    }

    /// <summary>
    /// Gets all holy sites for a religion
    /// </summary>
    public List<HolySiteData> GetReligionHolySites(string religionUID)
    {
        return _holySitesById.Values
            .Where(hs => hs.ReligionUID == religionUID)
            .ToList();
    }

    /// <summary>
    /// Gets the holy site at a specific position
    /// </summary>
    public HolySiteData? GetHolySiteAtPosition(BlockPos pos)
    {
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);
        _holySitesByChunk.TryGetValue(chunkCoord, out var holySite);
        return holySite;
    }

    /// <summary>
    /// Gets all holy sites
    /// </summary>
    public List<HolySiteData> GetAllHolySites()
    {
        return _holySitesById.Values.ToList();
    }

    #endregion

    #region Helper Methods

    private List<SerializableChunkPos> GetAdjacentChunks(SerializableChunkPos center)
    {
        return new List<SerializableChunkPos>
        {
            new SerializableChunkPos(center.X + 1, center.Z),     // East
            new SerializableChunkPos(center.X - 1, center.Z),     // West
            new SerializableChunkPos(center.X, center.Z + 1),     // South
            new SerializableChunkPos(center.X, center.Z - 1),     // North
            new SerializableChunkPos(center.X + 1, center.Z + 1), // Southeast
            new SerializableChunkPos(center.X + 1, center.Z - 1), // Northeast
            new SerializableChunkPos(center.X - 1, center.Z + 1), // Southwest
            new SerializableChunkPos(center.X - 1, center.Z - 1)  // Northwest
        };
    }

    #endregion

    #region Persistence

    private void OnSaveGameLoaded()
    {
        LoadAllHolySites();
    }

    private void OnGameWorldSave()
    {
        SaveAllHolySites();
    }

    private void LoadAllHolySites()
    {
        try
        {
            var data = _sapi.WorldManager.SaveGame.GetData(DATA_KEY);
            if (data != null)
            {
                var holySitesList = SerializerUtil.Deserialize<List<HolySiteData>>(data);
                if (holySitesList != null)
                {
                    _holySitesById.Clear();
                    _holySitesByChunk.Clear();

                    foreach (var holySite in holySitesList)
                    {
                        _holySitesById[holySite.HolySiteUID] = holySite;

                        // Map all chunks
                        foreach (var chunk in holySite.ConnectedChunks)
                        {
                            _holySitesByChunk[chunk] = holySite;
                        }
                    }

                    _sapi.Logger.Notification($"[DivineAscension] Loaded {_holySitesById.Count} holy sites");
                }
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to load holy sites: {ex.Message}");
        }
    }

    private void SaveAllHolySites()
    {
        try
        {
            var holySitesList = _holySitesById.Values.ToList();
            var data = SerializerUtil.Serialize(holySitesList);
            _sapi.WorldManager.SaveGame.StoreData(DATA_KEY, data);
            _sapi.Logger.Debug($"[DivineAscension] Saved {holySitesList.Count} holy sites");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to save holy sites: {ex.Message}");
        }
    }

    #endregion
}
```

## Command Integration

**File:** `DivineAscension/Commands/HolySiteCommands.cs`

```csharp
using DivineAscension.Commands.Parsers;
using DivineAscension.Services;
using DivineAscension.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

public class HolySiteCommands
{
    private readonly ICoreServerAPI _sapi;
    private readonly HolySiteManager _holySiteManager;
    private readonly ReligionManager _religionManager;
    private readonly LocalizationService _localizationService;

    public HolySiteCommands(
        ICoreServerAPI sapi,
        HolySiteManager holySiteManager,
        ReligionManager religionManager,
        LocalizationService localizationService)
    {
        _sapi = sapi;
        _holySiteManager = holySiteManager;
        _religionManager = religionManager;
        _localizationService = localizationService;
    }

    public void RegisterCommands()
    {
        _sapi.ChatCommands
            .Create("holysite")
            .WithDescription("Manage religion holy sites")
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("consecrate")
                .WithDescription("Consecrate a land claim as a holy site")
                .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("siteName"))  // Use QuotedString for names with spaces
                .HandleWith(OnConsecrateCommand)
            .EndSubCommand()
            .BeginSubCommand("expand")
                .WithDescription("Expand holy site to adjacent claims")
                .HandleWith(OnExpandCommand)
            .EndSubCommand()
            .BeginSubCommand("deconsecrate")
                .WithDescription("Remove holy site consecration")
                .HandleWith(OnDeconsecateCommand)
            .EndSubCommand()
            .BeginSubCommand("info")
                .WithDescription("Get information about current holy site")
                .HandleWith(OnInfoCommand)
            .EndSubCommand()
            .BeginSubCommand("list")
                .WithDescription("List all holy sites for your religion")
                .HandleWith(OnListCommand)
            .EndSubCommand();
    }

    private TextCommandResult OnConsecrateCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found.");

        var siteName = args[0] as string;
        if (string.IsNullOrWhiteSpace(siteName))
        {
            return TextCommandResult.Error(_localizationService.Get("holysite-error-name-required",
                "Please provide a name for the holy site."));
        }

        if (_holySiteManager.ConsecrateHolySite(player, siteName, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnExpandCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found.");

        if (_holySiteManager.ExpandHolySite(player, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnDeconsecateCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found.");

        if (_holySiteManager.DeconsecateHolySite(player, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnInfoCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found.");

        if (_holySiteManager.IsPlayerInHolySite(player, out var holySite) && holySite != null)
        {
            var religion = _religionManager.GetReligion(holySite.ReligionUID);
            var info = _localizationService.Get("holysite-info-template",
                "Holy Site: {0}\nReligion: {1}\nTier: {2} ({3})\nSize: {4} chunks\nSacred Territory Bonus: {5}x\nPrayer Bonus: {6}x\nConsecrated: {7}",
                holySite.SiteName,
                religion?.ReligionName ?? "Unknown",
                holySite.Tier,
                GetTierName(holySite.Tier),
                holySite.GetTotalChunks(),
                holySite.GetSacredTerritoryMultiplier(),
                holySite.GetPrayerMultiplier(),
                holySite.ConsecrationDate.ToString("yyyy-MM-dd"));

            return TextCommandResult.Success(info);
        }

        return TextCommandResult.Error(_localizationService.Get("holysite-error-not-in-site",
            "You are not in a holy site."));
    }

    private TextCommandResult OnListCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found.");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);

        if (religion == null)
        {
            return TextCommandResult.Error(_localizationService.Get("holysite-error-no-religion",
                "You are not in a religion."));
        }

        var holySites = _holySiteManager.GetReligionHolySites(religion.ReligionUID);

        if (holySites.Count == 0)
        {
            return TextCommandResult.Success(_localizationService.Get("holysite-list-empty",
                "Your religion has no holy sites."));
        }

        var list = $"{religion.ReligionName} Holy Sites:\n";
        foreach (var site in holySites)
        {
            list += $"- {site.SiteName} (Tier {site.Tier}, {site.GetTotalChunks()} chunks)\n";
        }

        return TextCommandResult.Success(list);
    }

    private string GetTierName(int tier)
    {
        return tier switch
        {
            1 => _localizationService.Get("holysite-tier-sacred-ground", "Sacred Ground"),
            2 => _localizationService.Get("holysite-tier-shrine", "Shrine"),
            3 => _localizationService.Get("holysite-tier-temple", "Temple"),
            _ => _localizationService.Get("holysite-tier-unknown", "Unknown")
        };
    }
}
```

## Integration with Activity Bonus System

Update `ActivityBonusSystem.cs` to use land claim holy sites:

**File:** `DivineAscension/Systems/ActivityBonusSystem.cs`

```csharp
using DivineAscension.Services;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

public partial class ActivityBonusSystem
{
    private readonly HolySiteManager _holySiteManager;
    private readonly LocalizationService _localizationService;

    public void UpdateSacredTerritoryBonus(IServerPlayer player)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player);
        if (!playerData.HasDeity()) return;

        // Check if player is in their own religion's holy site
        bool inHolySite = _holySiteManager.IsPlayerInOwnHolySite(player, out var holySite);

        if (inHolySite && holySite != null)
        {
            // Apply bonus based on holy site tier
            float multiplier = holySite.GetSacredTerritoryMultiplier();

            player.Entity.Stats.Set(
                "passiveFavorMultiplier",
                "sacred_territory",
                multiplier
            );
        }
        else
        {
            // Remove bonus when leaving
            player.Entity.Stats.Remove("passiveFavorMultiplier", "sacred_territory");
        }
    }

    public void ApplyPrayerBonus(IServerPlayer player)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player);
        if (!playerData.HasDeity()) return;

        // Check if in holy site for bonus
        bool inHolySite = _holySiteManager.IsPlayerInOwnHolySite(player, out var holySiteData);

        float multiplier;
        double durationMinutes;

        if (inHolySite && holySiteData != null)
        {
            // Enhanced bonus in holy site
            multiplier = holySiteData.GetPrayerMultiplier();
            durationMinutes = 15.0;
        }
        else
        {
            // Base prayer bonus
            multiplier = 2.0f;
            durationMinutes = 10.0;
        }

        double expiryTime = _sapi.World.Calendar.TotalHours + (durationMinutes / 60.0);

        player.Entity.Stats.Set(
            "passiveFavorMultiplier",
            "prayer_bonus",
            multiplier,
            expiryTime
        );

        string message = inHolySite && holySiteData != null
            ? _localizationService.Get("prayer-bonus-holy-site",
                "The sacred ground of {0} amplifies your prayers! {1}x favor for {2} minutes!",
                holySiteData.SiteName, multiplier, durationMinutes)
            : _localizationService.Get("prayer-bonus-standard",
                "Your devotion is rewarded! {0}x favor for {1} minutes.",
                multiplier, durationMinutes);

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            message,
            EnumChatType.Notification
        );
    }
}
```

## Usage Examples

### Creating a Holy Site

```bash
# Player must own a land claim
# Stand in your claimed land
/holysite consecrate "Temple of Khoras"

# Result: Creates Tier 1 Sacred Ground (1 chunk, 32×32 blocks)
# - 1.5x sacred territory bonus while inside
# - 2.0x prayer bonus when praying inside
```

### Expanding a Holy Site

```bash
# Stand in your holy site
# Claim adjacent chunks
/holysite expand

# Result: Adds adjacent owned chunks (max 6 per site)
# - 1 chunk = Tier 1 Sacred Ground (1.5x territory, 2.0x prayer)
# - 2-3 chunks = Tier 2 Shrine (2.0x territory, 2.5x prayer)
# - 4-6 chunks = Tier 3 Temple (2.5x territory, 3.0x prayer)
```

### Praying at Holy Site

```bash
# Stand in your holy site
/deity pray

# Result: Enhanced prayer bonus based on tier
# - Tier 1 (Sacred Ground): 2.0x for 15 min
# - Tier 2 (Shrine): 2.5x for 15 min
# - Tier 3 (Temple): 3.0x for 15 min
```

### Getting Info

```bash
/holysite info

# Output:
# Holy Site: Temple of Khoras
# Religion: Warriors of Khoras
# Tier: 3 (Temple)
# Size: 5/6 chunks
# Sacred Territory Bonus: 2.5x
# Prayer Bonus: 3.0x
# Consecrated: 2025-11-11
```

## Initialization in DivineAscensionSystemInitializer

**Location:** Add after step 13 (Network handlers) in `DivineAscensionSystemInitializer.cs`

**Step 14: Holy Site Manager**

```csharp
// Initialize holy site manager
// Dependencies: ReligionManager, PlayerProgressionDataManager, ProfanityFilterService, LocalizationService
_holySiteManager = new HolySiteManager(
    api,
    _religionManager,
    _playerProgressionDataManager,
    _profanityFilterService,
    _localizationService
);
_holySiteManager.Initialize();

// Initialize activity bonus system with holy site support
_activityBonusSystem = new ActivityBonusSystem(
    api,
    _playerProgressionDataManager,
    _holySiteManager,
    _localizationService
);
_activityBonusSystem.Initialize();

// Register holy site commands
_holySiteCommands = new HolySiteCommands(
    api,
    _holySiteManager,
    _religionManager,
    _localizationService
);
_holySiteCommands.RegisterCommands();
```

**Dispose Order:** Add to `DivineAscensionModSystem.Dispose()`:

```csharp
_holySiteManager?.Dispose();
```

## Advantages of This Approach

✅ **Leverages Existing System**: Works with VS's built-in land claims
✅ **No Core Modification**: Overlay pattern doesn't touch VS internals
✅ **Persistent**: Saves/loads with world data
✅ **Scalable**: Supports multi-chunk temples
✅ **Tiered Progression**: Sacred Ground → Shrine → Temple (max 6 chunks per site)
✅ **Religion Ownership**: Tied to religion, not individual player
✅ **Territory Control**: Creates meaningful PvP zones
✅ **Visual Feedback**: Players can see their claimed land is special

## Network Packets

Holy sites need client-server synchronization for the GUI. Following the existing packet patterns:

### Request Packet

**File:** `DivineAscension/Network/HolySite/HolySiteRequestPacket.cs`

```csharp
using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
///     Client requests holy site information or actions
/// </summary>
[ProtoContract]
public class HolySiteRequestPacket
{
    public HolySiteRequestPacket()
    {
    }

    public HolySiteRequestPacket(string action, string siteId = "", string religionUID = "")
    {
        Action = action;
        SiteId = siteId;
        ReligionUID = religionUID;
    }

    /// <summary>
    ///     Action: "list", "detail", "religion_sites"
    /// </summary>
    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     Holy site ID (required for "detail" action)
    /// </summary>
    [ProtoMember(2)]
    public string SiteId { get; set; } = string.Empty;

    /// <summary>
    ///     Religion UID (optional filter for "list", required for "religion_sites")
    /// </summary>
    [ProtoMember(3)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    ///     Domain filter (optional for "list")
    /// </summary>
    [ProtoMember(4)]
    public string DomainFilter { get; set; } = string.Empty;
}
```

### Response Packet

**File:** `DivineAscension/Network/HolySite/HolySiteResponsePacket.cs`

```csharp
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
///     Server responds with holy site information
/// </summary>
[ProtoContract]
public class HolySiteResponsePacket
{
    public HolySiteResponsePacket()
    {
    }

    public HolySiteResponsePacket(bool success, string message = "")
    {
        Success = success;
        Message = message;
    }

    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public string Action { get; set; } = string.Empty;
    [ProtoMember(4)] public List<HolySiteInfo> Sites { get; set; } = new();
    [ProtoMember(5)] public HolySiteDetailInfo? DetailedSite { get; set; }

    [ProtoContract]
    public class HolySiteInfo
    {
        [ProtoMember(1)] public string HolySiteUID { get; set; } = string.Empty;
        [ProtoMember(2)] public string SiteName { get; set; } = string.Empty;
        [ProtoMember(3)] public string ReligionUID { get; set; } = string.Empty;
        [ProtoMember(4)] public string ReligionName { get; set; } = string.Empty;
        [ProtoMember(5)] public string DeityDomain { get; set; } = string.Empty;
        [ProtoMember(6)] public int Tier { get; set; }
        [ProtoMember(7)] public int ChunkCount { get; set; }
        [ProtoMember(8)] public int ChunkX { get; set; }
        [ProtoMember(9)] public int ChunkZ { get; set; }
    }

    [ProtoContract]
    public class HolySiteDetailInfo
    {
        [ProtoMember(1)] public string HolySiteUID { get; set; } = string.Empty;
        [ProtoMember(2)] public string SiteName { get; set; } = string.Empty;
        [ProtoMember(3)] public string ReligionUID { get; set; } = string.Empty;
        [ProtoMember(4)] public string ReligionName { get; set; } = string.Empty;
        [ProtoMember(5)] public string DeityDomain { get; set; } = string.Empty;
        [ProtoMember(6)] public string DesignatedByUID { get; set; } = string.Empty;
        [ProtoMember(7)] public string DesignatedByName { get; set; } = string.Empty;
        [ProtoMember(8)] public int Tier { get; set; }
        [ProtoMember(9)] public int ChunkCount { get; set; }
        [ProtoMember(10)] public float SacredTerritoryMultiplier { get; set; }
        [ProtoMember(11)] public float PrayerMultiplier { get; set; }
        [ProtoMember(12)] public long ConsecrationDateTicks { get; set; }
        [ProtoMember(13)] public bool IsActive { get; set; }
        [ProtoMember(14)] public List<ChunkInfo> ConnectedChunks { get; set; } = new();
    }

    [ProtoContract]
    public class ChunkInfo
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Z { get; set; }
    }
}
```

### Packet Registration

**In `DivineAscensionModSystem.Start()`:**

```csharp
api.Network.RegisterChannel(NETWORK_CHANNEL)
    // ... existing packets ...
    .RegisterMessageType<HolySiteRequestPacket>()
    .RegisterMessageType<HolySiteResponsePacket>();
```

### Server-Side Handler

**File:** `DivineAscension/Systems/Networking/Server/HolySiteNetworkHandler.cs`

```csharp
using System;
using System.Linq;
using DivineAscension.Network.HolySite;
using DivineAscension.Systems;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

public class HolySiteNetworkHandler : IServerNetworkHandler
{
    private readonly ICoreServerAPI _sapi;
    private readonly IServerNetworkChannel _serverChannel;
    private readonly HolySiteManager _holySiteManager;
    private readonly ReligionManager _religionManager;

    public HolySiteNetworkHandler(
        ICoreServerAPI sapi,
        IServerNetworkChannel channel,
        HolySiteManager holySiteManager,
        ReligionManager religionManager)
    {
        _sapi = sapi;
        _serverChannel = channel;
        _holySiteManager = holySiteManager;
        _religionManager = religionManager;
    }

    public void RegisterHandlers()
    {
        _serverChannel.SetMessageHandler<HolySiteRequestPacket>(OnHolySiteRequest);
    }

    public void Dispose()
    {
    }

    private void OnHolySiteRequest(IServerPlayer fromPlayer, HolySiteRequestPacket packet)
    {
        _sapi.Logger.Debug($"[HolySiteNetworkHandler] Received '{packet.Action}' from {fromPlayer.PlayerName}");

        var response = new HolySiteResponsePacket { Action = packet.Action };

        try
        {
            switch (packet.Action.ToLower())
            {
                case "list":
                    var allSites = _holySiteManager.GetAllHolySites();

                    // Apply domain filter if provided
                    if (!string.IsNullOrEmpty(packet.DomainFilter) && packet.DomainFilter != "All")
                    {
                        allSites = allSites
                            .Where(s => {
                                var religion = _religionManager.GetReligion(s.ReligionUID);
                                return religion?.Domain.ToString() == packet.DomainFilter;
                            })
                            .ToList();
                    }

                    response.Success = true;
                    response.Sites = allSites.Select(s => {
                        var religion = _religionManager.GetReligion(s.ReligionUID);
                        return new HolySiteResponsePacket.HolySiteInfo
                        {
                            HolySiteUID = s.HolySiteUID,
                            SiteName = s.SiteName,
                            ReligionUID = s.ReligionUID,
                            ReligionName = religion?.ReligionName ?? "Unknown",
                            DeityDomain = religion?.Domain.ToString() ?? "Unknown",
                            Tier = s.Tier,
                            ChunkCount = s.GetTotalChunks(),
                            ChunkX = s.ChunkPos.X,
                            ChunkZ = s.ChunkPos.Z
                        };
                    }).ToList();
                    break;

                case "religion_sites":
                    var religionSites = _holySiteManager.GetReligionHolySites(packet.ReligionUID);
                    var religionData = _religionManager.GetReligion(packet.ReligionUID);

                    response.Success = true;
                    response.Sites = religionSites.Select(s => new HolySiteResponsePacket.HolySiteInfo
                    {
                        HolySiteUID = s.HolySiteUID,
                        SiteName = s.SiteName,
                        ReligionUID = s.ReligionUID,
                        ReligionName = religionData?.ReligionName ?? "Unknown",
                        DeityDomain = religionData?.Domain.ToString() ?? "Unknown",
                        Tier = s.Tier,
                        ChunkCount = s.GetTotalChunks(),
                        ChunkX = s.ChunkPos.X,
                        ChunkZ = s.ChunkPos.Z
                    }).ToList();
                    break;

                case "detail":
                    var site = _holySiteManager.GetAllHolySites()
                        .FirstOrDefault(s => s.HolySiteUID == packet.SiteId);

                    if (site == null)
                    {
                        response.Success = false;
                        response.Message = "Holy site not found.";
                        break;
                    }

                    var siteReligion = _religionManager.GetReligion(site.ReligionUID);
                    var designator = _sapi.World.PlayerByUid(site.DesignatedBy);

                    response.Success = true;
                    response.DetailedSite = new HolySiteResponsePacket.HolySiteDetailInfo
                    {
                        HolySiteUID = site.HolySiteUID,
                        SiteName = site.SiteName,
                        ReligionUID = site.ReligionUID,
                        ReligionName = siteReligion?.ReligionName ?? "Unknown",
                        DeityDomain = siteReligion?.Domain.ToString() ?? "Unknown",
                        DesignatedByUID = site.DesignatedBy,
                        DesignatedByName = designator?.PlayerName ?? "Unknown",
                        Tier = site.Tier,
                        ChunkCount = site.GetTotalChunks(),
                        SacredTerritoryMultiplier = site.GetSacredTerritoryMultiplier(),
                        PrayerMultiplier = site.GetPrayerMultiplier(),
                        ConsecrationDateTicks = site.ConsecrationDateTicks,
                        IsActive = site.IsActive,
                        ConnectedChunks = site.ConnectedChunks.Select(c =>
                            new HolySiteResponsePacket.ChunkInfo { X = c.X, Z = c.Z }).ToList()
                    };
                    break;

                default:
                    response.Success = false;
                    response.Message = $"Unknown action: {packet.Action}";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "An error occurred processing your request.";
            _sapi.Logger.Error($"[HolySiteNetworkHandler] Error: {ex.Message}");
        }

        _serverChannel.SendPacket(response, fromPlayer);
    }
}
```

### Client-Side Handler

**Add to `DivineAscensionNetworkClient.cs`:**

```csharp
// Event delegate
public event Action<HolySiteResponsePacket>? HolySiteDataReceived;

// In RegisterHandlers():
_clientChannel.SetMessageHandler<HolySiteResponsePacket>(OnHolySiteResponse);

// Handler method:
private void OnHolySiteResponse(HolySiteResponsePacket packet)
{
    HolySiteDataReceived?.Invoke(packet);
}

// In Dispose():
HolySiteDataReceived = null;
```

## GUI Integration

Holy sites are displayed as a **subtab under the Religion tab**, alongside existing subtabs (Browse, Info, Activity, Invites, Create, Roles).

### File Structure

```
DivineAscension/GUI/
├── State/
│   └── Religion/
│       ├── SubTab.cs                     # Add HolySites = 6
│       ├── HolySitesState.cs             # Holy sites subtab state (NEW)
│       └── HolySiteDetailState.cs        # Detail view state (NEW)
├── Events/
│   └── Religion/
│       └── HolySitesEvent.cs             # Holy sites events (NEW)
├── Models/
│   └── Religion/
│       └── HolySites/
│           ├── HolySitesViewModel.cs     # List view model (NEW)
│           └── HolySiteDetailViewModel.cs # Detail view model (NEW)
├── UI/Renderers/
│   └── Religion/
│       └── HolySites/
│           ├── HolySitesRenderer.cs      # List renderer (NEW)
│           └── HolySiteDetailRenderer.cs # Detail renderer (NEW)
└── Managers/
    └── ReligionStateManager.cs           # Add holy sites handling
```

### Update Religion SubTab Enum

**File:** `DivineAscension/GUI/State/Religion/SubTab.cs`

```csharp
namespace DivineAscension.GUI.State.Religion;

public enum SubTab
{
    Browse = 0,
    Info = 1,
    Activity = 2,
    Invites = 3,
    Create = 4,
    Roles = 5,
    HolySites = 6  // NEW
}
```

### State Container

**File:** `DivineAscension/GUI/State/Religion/HolySitesState.cs`

```csharp
using System.Collections.Generic;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.State.Religion;

/// <summary>
/// State for the Holy Sites subtab within the Religion tab
/// </summary>
public class HolySitesState
{
    public List<HolySiteResponsePacket.HolySiteInfo> Sites { get; set; } = new();
    public string? SelectedSiteId { get; set; }
    public bool IsLoading { get; set; }
    public bool ShowingDetail { get; set; }
    public float ScrollY { get; set; }
    public string? LastError { get; set; }

    // Site counts for display
    public int CurrentSiteCount { get; set; }
    public int MaxSiteCount { get; set; }

    public void Reset()
    {
        Sites.Clear();
        SelectedSiteId = null;
        IsLoading = false;
        ShowingDetail = false;
        ScrollY = 0f;
        LastError = null;
    }
}
```

**File:** `DivineAscension/GUI/State/Religion/HolySiteDetailState.cs`

```csharp
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.State.Religion;

/// <summary>
/// State for viewing a specific holy site's details
/// </summary>
public class HolySiteDetailState
{
    public HolySiteResponsePacket.HolySiteInfo? Site { get; set; }
    public bool IsLoading { get; set; }
    public string? LastError { get; set; }

    public void Reset()
    {
        Site = null;
        IsLoading = false;
        LastError = null;
    }
}
```

### Events

**File:** `DivineAscension/GUI/Events/Religion/HolySitesEvent.cs`

```csharp
namespace DivineAscension.GUI.Events.Religion;

/// <summary>
/// Events for the Holy Sites subtab
/// </summary>
public abstract record HolySitesEvent
{
    public record SiteSelected(string SiteId) : HolySitesEvent;
    public record ViewDetailClicked(string SiteId) : HolySitesEvent;
    public record BackToListClicked : HolySitesEvent;
    public record RefreshClicked : HolySitesEvent;
    public record ScrollChanged(float NewScrollY) : HolySitesEvent;
    public record DismissError : HolySitesEvent;
}
```

### Integration with ReligionStateManager

The holy sites functionality integrates into the existing `ReligionStateManager` rather than having a separate manager.

**Updates to:** `DivineAscension/GUI/Managers/ReligionStateManager.cs`

```csharp
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.State.Religion;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;
using Vintagestory.API.Client;

// Add to existing ReligionStateManager class:

public partial class ReligionStateManager
{
    // Add holy sites state to ReligionTabState
    public HolySitesState HolySitesState { get; } = new();
    public HolySiteDetailState HolySiteDetailState { get; } = new();

    /// <summary>
    /// Called when holy site data is received from the server
    /// </summary>
    public void OnHolySiteDataReceived(HolySiteResponsePacket packet)
    {
        HolySitesState.IsLoading = false;

        if (!packet.Success)
        {
            HolySitesState.LastError = packet.Message;
            return;
        }

        switch (packet.Action.ToLower())
        {
            case "list":
            case "religion_sites":
                HolySitesState.Sites = packet.Sites;
                HolySitesState.CurrentSiteCount = packet.CurrentSiteCount;
                HolySitesState.MaxSiteCount = packet.MaxSiteCount;
                break;
            case "detail":
                if (packet.DetailedSite != null)
                {
                    HolySiteDetailState.Site = packet.DetailedSite;
                    HolySitesState.ShowingDetail = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handles events from the holy sites subtab
    /// </summary>
    public void HandleHolySitesEvent(HolySitesEvent ev)
    {
        switch (ev)
        {
            case HolySitesEvent.SiteSelected sel:
                HolySitesState.SelectedSiteId = sel.SiteId;
                break;
            case HolySitesEvent.ViewDetailClicked vd:
                HolySiteDetailState.IsLoading = true;
                _uiService.RequestHolySiteDetail(vd.SiteId);
                break;
            case HolySitesEvent.BackToListClicked:
                HolySitesState.ShowingDetail = false;
                HolySiteDetailState.Reset();
                break;
            case HolySitesEvent.RefreshClicked:
                RequestReligionHolySites();
                break;
            case HolySitesEvent.ScrollChanged sc:
                HolySitesState.ScrollY = sc.NewScrollY;
                break;
            case HolySitesEvent.DismissError:
                HolySitesState.LastError = null;
                break;
        }
    }

    /// <summary>
    /// Requests holy sites for the player's current religion
    /// </summary>
    private void RequestReligionHolySites()
    {
        if (_currentReligionUID == null) return;
        HolySitesState.IsLoading = true;
        _uiService.RequestReligionHolySites(_currentReligionUID);
    }

    // Add to OnSubTabChanged handler:
    // case SubTab.HolySites:
    //     RequestReligionHolySites();
    //     break;

    // Add to Reset():
    // HolySitesState.Reset();
    // HolySiteDetailState.Reset();
}
```

### Subtab Rendering

The holy sites subtab is rendered within the Religion tab's content area when `SubTab.HolySites` is selected.

**Add case to religion subtab routing in `ReligionStateManager`:**

```csharp
case SubTab.HolySites:
    DrawHolySitesSubtab(x, y, width, height);
    break;
```

**Holy sites subtab drawing method:**

```csharp
private void DrawHolySitesSubtab(float x, float y, float width, float height)
{
    if (HolySitesState.ShowingDetail)
    {
        DrawHolySiteDetail(x, y, width, height);
        return;
    }

    var vm = new HolySitesViewModel(
        sites: HolySitesState.Sites,
        isLoading: HolySitesState.IsLoading,
        selectedSiteId: HolySitesState.SelectedSiteId,
        scrollY: HolySitesState.ScrollY,
        currentSiteCount: HolySitesState.CurrentSiteCount,
        maxSiteCount: HolySitesState.MaxSiteCount,
        lastError: HolySitesState.LastError,
        x: x, y: y, width: width, height: height);

    var result = HolySitesRenderer.Draw(vm, ImGui.GetWindowDrawList());

    foreach (var ev in result.Events)
    {
        HandleHolySitesEvent(ev);
    }
}

private void DrawHolySiteDetail(float x, float y, float width, float height)
{
    var vm = new HolySiteDetailViewModel(
        site: HolySiteDetailState.Site,
        isLoading: HolySiteDetailState.IsLoading,
        lastError: HolySiteDetailState.LastError,
        x: x, y: y, width: width, height: height);

    var result = HolySiteDetailRenderer.Draw(vm, ImGui.GetWindowDrawList());

    foreach (var ev in result.Events)
    {
        HandleHolySitesEvent(ev);
    }
}
```

### Subtab Button

Add the "Holy Sites" button to the religion tab's subtab bar. The button should only be visible when the player is in a religion.

**In religion subtab renderer:**

```csharp
// Add to subtab buttons (only show if player is in a religion)
if (isInReligion)
{
    if (RenderSubTabButton("Holy Sites", currentSubTab == SubTab.HolySites, ref buttonX, y))
    {
        events.Add(new SubTabChangedEvent(SubTab.HolySites));
    }
}
```

## Localization Keys

Add these keys to `DivineAscension/Constants/LocalizationKeys.cs`:

```csharp
#region Holy Site UI (Religion Subtab)
public const string UI_RELIGION_SUBTAB_HOLYSITES = "divineascension:ui.religion.subtab.holysites";
public const string UI_HOLYSITE_LIST_TITLE = "divineascension:ui.holysite.list.title";
public const string UI_HOLYSITE_DETAIL_TITLE = "divineascension:ui.holysite.detail.title";
public const string UI_HOLYSITE_SITE_COUNT = "divineascension:ui.holysite.site_count";
public const string UI_HOLYSITE_TABLE_NAME = "divineascension:ui.holysite.table.name";
public const string UI_HOLYSITE_TABLE_TIER = "divineascension:ui.holysite.table.tier";
public const string UI_HOLYSITE_TABLE_CHUNKS = "divineascension:ui.holysite.table.chunks";
public const string UI_HOLYSITE_DETAIL_CONSECRATED_BY = "divineascension:ui.holysite.detail.consecrated_by";
public const string UI_HOLYSITE_DETAIL_CONSECRATED_ON = "divineascension:ui.holysite.detail.consecrated_on";
public const string UI_HOLYSITE_DETAIL_TERRITORY_BONUS = "divineascension:ui.holysite.detail.territory_bonus";
public const string UI_HOLYSITE_DETAIL_PRAYER_BONUS = "divineascension:ui.holysite.detail.prayer_bonus";
public const string UI_HOLYSITE_TIER_SACRED_GROUND = "divineascension:ui.holysite.tier.sacred-ground";
public const string UI_HOLYSITE_TIER_SHRINE = "divineascension:ui.holysite.tier.shrine";
public const string UI_HOLYSITE_TIER_TEMPLE = "divineascension:ui.holysite.tier.temple";
public const string UI_HOLYSITE_NO_SITES = "divineascension:ui.holysite.no_sites";
public const string UI_HOLYSITE_LOADING = "divineascension:ui.holysite.loading";
#endregion

#region Holy Site Commands
public const string CMD_HOLYSITE_CONSECRATE_SUCCESS = "divineascension:cmd.holysite.consecrate.success";
public const string CMD_HOLYSITE_EXPAND_SUCCESS = "divineascension:cmd.holysite.expand.success";
public const string CMD_HOLYSITE_DECONSECRATE_SUCCESS = "divineascension:cmd.holysite.deconsecrate.success";
public const string CMD_HOLYSITE_ERROR_NO_RELIGION = "divineascension:cmd.holysite.error.no_religion";
public const string CMD_HOLYSITE_ERROR_NOT_FOUNDER = "divineascension:cmd.holysite.error.not_founder";
public const string CMD_HOLYSITE_ERROR_NAME_TOO_SHORT = "divineascension:cmd.holysite.error.name_too_short";
public const string CMD_HOLYSITE_ERROR_NAME_TOO_LONG = "divineascension:cmd.holysite.error.name_too_long";
public const string CMD_HOLYSITE_ERROR_PROFANITY = "divineascension:cmd.holysite.error.profanity";
public const string CMD_HOLYSITE_ERROR_NO_CLAIM = "divineascension:cmd.holysite.error.no_claim";
public const string CMD_HOLYSITE_ERROR_NOT_OWNER = "divineascension:cmd.holysite.error.not_owner";
public const string CMD_HOLYSITE_ERROR_ALREADY_CONSECRATED = "divineascension:cmd.holysite.error.already_consecrated";
public const string CMD_HOLYSITE_ERROR_NOT_IN_SITE = "divineascension:cmd.holysite.error.not_in_site";
public const string CMD_HOLYSITE_ERROR_NO_ADJACENT = "divineascension:cmd.holysite.error.no_adjacent";
#endregion

#region Holy Site Network
public const string NET_HOLYSITE_NOT_FOUND = "divineascension:net.holysite.not_found";
public const string NET_HOLYSITE_PRAYER_BONUS = "divineascension:net.holysite.prayer_bonus";
public const string NET_HOLYSITE_TERRITORY_BONUS = "divineascension:net.holysite.territory_bonus";
#endregion
```

### English Translations

Add to `DivineAscension/assets/divineascension/lang/en.json`:

```json
{
  "_comment": "Holy Site UI (Religion Subtab)",
  "divineascension:ui.religion.subtab.holysites": "Holy Sites",
  "divineascension:ui.holysite.list.title": "Holy Sites",
  "divineascension:ui.holysite.detail.title": "Holy Site Details",
  "divineascension:ui.holysite.site_count": "Sites: {0}/{1}",
  "divineascension:ui.holysite.table.name": "Name",
  "divineascension:ui.holysite.table.tier": "Tier",
  "divineascension:ui.holysite.table.chunks": "Size",
  "divineascension:ui.holysite.detail.consecrated_by": "Consecrated By",
  "divineascension:ui.holysite.detail.consecrated_on": "Consecrated On",
  "divineascension:ui.holysite.detail.territory_bonus": "Sacred Territory Bonus: {0}x",
  "divineascension:ui.holysite.detail.prayer_bonus": "Prayer Bonus: {0}x",
  "divineascension:ui.holysite.tier.sacred-ground": "Sacred Ground",
  "divineascension:ui.holysite.tier.shrine": "Shrine",
  "divineascension:ui.holysite.tier.temple": "Temple",
  "divineascension:ui.holysite.no_sites": "Your religion has no holy sites yet.",
  "divineascension:ui.holysite.loading": "Loading holy sites...",

  "_comment": "Holy Site Commands",
  "divineascension:cmd.holysite.consecrate.success": "You have consecrated {0} as a holy site for {1}! ({2}/{3} sites)",
  "divineascension:cmd.holysite.expand.success": "{0} expanded by {1} chunk(s)! Now {2}/{3} chunks (Tier {4}: {5}).",
  "divineascension:cmd.holysite.deconsecrate.success": "{0} has been deconsecrated.",
  "divineascension:cmd.holysite.error.no_religion": "You must be in a religion to consecrate holy sites.",
  "divineascension:cmd.holysite.error.not_founder": "Only the religion founder can consecrate holy sites.",
  "divineascension:cmd.holysite.error.name_too_short": "Holy site name must be at least 3 characters.",
  "divineascension:cmd.holysite.error.name_too_long": "Holy site name must be 50 characters or less.",
  "divineascension:cmd.holysite.error.profanity": "Holy site name contains inappropriate content.",
  "divineascension:cmd.holysite.error.no_claim": "You must be standing in a claimed area.",
  "divineascension:cmd.holysite.error.not_owner": "You must own this land claim to consecrate it.",
  "divineascension:cmd.holysite.error.already_consecrated": "This land is already consecrated.",
  "divineascension:cmd.holysite.error.site_limit": "Your religion has reached its holy site limit ({0} sites at {1} rank). Gain more prestige to unlock additional sites.",
  "divineascension:cmd.holysite.error.max_chunks": "This holy site has reached its maximum size ({0} chunks). Consider creating a new holy site in another location.",
  "divineascension:cmd.holysite.error.not_in_site": "You must be standing in a holy site.",
  "divineascension:cmd.holysite.error.no_adjacent": "No adjacent unconsecrated claims found.",

  "_comment": "Holy Site Network",
  "divineascension:net.holysite.not_found": "Holy site not found.",
  "divineascension:net.holysite.prayer_bonus": "The sacred ground of {0} amplifies your prayers! {1}x favor for {2} minutes!",
  "divineascension:net.holysite.territory_bonus": "Your devotion is rewarded! {0}x favor for {1} minutes."
}
```

## Future Enhancements

1. **Visual Markers**: Place special blocks/particles at holy site boundaries
2. **Prestige Cost**: Require prestige to consecrate/expand
3. **Maintenance**: Require periodic offerings to keep site active
4. **Territory Buffs**: Additional benefits (spawn protection, faster crafting)
5. **Raids**: Allow attacking enemy holy sites for prestige
6. **Blessings**: Unlock site-specific blessings at higher tiers
7. **Pilgrimage**: Visiting other religion's holy sites could grant temporary buffs

## Summary

This land claim integration:
- Uses Vintage Story's existing claim API
- Creates an overlay metadata system
- Supports expandable multi-chunk temples
- Provides tier-based progression
- Integrates seamlessly with Phase 3 activity bonuses
- Requires zero changes to VS core systems

The holy site system becomes a core endgame feature for religions!
