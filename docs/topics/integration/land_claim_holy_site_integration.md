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
    /// Gets the total number of chunks in this holy site
    /// </summary>
    public int GetTotalChunks()
    {
        return ConnectedChunks.Count;
    }

    /// <summary>
    /// Calculates the tier based on chunk count
    /// </summary>
    public void UpdateTier()
    {
        int chunkCount = GetTotalChunks();
        Tier = chunkCount switch
        {
            1 => 1,                    // Shrine (1 chunk)
            >= 2 and <= 8 => 2,        // Temple (2-8 chunks)
            >= 9 => 3                  // Cathedral (9+ chunks)
        };
    }

    /// <summary>
    /// Gets the sacred territory multiplier for this holy site
    /// </summary>
    public float GetSacredTerritoryMultiplier()
    {
        return Tier switch
        {
            1 => 1.5f,  // Shrine
            2 => 2.0f,  // Temple
            3 => 2.5f,  // Cathedral
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
            1 => 2.0f,  // Shrine
            2 => 2.5f,  // Temple
            3 => 3.0f,  // Cathedral
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
    private readonly PlayerProgressionDataManager _playerProgressionDataManager;
    private readonly ProfanityFilterService _profanityFilterService;
    private readonly LocalizationService _localizationService;

    // Map chunk coordinates to holy site data
    private readonly Dictionary<SerializableChunkPos, HolySiteData> _holySitesByChunk = new();

    // Map holy site UID to data
    private readonly Dictionary<string, HolySiteData> _holySitesById = new();

    public HolySiteManager(
        ICoreServerAPI sapi,
        ReligionManager religionManager,
        PlayerProgressionDataManager playerProgressionDataManager,
        ProfanityFilterService profanityFilterService,
        LocalizationService localizationService)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _playerProgressionDataManager = playerProgressionDataManager;
        _profanityFilterService = profanityFilterService;
        _localizationService = localizationService;
    }

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

        message = _localizationService.Get("holysite-consecrated",
            "You have consecrated {0} as a holy site for {1}!",
            siteName, religion.ReligionName);

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

        // Find the main holy site to expand
        var mainHolySite = _holySitesById[adjacentSite.HolySiteUID];

        // Add chunks to the holy site
        foreach (var chunk in expandableChunks)
        {
            mainHolySite.ConnectedChunks.Add(chunk);
            _holySitesByChunk[chunk] = mainHolySite;
        }

        mainHolySite.UpdateTier();

        message = _localizationService.Get("holysite-expanded",
            "{0} expanded! Now {1} chunks (Tier {2}).",
            mainHolySite.SiteName, mainHolySite.GetTotalChunks(), mainHolySite.Tier);

        return true;
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
            1 => _localizationService.Get("holysite-tier-shrine", "Shrine"),
            2 => _localizationService.Get("holysite-tier-temple", "Temple"),
            3 => _localizationService.Get("holysite-tier-cathedral", "Cathedral"),
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

# Result: Creates Tier 1 Shrine (1 chunk)
# - 1.5x sacred territory bonus while inside
# - 2.0x prayer bonus when praying inside
```

### Expanding a Holy Site

```bash
# Stand in your holy site
# Claim adjacent chunks
/holysite expand

# Result: Adds adjacent owned chunks
# - 2-8 chunks = Tier 2 Temple (2.0x territory, 2.5x prayer)
# - 9+ chunks = Tier 3 Cathedral (2.5x territory, 3.0x prayer)
```

### Praying at Holy Site

```bash
# Stand in your holy site
/deity pray

# Result: Enhanced prayer bonus based on tier
# - Tier 1: 2.0x for 15 min
# - Tier 2: 2.5x for 15 min
# - Tier 3: 3.0x for 15 min
```

### Getting Info

```bash
/holysite info

# Output:
# Holy Site: Temple of Khoras
# Religion: Warriors of Khoras
# Tier: 2 (Temple)
# Size: 5 chunks
# Sacred Territory Bonus: 2.0x
# Prayer Bonus: 2.5x
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
✅ **Tiered Progression**: Shrine → Temple → Cathedral
✅ **Religion Ownership**: Tied to religion, not individual player
✅ **Territory Control**: Creates meaningful PvP zones
✅ **Visual Feedback**: Players can see their claimed land is special

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
