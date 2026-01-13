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
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace DivineAscension.Data;

[ProtoContract]
public class HolySiteData
{
    /// <summary>
    /// Unique identifier for this holy site
    /// </summary>
    [ProtoMember(1)]
    public string HolySiteUID { get; set; } = string.Empty;

    /// <summary>
    /// Chunk coordinates of the claimed land
    /// </summary>
    [ProtoMember(2)]
    public Vec2i ChunkPos { get; set; }

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
    /// When the site was consecrated
    /// </summary>
    [ProtoMember(6)]
    public DateTime ConsecrationDate { get; set; }

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
    public List<Vec2i> ConnectedChunks { get; set; } = new();

    public HolySiteData() { }

    public HolySiteData(string holySiteUID, Vec2i chunkPos, string religionUID, string siteName, string designatedBy)
    {
        HolySiteUID = holySiteUID;
        ChunkPos = chunkPos;
        ReligionUID = religionUID;
        SiteName = siteName;
        DesignatedBy = designatedBy;
        ConsecrationDate = DateTime.UtcNow;
        ConnectedChunks = new List<Vec2i> { chunkPos };
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

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
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
    private readonly PlayerProgressionDataManager _playerDataManager;

    // Map chunk coordinates to holy site data
    private readonly Dictionary<Vec2i, HolySiteData> _holySitesByChunk = new();

    // Map holy site UID to data
    private readonly Dictionary<string, HolySiteData> _holySitesById = new();

    public HolySiteManager(ICoreServerAPI sapi, ReligionManager religionManager, PlayerProgressionDataManager playerDataManager)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _playerDataManager = playerDataManager;
    }

    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Holy Site Manager...");

        // Register persistence handlers
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        _sapi.Logger.Notification("[DivineAscension] Holy Site Manager initialized");
    }

    #region Holy Site Creation

    /// <summary>
    /// Consecrates a land claim as a holy site
    /// </summary>
    public bool ConsecrateHolySite(IServerPlayer player, string siteName, out string message)
    {
        var playerData = _playerDataManager.GetOrCreatePlayerData(player);
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);

        // Validation
        if (religion == null)
        {
            message = "You must be in a religion to consecrate holy sites.";
            return false;
        }

        if (!religion.IsFounder(player.PlayerUID))
        {
            message = "Only the religion founder can consecrate holy sites.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(siteName) || siteName.Length < 3)
        {
            message = "Holy site name must be at least 3 characters.";
            return false;
        }

        // Get land claim at player's position
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkPos = pos.ToChunkPos();
        var claim = _sapi.World.Claims.Get(chunkPos);

        if (claim == null)
        {
            message = "You must be standing in a claimed area.";
            return false;
        }

        if (!claim.OwnedByPlayerUid.Contains(player.PlayerUID))
        {
            message = "You must own this land claim to consecrate it.";
            return false;
        }

        // Check if already consecrated
        var chunkCoord = new Vec2i(chunkPos.X, chunkPos.Y);
        if (_holySitesByChunk.ContainsKey(chunkCoord))
        {
            message = "This land is already consecrated.";
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

        message = $"You have consecrated {siteName} as a holy site for {religion.ReligionName}!";

        _sapi.Logger.Notification($"[DivineAscension] Holy site '{siteName}' created by {player.PlayerName}");

        return true;
    }

    /// <summary>
    /// Expands a holy site to include adjacent land claims
    /// </summary>
    public bool ExpandHolySite(IServerPlayer player, out string message)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkPos = pos.ToChunkPos();
        var chunkCoord = new Vec2i(chunkPos.X, chunkPos.Y);

        // Check if standing in a holy site
        if (!_holySitesByChunk.TryGetValue(chunkCoord, out var adjacentSite))
        {
            message = "You must be standing in a holy site to expand it.";
            return false;
        }

        var religion = _religionManager.GetReligion(adjacentSite.ReligionUID);
        if (religion == null || !religion.IsFounder(player.PlayerUID))
        {
            message = "Only the religion founder can expand holy sites.";
            return false;
        }

        // Get adjacent chunks
        var adjacentChunks = GetAdjacentChunks(chunkCoord);
        var expandableChunks = new List<Vec2i>();

        foreach (var adjChunk in adjacentChunks)
        {
            // Check if chunk is claimed by player
            var claim = _sapi.World.Claims.Get(new ChunkPos2D(adjChunk.X, adjChunk.Y));
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
            message = "No adjacent unconsecrated claims found.";
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

        message = $"{mainHolySite.SiteName} expanded! Now {mainHolySite.GetTotalChunks()} chunks (Tier {mainHolySite.Tier}).";

        return true;
    }

    /// <summary>
    /// Deconsecrates a holy site
    /// </summary>
    public bool DeconsectrateHolySite(IServerPlayer player, out string message)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkPos = pos.ToChunkPos();
        var chunkCoord = new Vec2i(chunkPos.X, chunkPos.Y);

        if (!_holySitesByChunk.TryGetValue(chunkCoord, out var holySite))
        {
            message = "You are not standing in a holy site.";
            return false;
        }

        var religion = _religionManager.GetReligion(holySite.ReligionUID);
        if (religion == null || !religion.IsFounder(player.PlayerUID))
        {
            message = "Only the religion founder can deconsecrate holy sites.";
            return false;
        }

        // Remove from all mappings
        foreach (var chunk in holySite.ConnectedChunks)
        {
            _holySitesByChunk.Remove(chunk);
        }
        _holySitesById.Remove(holySite.HolySiteUID);

        message = $"{holySite.SiteName} has been deconsecrated.";

        return true;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Checks if a player is currently in a holy site
    /// </summary>
    public bool IsPlayerInHolySite(IServerPlayer player, out HolySiteData holySite)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkPos = pos.ToChunkPos();
        var chunkCoord = new Vec2i(chunkPos.X, chunkPos.Y);

        return _holySitesByChunk.TryGetValue(chunkCoord, out holySite);
    }

    /// <summary>
    /// Checks if a player is in their own religion's holy site
    /// </summary>
    public bool IsPlayerInOwnHolySite(IServerPlayer player, out HolySiteData holySite)
    {
        if (IsPlayerInHolySite(player, out holySite))
        {
            var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            return religion != null && religion.ReligionUID == holySite.ReligionUID;
        }

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
    public HolySiteData GetHolySiteAtPosition(BlockPos pos)
    {
        var chunkPos = pos.ToChunkPos();
        var chunkCoord = new Vec2i(chunkPos.X, chunkPos.Y);
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

    private List<Vec2i> GetAdjacentChunks(Vec2i center)
    {
        return new List<Vec2i>
        {
            new Vec2i(center.X + 1, center.Y),     // East
            new Vec2i(center.X - 1, center.Y),     // West
            new Vec2i(center.X, center.Y + 1),     // South
            new Vec2i(center.X, center.Y - 1),     // North
            new Vec2i(center.X + 1, center.Y + 1), // Southeast
            new Vec2i(center.X + 1, center.Y - 1), // Northeast
            new Vec2i(center.X - 1, center.Y + 1), // Southwest
            new Vec2i(center.X - 1, center.Y - 1)  // Northwest
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
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

public class HolySiteCommands
{
    private readonly ICoreServerAPI _sapi;
    private readonly HolySiteManager _holySiteManager;
    private readonly ReligionManager _religionManager;

    public HolySiteCommands(ICoreServerAPI sapi, HolySiteManager holySiteManager, ReligionManager religionManager)
    {
        _sapi = sapi;
        _holySiteManager = holySiteManager;
        _religionManager = religionManager;
    }

    public void RegisterCommands()
    {
        _sapi.ChatCommands
            .Create("holysite")
            .WithDescription("Manage religion holy sites")
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("consecrate")
                .WithDescription("Consecrate a land claim as a holy site")
                .WithArgs(_sapi.ChatCommands.Parsers.Word("siteName"))
                .HandleWith(OnConsecrateCommand)
            .EndSubCommand()
            .BeginSubCommand("expand")
                .WithDescription("Expand holy site to adjacent claims")
                .HandleWith(OnExpandCommand)
            .EndSubCommand()
            .BeginSubCommand("deconsecrate")
                .WithDescription("Remove holy site consecration")
                .HandleWith(OnDeconsectrateCommand)
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
        var siteName = args[0] as string;

        if (_holySiteManager.ConsecrateHolySite(player, siteName, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnExpandCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;

        if (_holySiteManager.ExpandHolySite(player, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnDeconsectrateCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;

        if (_holySiteManager.DeconsectrateHolySite(player, out string message))
        {
            return TextCommandResult.Success(message);
        }

        return TextCommandResult.Error(message);
    }

    private TextCommandResult OnInfoCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;

        if (_holySiteManager.IsPlayerInHolySite(player, out var holySite))
        {
            var religion = _religionManager.GetReligion(holySite.ReligionUID);
            var info = $"Holy Site: {holySite.SiteName}\n" +
                      $"Religion: {religion?.ReligionName ?? "Unknown"}\n" +
                      $"Tier: {holySite.Tier} ({GetTierName(holySite.Tier)})\n" +
                      $"Size: {holySite.GetTotalChunks()} chunks\n" +
                      $"Sacred Territory Bonus: {holySite.GetSacredTerritoryMultiplier()}x\n" +
                      $"Prayer Bonus: {holySite.GetPrayerMultiplier()}x\n" +
                      $"Consecrated: {holySite.ConsecrationDate:yyyy-MM-dd}";

            return TextCommandResult.Success(info);
        }

        return TextCommandResult.Error("You are not in a holy site.");
    }

    private TextCommandResult OnListCommand(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);

        if (religion == null)
        {
            return TextCommandResult.Error("You are not in a religion.");
        }

        var holySites = _holySiteManager.GetReligionHolySites(religion.ReligionUID);

        if (holySites.Count == 0)
        {
            return TextCommandResult.Success("Your religion has no holy sites.");
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
            1 => "Shrine",
            2 => "Temple",
            3 => "Cathedral",
            _ => "Unknown"
        };
    }
}
```

## Integration with Activity Bonus System

Update `ActivityBonusSystem.cs` to use land claim holy sites:

```csharp
public void UpdateSacredTerritoryBonus(IServerPlayer player)
{
    var playerData = _playerDataManager.GetOrCreatePlayerData(player);
    if (!playerData.HasDeity()) return;

    // Check if player is in their own religion's holy site
    bool inHolySite = _holySiteManager.IsPlayerInOwnHolySite(player, out var holySite);

    if (inHolySite)
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
    var playerData = _playerDataManager.GetOrCreatePlayerData(player);
    if (!playerData.HasDeity()) return;

    // Check if in holy site for bonus
    bool inHolySite = _holySiteManager.IsPlayerInOwnHolySite(player, out var holySiteData);

    float multiplier;
    double durationMinutes;

    if (inHolySite)
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

    string message = inHolySite
        ? $"The sacred ground of {holySiteData.SiteName} amplifies your prayers! {multiplier}x favor for {durationMinutes} minutes!"
        : $"Your devotion is rewarded! {multiplier}x favor for {durationMinutes} minutes.";

    player.SendMessage(
        GlobalConstants.GeneralChatGroup,
        message,
        EnumChatType.Notification
    );
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

## Initialization in DivineAscensionModSystem

```csharp
// Initialize holy site manager (Phase 3)
_holySiteManager = new HolySiteManager(api, _religionManager, _playerDataManager);
_holySiteManager.Initialize();

// Initialize activity bonus system with holy site support
_activityBonusSystem = new ActivityBonusSystem(api, _playerDataManager, _holySiteManager);
_activityBonusSystem.Initialize();

// Register holy site commands
_holySiteCommands = new HolySiteCommands(api, _holySiteManager, _religionManager);
_holySiteCommands.RegisterCommands();
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
