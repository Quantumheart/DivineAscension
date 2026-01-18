# Implementation Plan: Player-Designed Custom Domains

## Overview

This feature allows players to create custom deity domains beyond the five built-in domains (Craft, Wild, Conquest, Harvest, Stone). Players will be able to:

- Design custom domains with unique names, descriptions, colors, and icons
- Configure which game activities award favor for their custom domain
- Create blessings associated with custom domains
- Found religions using custom domains
- Form civilizations with custom domain religions

This is a **major architectural change** that transforms the domain system from a hardcoded enum to a dynamic, extensible registry pattern.

## Acceptance Criteria

1. **Domain Creation**: Players can create custom domains via GUI and commands
2. **Favor Configuration**: Custom domains can specify which activities award favor (mining, crafting, hunting, etc.)
3. **Visual Customization**: Custom domains have configurable names, descriptions, colors, and icons
4. **Blessing Support**: Players can create custom blessings for their domains
5. **Religion Integration**: Religions can be founded with custom domains
6. **Civilization Compatibility**: Civilizations enforce domain uniqueness across built-in and custom domains
7. **Persistence**: Custom domains persist across server restarts
8. **Backward Compatibility**: Existing religions with built-in domains continue to work
9. **Permission System**: Domain creators have admin rights over their domains
10. **Content Moderation**: Profanity filter applies to domain names and descriptions

---

## Architecture Decision: Three-Phase Transformation

Given the tight coupling of the `DeityDomain` enum (30+ files), this implementation will proceed in **three major phases**:

### Phase 1: Foundation (Domain Registry & Data Models)
- Create domain data model and registry
- Migrate built-in domains to domain IDs
- Update data persistence layer
- **No breaking changes** - enum still exists, accessed through registry

### Phase 2: System Integration (Favor & Blessings)
- Redesign favor tracking for extensibility
- Update blessing system for custom domains
- Network layer and command support
- Custom domain CRUD operations enabled

### Phase 3: UI & Polish (Domain Creation Experience)
- GUI for domain creation and management
- Custom blessing creation UI
- Icon upload and management
- Localization support

---

## Phase 1: Foundation (Domain Registry & Data Models)

### 1.1 Create Domain Data Model

**File:** `DivineAscension/Data/DomainData.cs` (NEW)

```csharp
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Represents a deity domain (built-in or custom).
/// </summary>
[ProtoContract]
public record DomainData
{
    /// <summary>
    /// Unique domain identifier. Built-in: "builtin_craft", "builtin_wild", etc.
    /// Custom: GUID format.
    /// </summary>
    [ProtoMember(1)]
    public string DomainId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the domain (e.g., "Craft", "My Custom Domain")
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the domain's focus and philosophy
    /// </summary>
    [ProtoMember(3)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Icon path in assets (e.g., "divineascension:textures/icons/domains/custom_xyz.png")
    /// </summary>
    [ProtoMember(4)]
    public string IconPath { get; init; } = string.Empty;

    /// <summary>
    /// RGBA color for UI display (format: "r,g,b,a" e.g., "0.8,0.2,0.2,1.0")
    /// </summary>
    [ProtoMember(5)]
    public string ColorRGBA { get; init; } = "0.5,0.5,0.5,1.0"; // Default gray

    /// <summary>
    /// True for built-in domains (Craft, Wild, etc.), false for player-created
    /// </summary>
    [ProtoMember(6)]
    public bool IsBuiltIn { get; init; } = false;

    /// <summary>
    /// UID of the player who created this domain (empty for built-in)
    /// </summary>
    [ProtoMember(7)]
    public string CreatorUID { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp of domain creation (UTC ticks)
    /// </summary>
    [ProtoMember(8)]
    public long CreatedAtTicks { get; init; } = 0;

    /// <summary>
    /// Favor tracking configuration (serialized JSON)
    /// </summary>
    [ProtoMember(9)]
    public string FavorConfigJson { get; init; } = string.Empty;

    /// <summary>
    /// Data version for migrations
    /// </summary>
    [ProtoMember(10)]
    public int DataVersion { get; init; } = 1;

    public DomainData() { }

    public DomainData(string domainId, string name, string description,
        string iconPath, string colorRGBA, bool isBuiltIn, string creatorUID)
    {
        DomainId = domainId;
        Name = name;
        Description = description;
        IconPath = iconPath;
        ColorRGBA = colorRGBA;
        IsBuiltIn = isBuiltIn;
        CreatorUID = creatorUID;
        CreatedAtTicks = DateTime.UtcNow.Ticks;
        DataVersion = 1;
    }
}
```

**File:** `DivineAscension/Data/DomainWorldData.cs` (NEW)

```csharp
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// World save data for all domains (built-in + custom)
/// </summary>
[ProtoContract]
public class DomainWorldData
{
    [ProtoMember(1)]
    public Dictionary<string, DomainData> Domains { get; set; } = new();

    [ProtoMember(2)]
    public int DataVersion { get; set; } = 1;
}
```

**File:** `DivineAscension/Models/FavorTrackingConfig.cs` (NEW)

```csharp
namespace DivineAscension.Models;

/// <summary>
/// Configuration for which activities award favor for a domain
/// </summary>
public class FavorTrackingConfig
{
    /// <summary>
    /// Map of activity type to favor multiplier (e.g., "mining" -> 1.0, "crafting" -> 0.5)
    /// </summary>
    public Dictionary<string, float> ActivityMultipliers { get; set; } = new();

    /// <summary>
    /// Passive favor generation rate (favor per hour)
    /// </summary>
    public float PassiveFavorRate { get; set; } = 0.5f;

    /// <summary>
    /// Supported activity types (for validation and UI)
    /// </summary>
    public static readonly string[] SupportedActivities =
    {
        "mining", "smithing", "smelting",        // Craft-like
        "hunting", "skinning", "foraging",       // Wild-like
        "harvesting", "planting", "cooking",     // Harvest-like
        "stonework", "construction", "pottery",  // Stone-like
        "combat", "exploration"                  // Conquest-like
    };

    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }

    public static FavorTrackingConfig FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new FavorTrackingConfig();

        return System.Text.Json.JsonSerializer.Deserialize<FavorTrackingConfig>(json)
            ?? new FavorTrackingConfig();
    }
}
```

### 1.2 Create Domain Registry

**File:** `DivineAscension/Systems/DomainRegistry.cs` (NEW)

```csharp
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using System.Collections.Concurrent;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Central registry for all domains (built-in and custom).
/// Manages domain CRUD operations and provides lookup services.
/// </summary>
public class DomainRegistry : IDomainRegistry
{
    private readonly ICoreServerAPI _sapi;
    private readonly ConcurrentDictionary<string, DomainData> _domains = new();
    private readonly object _lock = new();

    // Built-in domain ID constants
    public const string BUILTIN_CRAFT = "builtin_craft";
    public const string BUILTIN_WILD = "builtin_wild";
    public const string BUILTIN_CONQUEST = "builtin_conquest";
    public const string BUILTIN_HARVEST = "builtin_harvest";
    public const string BUILTIN_STONE = "builtin_stone";

    public DomainRegistry(ICoreServerAPI sapi)
    {
        _sapi = sapi;
    }

    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Domain Registry...");
        RegisterBuiltInDomains();
        LoadCustomDomains();
        _sapi.Logger.Notification($"[DivineAscension] Domain Registry initialized with {_domains.Count} domains");
    }

    /// <summary>
    /// Registers the five built-in domains with fixed IDs
    /// </summary>
    private void RegisterBuiltInDomains()
    {
        RegisterBuiltIn(BUILTIN_CRAFT, "Craft", "The domain of creation and craftsmanship",
            "craft.png", "0.8,0.2,0.2,1.0", CreateCraftConfig());

        RegisterBuiltIn(BUILTIN_WILD, "Wild", "The domain of nature and the hunt",
            "wild.png", "0.4,0.8,0.3,1.0", CreateWildConfig());

        RegisterBuiltIn(BUILTIN_CONQUEST, "Conquest", "The domain of war and glory",
            "conquest.png", "0.6,0.1,0.3,1.0", CreateConquestConfig());

        RegisterBuiltIn(BUILTIN_HARVEST, "Harvest", "The domain of agriculture and sustenance",
            "harvest.png", "0.9,0.9,0.6,1.0", CreateHarvestConfig());

        RegisterBuiltIn(BUILTIN_STONE, "Stone", "The domain of earth and construction",
            "stone.png", "0.5,0.4,0.2,1.0", CreateStoneConfig());
    }

    private void RegisterBuiltIn(string id, string name, string desc,
        string icon, string color, FavorTrackingConfig config)
    {
        var domain = new DomainData(
            domainId: id,
            name: name,
            description: desc,
            iconPath: $"divineascension:textures/icons/domains/{icon}",
            colorRGBA: color,
            isBuiltIn: true,
            creatorUID: ""
        ) with { FavorConfigJson = config.ToJson() };

        _domains[id] = domain;
    }

    // Create favor configs for built-in domains...
    private FavorTrackingConfig CreateCraftConfig() => new()
    {
        ActivityMultipliers = new()
        {
            ["mining"] = 1.0f,
            ["smithing"] = 1.0f,
            ["smelting"] = 1.0f
        }
    };

    private FavorTrackingConfig CreateWildConfig() => new()
    {
        ActivityMultipliers = new()
        {
            ["hunting"] = 1.0f,
            ["skinning"] = 0.5f,
            ["foraging"] = 1.0f
        }
    };

    private FavorTrackingConfig CreateConquestConfig() => new()
    {
        ActivityMultipliers = new()
        {
            ["combat"] = 1.0f,
            ["exploration"] = 1.0f
        }
    };

    private FavorTrackingConfig CreateHarvestConfig() => new()
    {
        ActivityMultipliers = new()
        {
            ["harvesting"] = 1.0f,
            ["planting"] = 0.5f,
            ["cooking"] = 0.5f
        }
    };

    private FavorTrackingConfig CreateStoneConfig() => new()
    {
        ActivityMultipliers = new()
        {
            ["stonework"] = 1.0f,
            ["construction"] = 1.0f,
            ["pottery"] = 0.5f
        }
    };

    public DomainData? GetDomain(string domainId)
    {
        _domains.TryGetValue(domainId, out var domain);
        return domain;
    }

    public IEnumerable<DomainData> GetAllDomains() => _domains.Values;

    public IEnumerable<DomainData> GetCustomDomains() =>
        _domains.Values.Where(d => !d.IsBuiltIn);

    public IEnumerable<DomainData> GetBuiltInDomains() =>
        _domains.Values.Where(d => d.IsBuiltIn);

    /// <summary>
    /// Convert old enum to domain ID (migration helper)
    /// </summary>
    public static string EnumToDomainId(DeityDomain domain)
    {
        return domain switch
        {
            DeityDomain.Craft => BUILTIN_CRAFT,
            DeityDomain.Wild => BUILTIN_WILD,
            DeityDomain.Conquest => BUILTIN_CONQUEST,
            DeityDomain.Harvest => BUILTIN_HARVEST,
            DeityDomain.Stone => BUILTIN_STONE,
            _ => throw new ArgumentException($"Unknown domain: {domain}")
        };
    }

    /// <summary>
    /// Convert domain ID to enum (backward compatibility)
    /// </summary>
    public static DeityDomain DomainIdToEnum(string domainId)
    {
        return domainId switch
        {
            BUILTIN_CRAFT => DeityDomain.Craft,
            BUILTIN_WILD => DeityDomain.Wild,
            BUILTIN_CONQUEST => DeityDomain.Conquest,
            BUILTIN_HARVEST => DeityDomain.Harvest,
            BUILTIN_STONE => DeityDomain.Stone,
            _ => DeityDomain.None  // Custom domains map to None
        };
    }

    // Persistence methods
    private void LoadCustomDomains()
    {
        lock (_lock)
        {
            var data = _sapi.WorldManager.SaveGame.GetData("divineascension:domains");
            if (data == null) return;

            using var stream = new MemoryStream(data);
            var worldData = ProtoBuf.Serializer.Deserialize<DomainWorldData>(stream);

            foreach (var domain in worldData.Domains.Values.Where(d => !d.IsBuiltIn))
            {
                _domains[domain.DomainId] = domain;
            }

            _sapi.Logger.Notification($"[DivineAscension] Loaded {worldData.Domains.Count} custom domains");
        }
    }

    public void SaveDomains()
    {
        lock (_lock)
        {
            var worldData = new DomainWorldData
            {
                Domains = _domains.Where(kvp => !kvp.Value.IsBuiltIn)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                DataVersion = 1
            };

            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, worldData);
            _sapi.WorldManager.SaveGame.StoreData("divineascension:domains", stream.ToArray());
        }
    }

    // CRUD methods for custom domains (Phase 2)
    public DomainData? CreateCustomDomain(string name, string description, string creatorUID)
    {
        // TODO: Implement in Phase 2
        throw new NotImplementedException();
    }

    public bool DeleteCustomDomain(string domainId, string requestorUID)
    {
        // TODO: Implement in Phase 2
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        SaveDomains();
    }
}
```

**Interface:** `DivineAscension/Systems/Interfaces/IDomainRegistry.cs` (NEW)

```csharp
using DivineAscension.Data;

namespace DivineAscension.Systems.Interfaces;

public interface IDomainRegistry
{
    void Initialize();
    DomainData? GetDomain(string domainId);
    IEnumerable<DomainData> GetAllDomains();
    IEnumerable<DomainData> GetCustomDomains();
    IEnumerable<DomainData> GetBuiltInDomains();
    void SaveDomains();
    void Dispose();
}
```

### 1.3 Update ReligionData for Domain ID Migration

**File:** `DivineAscension/Data/ReligionData.cs`

Add new field (keep existing `Domain` for backward compatibility):

```csharp
/// <summary>
/// Domain ID (new system). When migrated, replaces Domain enum.
/// </summary>
[ProtoMember(19)]
public string DomainId { get; set; } = string.Empty;

/// <summary>
/// Gets the effective domain ID (prefers DomainId, falls back to enum conversion)
/// </summary>
public string GetDomainId()
{
    if (!string.IsNullOrEmpty(DomainId))
        return DomainId;

    // Fallback: convert enum to ID for migration
    return DomainRegistry.EnumToDomainId(Domain);
}
```

### 1.4 Update Initialization Order

**File:** `DivineAscension/Systems/DivineAscensionSystemInitializer.cs`

```csharp
public static void InitializeServerSystems(ICoreServerAPI sapi, /* ... */)
{
    // NEW: Initialize DomainRegistry FIRST (before ReligionManager)
    var domainRegistry = new DomainRegistry(sapi);
    domainRegistry.Initialize();

    sapi.Event.SaveGameLoaded += domainRegistry.LoadCustomDomains;
    sapi.Event.GameWorldSave += domainRegistry.SaveDomains;

    // Existing initialization continues...
    var religionManager = new ReligionManager(/* pass domainRegistry */);
    // ...
}
```

### 1.5 Data Migration Strategy

**File:** `DivineAscension/Systems/Migrations/DomainMigration.cs` (NEW)

```csharp
namespace DivineAscension.Systems.Migrations;

/// <summary>
/// Migrates religions from DeityDomain enum to DomainId string
/// </summary>
public static class DomainMigration
{
    public static void MigrateReligions(IEnumerable<ReligionData> religions)
    {
        foreach (var religion in religions)
        {
            if (string.IsNullOrEmpty(religion.DomainId) && religion.Domain != DeityDomain.None)
            {
                religion.DomainId = DomainRegistry.EnumToDomainId(religion.Domain);
                // Note: Don't clear Domain field yet (keep for rollback safety)
            }
        }
    }
}
```

Call this in `ReligionManager` after loading religions from save data.

---

## Phase 2: System Integration (Favor & Blessings)

### 2.1 Redesign Favor Tracking System

**Current Problem:** 10 hardcoded tracker classes, one per built-in domain

**Solution:** Generic, configurable favor tracker

**File:** `DivineAscension/Systems/Favor/GenericFavorTracker.cs` (NEW)

```csharp
using DivineAscension.Models;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Favor;

/// <summary>
/// Generic favor tracker that awards favor based on domain configuration
/// </summary>
public class GenericFavorTracker
{
    private readonly IPlayerProgressionDataManager _progressionManager;
    private readonly ICoreServerAPI _sapi;
    private readonly IFavorSystem _favorSystem;
    private readonly IDomainRegistry _domainRegistry;

    // Cache of domain ID -> followers
    private readonly ConcurrentDictionary<string, HashSet<string>> _domainFollowers = new();

    public GenericFavorTracker(IPlayerProgressionDataManager progressionManager,
        ICoreServerAPI sapi, IFavorSystem favorSystem, IDomainRegistry domainRegistry)
    {
        _progressionManager = progressionManager;
        _sapi = sapi;
        _favorSystem = favorSystem;
        _domainRegistry = domainRegistry;
    }

    public void Initialize()
    {
        RefreshFollowerCaches();
        _progressionManager.OnPlayerDataChanged += RefreshFollowerCaches;

        // Subscribe to all game events
        SubscribeToMiningEvents();
        SubscribeToSmithingEvents();
        SubscribeToHuntingEvents();
        // ... etc for all activity types
    }

    private void RefreshFollowerCaches()
    {
        _domainFollowers.Clear();

        foreach (var domain in _domainRegistry.GetAllDomains())
        {
            _domainFollowers[domain.DomainId] = new HashSet<string>();
        }

        foreach (var playerData in _progressionManager.GetAllPlayerData())
        {
            var domainId = playerData.GetDomainId(); // Updated method
            if (!string.IsNullOrEmpty(domainId))
            {
                if (_domainFollowers.TryGetValue(domainId, out var followers))
                {
                    followers.Add(playerData.PlayerUID);
                }
            }
        }
    }

    /// <summary>
    /// Awards favor for an activity to all followers of domains that track this activity
    /// </summary>
    public void AwardFavorForActivity(string playerUID, string activityType, float baseFavor)
    {
        var playerDomainId = GetPlayerDomainId(playerUID);
        if (string.IsNullOrEmpty(playerDomainId)) return;

        var domain = _domainRegistry.GetDomain(playerDomainId);
        if (domain == null) return;

        var config = FavorTrackingConfig.FromJson(domain.FavorConfigJson);

        if (config.ActivityMultipliers.TryGetValue(activityType, out var multiplier))
        {
            var favorAmount = baseFavor * multiplier;
            _favorSystem.AwardFavor(playerUID, favorAmount, $"{activityType} activity");
        }
    }

    private void SubscribeToMiningEvents()
    {
        // Subscribe to mining patches
        MiningPatches.OnOreMined += (playerUID, ore, tier) =>
        {
            AwardFavorForActivity(playerUID, "mining", tier * 1.0f);
        };
    }

    // Similar for other activity types...
}
```

**Migration Note:** Keep existing 10 trackers in Phase 2 for stability. In Phase 3, gradually replace with `GenericFavorTracker`.

### 2.2 Update Blessing System

**File:** `DivineAscension/Models/Blessing.cs`

```csharp
// Add new field
public string DomainId { get; set; } = string.Empty;

// Keep old field for backward compatibility
[Obsolete("Use DomainId instead")]
public DeityDomain Domain { get; set; } = DeityDomain.None;

// Helper method
public string GetDomainId()
{
    if (!string.IsNullOrEmpty(DomainId))
        return DomainId;

    return DomainRegistry.EnumToDomainId(Domain);
}
```

**File:** `DivineAscension/Services/BlessingLoader.cs`

Update to support both enum-based and ID-based blessings:

```csharp
private Blessing ConvertToBlessing(BlessingJsonDto dto, string fileSource)
{
    var blessing = new Blessing
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        // ...
    };

    // Try to parse as domain ID first (custom domains)
    var domainData = _domainRegistry.GetDomain(fileSource);
    if (domainData != null)
    {
        blessing.DomainId = domainData.DomainId;
    }
    else
    {
        // Fallback: parse as enum (built-in domains)
        if (Enum.TryParse<DeityDomain>(fileSource, true, out var domain))
        {
            blessing.DomainId = DomainRegistry.EnumToDomainId(domain);
            blessing.Domain = domain; // Backward compatibility
        }
    }

    return blessing;
}
```

### 2.3 Custom Domain CRUD Implementation

**Complete `DomainRegistry.CreateCustomDomain`:**

```csharp
public DomainData? CreateCustomDomain(string name, string description,
    string creatorUID, string colorRGBA, FavorTrackingConfig favorConfig)
{
    lock (_lock)
    {
        // Validate name uniqueness (case-insensitive)
        if (_domains.Values.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            _sapi.Logger.Warning($"[DivineAscension] Domain name '{name}' already exists");
            return null;
        }

        // Validate name length
        if (name.Length < 3 || name.Length > 50)
        {
            _sapi.Logger.Warning("[DivineAscension] Domain name must be 3-50 characters");
            return null;
        }

        // Profanity filter
        if (_profanityFilter.ContainsProfanity(name) || _profanityFilter.ContainsProfanity(description))
        {
            _sapi.Logger.Warning("[DivineAscension] Domain contains inappropriate content");
            return null;
        }

        var domainId = $"custom_{Guid.NewGuid():N}";
        var domain = new DomainData(
            domainId: domainId,
            name: name,
            description: description,
            iconPath: $"divineascension:textures/icons/domains/{domainId}.png", // Placeholder
            colorRGBA: colorRGBA,
            isBuiltIn: false,
            creatorUID: creatorUID
        ) with { FavorConfigJson = favorConfig.ToJson() };

        _domains[domainId] = domain;
        SaveDomains();

        _sapi.Logger.Notification($"[DivineAscension] Created custom domain '{name}' (ID: {domainId})");
        return domain;
    }
}

public bool DeleteCustomDomain(string domainId, string requestorUID)
{
    lock (_lock)
    {
        if (!_domains.TryGetValue(domainId, out var domain))
            return false;

        // Cannot delete built-in domains
        if (domain.IsBuiltIn)
        {
            _sapi.Logger.Warning("[DivineAscension] Cannot delete built-in domain");
            return false;
        }

        // Permission check
        if (domain.CreatorUID != requestorUID)
        {
            _sapi.Logger.Warning("[DivineAscension] Only domain creator can delete domain");
            return false;
        }

        // Check for active religions using this domain
        var religionsUsingDomain = _religionManager.GetAllReligions()
            .Where(r => r.GetDomainId() == domainId)
            .ToList();

        if (religionsUsingDomain.Any())
        {
            _sapi.Logger.Warning($"[DivineAscension] Cannot delete domain - {religionsUsingDomain.Count} religions use it");
            return false;
        }

        _domains.TryRemove(domainId, out _);
        SaveDomains();

        _sapi.Logger.Notification($"[DivineAscension] Deleted custom domain '{domain.Name}'");
        return true;
    }
}
```

### 2.4 Network Layer for Custom Domains

**File:** `DivineAscension/Network/Domain/CreateDomainRequestPacket.cs` (NEW)

```csharp
using ProtoBuf;

namespace DivineAscension.Network.Domain;

[ProtoContract]
public class CreateDomainRequestPacket
{
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Description { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string ColorRGBA { get; set; } = "0.5,0.5,0.5,1.0";

    [ProtoMember(4)]
    public Dictionary<string, float> ActivityMultipliers { get; set; } = new();

    [ProtoMember(5)]
    public float PassiveFavorRate { get; set; } = 0.5f;
}

[ProtoContract]
public class CreateDomainResponsePacket
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string DomainId { get; set; } = string.Empty;
}
```

**Similar packets for:**
- `DeleteDomainRequestPacket` / `DeleteDomainResponsePacket`
- `ListDomainsRequestPacket` / `ListDomainsResponsePacket`
- `DomainDetailRequestPacket` / `DomainDetailResponsePacket`

**File:** `DivineAscension/Systems/Networking/Server/DomainNetworkHandler.cs` (NEW)

```csharp
using DivineAscension.Network.Domain;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

public class DomainNetworkHandler
{
    private readonly ICoreServerAPI _sapi;
    private readonly IDomainRegistry _domainRegistry;
    private const string CHANNEL = "divineascension";

    public DomainNetworkHandler(ICoreServerAPI sapi, IDomainRegistry domainRegistry)
    {
        _sapi = sapi;
        _domainRegistry = domainRegistry;
    }

    public void Initialize()
    {
        _sapi.Network.RegisterChannel(CHANNEL)
            .RegisterMessageType<CreateDomainRequestPacket>()
            .RegisterMessageType<CreateDomainResponsePacket>()
            .RegisterMessageType<DeleteDomainRequestPacket>()
            .RegisterMessageType<DeleteDomainResponsePacket>()
            .RegisterMessageType<ListDomainsRequestPacket>()
            .RegisterMessageType<ListDomainsResponsePacket>()
            .SetMessageHandler<CreateDomainRequestPacket>(OnCreateDomainRequest)
            .SetMessageHandler<DeleteDomainRequestPacket>(OnDeleteDomainRequest)
            .SetMessageHandler<ListDomainsRequestPacket>(OnListDomainsRequest);

        _sapi.Logger.Notification("[DivineAscension] Domain network handler initialized");
    }

    private void OnCreateDomainRequest(IServerPlayer player, CreateDomainRequestPacket packet)
    {
        var favorConfig = new FavorTrackingConfig
        {
            ActivityMultipliers = packet.ActivityMultipliers,
            PassiveFavorRate = packet.PassiveFavorRate
        };

        var domain = _domainRegistry.CreateCustomDomain(
            packet.Name,
            packet.Description,
            player.PlayerUID,
            packet.ColorRGBA,
            favorConfig
        );

        var response = new CreateDomainResponsePacket();
        if (domain != null)
        {
            response.Success = true;
            response.Message = $"Created domain '{domain.Name}'";
            response.DomainId = domain.DomainId;
        }
        else
        {
            response.Success = false;
            response.Message = "Failed to create domain (name may be taken or invalid)";
        }

        _sapi.Network.GetChannel(CHANNEL).SendPacket(response, player);
    }

    private void OnDeleteDomainRequest(IServerPlayer player, DeleteDomainRequestPacket packet)
    {
        var success = _domainRegistry.DeleteCustomDomain(packet.DomainId, player.PlayerUID);

        var response = new DeleteDomainResponsePacket
        {
            Success = success,
            Message = success
                ? "Domain deleted successfully"
                : "Failed to delete domain (may be in use or you lack permission)"
        };

        _sapi.Network.GetChannel(CHANNEL).SendPacket(response, player);
    }

    private void OnListDomainsRequest(IServerPlayer player, ListDomainsRequestPacket packet)
    {
        var domains = _domainRegistry.GetAllDomains().Select(d => new DomainListItemDto
        {
            DomainId = d.DomainId,
            Name = d.Name,
            Description = d.Description,
            ColorRGBA = d.ColorRGBA,
            IsBuiltIn = d.IsBuiltIn,
            CreatorUID = d.CreatorUID
        }).ToList();

        var response = new ListDomainsResponsePacket
        {
            Domains = domains
        };

        _sapi.Network.GetChannel(CHANNEL).SendPacket(response, player);
    }
}
```

### 2.5 Command Support

**File:** `DivineAscension/Commands/DomainCommands.cs` (NEW)

```csharp
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

public class DomainCommands
{
    private readonly ICoreServerAPI _sapi;
    private readonly IDomainRegistry _domainRegistry;

    public DomainCommands(ICoreServerAPI sapi, IDomainRegistry domainRegistry)
    {
        _sapi = sapi;
        _domainRegistry = domainRegistry;
    }

    public void RegisterCommands()
    {
        _sapi.ChatCommands
            .GetOrCreate("da")
            .BeginSubCommand("domain")
                .WithDescription("Manage custom domains")

                .BeginSubCommand("list")
                    .WithDescription("List all available domains")
                    .HandleWith(OnListDomains)
                .EndSubCommand()

                .BeginSubCommand("info")
                    .WithArgs(_sapi.ChatCommands.Parsers.Word("domain_name"))
                    .WithDescription("View details about a domain")
                    .HandleWith(OnDomainInfo)
                .EndSubCommand()

                .BeginSubCommand("delete")
                    .WithArgs(_sapi.ChatCommands.Parsers.Word("domain_name"))
                    .WithDescription("Delete your custom domain")
                    .HandleWith(OnDeleteDomain)
                .EndSubCommand()

            .EndSubCommand();
    }

    private TextCommandResult OnListDomains(TextCommandCallingArgs args)
    {
        var domains = _domainRegistry.GetAllDomains().ToList();

        var builtIn = domains.Where(d => d.IsBuiltIn).ToList();
        var custom = domains.Where(d => !d.IsBuiltIn).ToList();

        var response = new StringBuilder();
        response.AppendLine($"=== Built-In Domains ({builtIn.Count}) ===");
        foreach (var domain in builtIn)
        {
            response.AppendLine($"  {domain.Name}: {domain.Description}");
        }

        response.AppendLine($"\n=== Custom Domains ({custom.Count}) ===");
        foreach (var domain in custom)
        {
            response.AppendLine($"  {domain.Name}: {domain.Description}");
        }

        return TextCommandResult.Success(response.ToString());
    }

    private TextCommandResult OnDomainInfo(TextCommandCallingArgs args)
    {
        var domainName = args.Parsers[0].GetValue() as string;
        var domain = _domainRegistry.GetAllDomains()
            .FirstOrDefault(d => d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));

        if (domain == null)
            return TextCommandResult.Error($"Domain '{domainName}' not found");

        var config = FavorTrackingConfig.FromJson(domain.FavorConfigJson);

        var response = new StringBuilder();
        response.AppendLine($"=== {domain.Name} ===");
        response.AppendLine($"Description: {domain.Description}");
        response.AppendLine($"Type: {(domain.IsBuiltIn ? "Built-In" : "Custom")}");
        response.AppendLine($"Passive Favor: {config.PassiveFavorRate}/hour");
        response.AppendLine("\nActivities:");
        foreach (var activity in config.ActivityMultipliers)
        {
            response.AppendLine($"  {activity.Key}: {activity.Value}x multiplier");
        }

        return TextCommandResult.Success(response.ToString());
    }

    private TextCommandResult OnDeleteDomain(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        var domainName = args.Parsers[0].GetValue() as string;
        var domain = _domainRegistry.GetAllDomains()
            .FirstOrDefault(d => d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));

        if (domain == null)
            return TextCommandResult.Error($"Domain '{domainName}' not found");

        var success = _domainRegistry.DeleteCustomDomain(domain.DomainId, player.PlayerUID);

        return success
            ? TextCommandResult.Success($"Deleted domain '{domain.Name}'")
            : TextCommandResult.Error("Failed to delete domain (may be in use or you lack permission)");
    }
}
```

---

## Phase 3: UI & Polish (Domain Creation Experience)

### 3.1 GUI Domain Tab

**File:** `DivineAscension/GUI/State/DomainTabState.cs` (NEW)

```csharp
namespace DivineAscension.GUI.State;

public class DomainTabState
{
    public DomainSubTab CurrentSubTab { get; set; } = DomainSubTab.Browse;

    // Browse state
    public List<DomainListItemVM> AvailableDomains { get; set; } = new();
    public string? SelectedDomainId { get; set; }

    // Create state
    public string CreateName { get; set; } = string.Empty;
    public string CreateDescription { get; set; } = string.Empty;
    public Vector4 CreateColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);
    public Dictionary<string, bool> CreateActivityToggles { get; set; } = new();
    public string? CreateValidationError { get; set; }
}

public enum DomainSubTab
{
    Browse,
    Create,
    MyDomains
}
```

**File:** `DivineAscension/GUI/UI/Renderers/Domain/DomainCreateRenderer.cs` (NEW)

```csharp
using ImGuiNET;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Renderers.Domain;

public static class DomainCreateRenderer
{
    public static void Render(DomainTabState state)
    {
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), "Create Custom Domain");
        ImGui.Separator();

        // Name input
        ImGui.Text("Domain Name:");
        var name = state.CreateName;
        if (ImGui.InputText("##domainName", ref name, 50))
        {
            state.CreateName = name;
        }

        // Description input
        ImGui.Text("Description:");
        var desc = state.CreateDescription;
        if (ImGui.InputTextMultiline("##domainDesc", ref desc, 500, new Vector2(-1, 80)))
        {
            state.CreateDescription = desc;
        }

        // Color picker
        ImGui.Text("Domain Color:");
        var color = state.CreateColor;
        if (ImGui.ColorEdit4("##domainColor", ref color))
        {
            state.CreateColor = color;
        }

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.7f, 0.9f, 1f, 1f), "Favor-Granting Activities");
        ImGui.Text("Select which activities award favor for this domain:");

        // Activity checkboxes
        var activities = FavorTrackingConfig.SupportedActivities;
        foreach (var activity in activities)
        {
            if (!state.CreateActivityToggles.ContainsKey(activity))
                state.CreateActivityToggles[activity] = false;

            var enabled = state.CreateActivityToggles[activity];
            if (ImGui.Checkbox($"{activity}##activity_{activity}", ref enabled))
            {
                state.CreateActivityToggles[activity] = enabled;
            }
        }

        ImGui.Separator();

        // Validation error
        if (!string.IsNullOrEmpty(state.CreateValidationError))
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), state.CreateValidationError);
        }

        // Create button
        var canCreate = !string.IsNullOrEmpty(state.CreateName) &&
                       state.CreateName.Length >= 3 &&
                       state.CreateActivityToggles.Any(kvp => kvp.Value);

        if (!canCreate) ImGui.BeginDisabled();

        if (ImGui.Button("Create Domain", new Vector2(200, 30)))
        {
            // Emit create event
            GuiEvents.EmitDomainCreateClicked(state);
        }

        if (!canCreate) ImGui.EndDisabled();

        if (!canCreate)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f),
                "(Name must be 3+ chars, select at least one activity)");
        }
    }
}
```

### 3.2 Icon Upload System

**File:** `DivineAscension/Systems/IconManager.cs` (NEW)

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Manages custom icon uploads for domains
/// </summary>
public class IconManager
{
    private readonly ICoreServerAPI _sapi;
    private const int MAX_ICON_SIZE_BYTES = 512 * 1024; // 512 KB
    private const int TARGET_ICON_SIZE_PX = 64;

    public IconManager(ICoreServerAPI sapi)
    {
        _sapi = sapi;
    }

    public (bool success, string message) UploadDomainIcon(string domainId, byte[] imageData)
    {
        // Validate file size
        if (imageData.Length > MAX_ICON_SIZE_BYTES)
        {
            return (false, $"Icon too large (max {MAX_ICON_SIZE_BYTES / 1024} KB)");
        }

        try
        {
            // Validate is valid PNG
            using var ms = new MemoryStream(imageData);
            using var image = Image.FromStream(ms);

            if (image.RawFormat.Guid != ImageFormat.Png.Guid)
            {
                return (false, "Icon must be PNG format");
            }

            // Resize to standard size
            var resized = ResizeImage(image, TARGET_ICON_SIZE_PX, TARGET_ICON_SIZE_PX);

            // Save to mod assets directory
            var iconPath = GetIconPath(domainId);
            resized.Save(iconPath, ImageFormat.Png);

            return (true, "Icon uploaded successfully");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to upload icon: {ex.Message}");
            return (false, "Invalid image file");
        }
    }

    private Image ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using var graphics = Graphics.FromImage(destImage);
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        using var wrapMode = new System.Drawing.Imaging.ImageAttributes();
        wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

        return destImage;
    }

    private string GetIconPath(string domainId)
    {
        var modDataPath = _sapi.GetOrCreateDataPath("ModData/DivineAscension/icons/domains");
        return Path.Combine(modDataPath, $"{domainId}.png");
    }
}
```

### 3.3 Custom Blessing Creation

**File:** `DivineAscension/GUI/UI/Renderers/Blessing/CustomBlessingCreateRenderer.cs` (NEW)

This renderer allows players to:
1. Select their custom domain
2. Define blessing name, description
3. Choose blessing type (player/religion)
4. Configure stat modifiers (dropdown of available stats + value input)
5. Set unlock requirements (rank threshold)
6. Preview blessing card

Implementation follows similar pattern to `DomainCreateRenderer` with stat modifier configuration UI.

### 3.4 Localization Support

**File:** `DivineAscension/Services/LocalizationService.cs`

Extend to support runtime-registered translations for custom domains:

```csharp
public void RegisterCustomDomainTranslation(string domainId, string languageCode, string name, string description)
{
    var key = $"domain.{domainId}.name";
    _translations[$"{languageCode}:{key}"] = name;

    var descKey = $"domain.{domainId}.description";
    _translations[$"{languageCode}:{descKey}"] = description;
}
```

**File:** `assets/divineascension/lang/en.json`

Add new localization keys:

```json
{
  "domain-create-title": "Create Custom Domain",
  "domain-create-name-label": "Domain Name",
  "domain-create-description-label": "Description",
  "domain-create-color-label": "Domain Color",
  "domain-create-activities-label": "Favor-Granting Activities",
  "domain-create-success": "Custom domain created successfully!",
  "domain-create-error-name-taken": "A domain with this name already exists",
  "domain-create-error-invalid-name": "Domain name must be 3-50 characters",
  "domain-create-error-no-activities": "Select at least one activity",
  "domain-create-error-profanity": "Domain name or description contains inappropriate content",
  "domain-delete-success": "Domain deleted successfully",
  "domain-delete-error-in-use": "Cannot delete domain - {0} religions are using it",
  "domain-delete-error-permission": "Only the domain creator can delete it"
}
```

---

## Implementation Order & Timeline

| Phase | Components | Estimated Effort | Priority |
|-------|-----------|------------------|----------|
| **Phase 1: Foundation** | Domain data models, registry, migration | 2-3 weeks | Critical |
| **Phase 2: Integration** | Favor tracking, blessings, networking, commands | 3-4 weeks | Critical |
| **Phase 3: UI & Polish** | GUI creation, icon upload, custom blessings | 2-3 weeks | High |
| **Testing & Documentation** | Comprehensive testing, docs updates | 1-2 weeks | Critical |

**Total estimated effort:** 8-12 weeks (phased rollout reduces risk)

---

## Testing Strategy

### Unit Tests

**New Test Files:**

1. `DivineAscension.Tests/Systems/DomainRegistryTests.cs`
   - Test built-in domain registration
   - Test custom domain CRUD
   - Test domain ID conversion (enum ↔ ID)
   - Test persistence

2. `DivineAscension.Tests/Systems/GenericFavorTrackerTests.cs`
   - Test activity-based favor awarding
   - Test multiplier application
   - Test follower cache refresh

3. `DivineAscension.Tests/Migrations/DomainMigrationTests.cs`
   - Test religion migration from enum to ID
   - Test backward compatibility

4. `DivineAscension.Tests/Services/IconManagerTests.cs`
   - Test icon validation
   - Test resize functionality
   - Test file storage

### Integration Tests

1. **End-to-End Domain Creation:**
   - Player creates custom domain via GUI
   - Domain persists across server restart
   - Player founds religion with custom domain
   - Favor tracking works for configured activities

2. **Civilization Domain Diversity:**
   - Civilization with 1 built-in + 1 custom domain
   - Cannot add duplicate custom domains
   - Domain deletion blocked if used by civilization

3. **Migration Testing:**
   - Load world with old enum-based religions
   - Verify migration to domain IDs
   - Verify game functionality unchanged

### Performance Tests

1. **Domain Lookup Performance:**
   - Benchmark `GetDomain()` with 100+ custom domains
   - Verify O(1) lookup via dictionary

2. **Favor Tracking Overhead:**
   - Compare generic tracker vs hardcoded trackers
   - Ensure <5% performance degradation

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Enum coupling too tight** | Blocks migration | Medium | Phase 1 keeps enum, gradual replacement |
| **Favor tracking regression** | Game balance broken | Medium | Keep old trackers parallel in Phase 2, extensive testing |
| **Save data corruption** | Player data loss | Low | Backup migration, rollback safety with dual fields |
| **UI complexity** | Poor UX | Medium | Iterative design, user testing in Phase 3 |
| **Icon upload abuse** | Server storage issues | Low | File size limits, format validation, admin moderation |
| **Custom domain spam** | Database bloat | Medium | Limit domains per player (e.g., 3 max), cleanup tools |
| **Blessing balance issues** | Overpowered custom blessings | High | Stat modifier caps, admin review system |

---

## Backward Compatibility Strategy

1. **Dual Field Pattern:**
   - `ReligionData` has both `Domain` (enum) and `DomainId` (string)
   - `GetDomainId()` method prefers `DomainId`, falls back to enum conversion
   - Allows rollback if migration fails

2. **Migration Checkpoints:**
   - Phase 1: Add fields, no removal
   - Phase 2: Use new fields, read old fields
   - Phase 3+: Deprecate old fields (mark `[Obsolete]`)
   - Future: Remove enum entirely (v5.0.0+)

3. **Version Gating:**
   - Add `DataVersion` to `DomainWorldData` and `ReligionData.DomainId`
   - Migration code checks version, skips if already migrated
   - Log migration status for debugging

---

## Configuration Options

**File:** `DivineAscension/Configuration/DomainConfig.cs` (NEW)

```csharp
namespace DivineAscension.Configuration;

public class DomainConfig
{
    /// <summary>
    /// Maximum custom domains per player (0 = unlimited)
    /// </summary>
    public int MaxDomainsPerPlayer { get; set; } = 3;

    /// <summary>
    /// Allow players to create custom blessings for their domains
    /// </summary>
    public bool AllowCustomBlessings { get; set; } = true;

    /// <summary>
    /// Require admin approval for custom domains
    /// </summary>
    public bool RequireAdminApproval { get; set; } = false;

    /// <summary>
    /// Maximum file size for domain icons (bytes)
    /// </summary>
    public int MaxIconSizeBytes { get; set; } = 512 * 1024; // 512 KB
}
```

Admin commands:
- `/da config domain maxperplayer <number>` - Set domain limit
- `/da domain approve <domain_name>` - Approve pending domain
- `/da domain reject <domain_name>` - Reject domain creation

---

## Files to Modify Summary

### New Files (Phase 1)
| File | Purpose |
|------|---------|
| `DivineAscension/Data/DomainData.cs` | Domain data model |
| `DivineAscension/Data/DomainWorldData.cs` | Domain persistence |
| `DivineAscension/Models/FavorTrackingConfig.cs` | Favor configuration |
| `DivineAscension/Systems/DomainRegistry.cs` | Domain registry implementation |
| `DivineAscension/Systems/Interfaces/IDomainRegistry.cs` | Domain registry interface |
| `DivineAscension/Systems/Migrations/DomainMigration.cs` | Migration utilities |

### Modified Files (Phase 1)
| File | Changes |
|------|---------|
| `DivineAscension/Data/ReligionData.cs` | Add `DomainId` field, `GetDomainId()` method |
| `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` | Initialize `DomainRegistry` first |
| `DivineAscension/Models/Enum/DeityDomain.cs` | Mark as `[Obsolete]` in future phase |

### New Files (Phase 2)
| File | Purpose |
|------|---------|
| `DivineAscension/Systems/Favor/GenericFavorTracker.cs` | Generic favor tracker |
| `DivineAscension/Network/Domain/*.cs` | Domain CRUD packets (4 files) |
| `DivineAscension/Systems/Networking/Server/DomainNetworkHandler.cs` | Server-side domain handler |
| `DivineAscension/Commands/DomainCommands.cs` | Command support |
| `DivineAscension/Configuration/DomainConfig.cs` | Configuration options |

### Modified Files (Phase 2)
| File | Changes |
|------|---------|
| `DivineAscension/Models/Blessing.cs` | Add `DomainId` field, `GetDomainId()` method |
| `DivineAscension/Services/BlessingLoader.cs` | Support domain ID loading |
| `DivineAscension/Systems/BlessingRegistry.cs` | Query by domain ID |
| `DivineAscension/Systems/FavorSystem.cs` | Initialize `GenericFavorTracker` |
| `DivineAscension/GUI/UI/Utilities/DomainHelper.cs` | Support custom domain lookups |

### New Files (Phase 3)
| File | Purpose |
|------|---------|
| `DivineAscension/GUI/State/DomainTabState.cs` | Domain tab state |
| `DivineAscension/GUI/UI/Renderers/Domain/DomainCreateRenderer.cs` | Domain creation UI |
| `DivineAscension/GUI/UI/Renderers/Domain/DomainBrowseRenderer.cs` | Domain browsing UI |
| `DivineAscension/GUI/UI/Renderers/Blessing/CustomBlessingCreateRenderer.cs` | Custom blessing UI |
| `DivineAscension/Systems/IconManager.cs` | Icon upload management |
| `DivineAscension/GUI/UI/Utilities/DomainIconLoader.cs` | Icon loading |

### Modified Files (Phase 3)
| File | Changes |
|------|---------|
| `DivineAscension/GUI/GuiDialog.cs` | Add domain tab |
| `DivineAscension/GUI/GuiDialogManager.cs` | Manage domain tab state |
| `DivineAscension/Services/LocalizationService.cs` | Runtime translation registration |
| `assets/divineascension/lang/en.json` | Domain localization keys |

### Test Files (All Phases)
| File | Purpose |
|------|---------|
| `DivineAscension.Tests/Systems/DomainRegistryTests.cs` | Registry tests |
| `DivineAscension.Tests/Systems/GenericFavorTrackerTests.cs` | Favor tracking tests |
| `DivineAscension.Tests/Migrations/DomainMigrationTests.cs` | Migration tests |
| `DivineAscension.Tests/Services/IconManagerTests.cs` | Icon management tests |
| `DivineAscension.Tests/GUI/DomainTabStateTests.cs` | UI state tests |

**Total files:** ~30 new files, ~15 modified files

---

## Documentation Updates Required

1. **CLAUDE.md:**
   - Update initialization order (add `DomainRegistry`)
   - Document domain system architecture
   - Update favor tracker pattern description
   - Add custom domain feature overview

2. **PLAYER_GUIDE.md:**
   - Add "Creating Custom Domains" section
   - Add "Custom Blessings" section
   - Update FAQ with domain questions
   - Add examples of custom domain configurations

3. **API_REFERENCE.md (new):**
   - `IDomainRegistry` interface documentation
   - `FavorTrackingConfig` structure reference
   - Network packet specifications
   - Command reference

4. **MIGRATION_GUIDE.md (new):**
   - v4.x → v5.0 migration guide
   - Enum to domain ID conversion
   - Backup and rollback procedures

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Custom domains created** | 10+ per active server | Track registry size |
| **Religions using custom domains** | 25%+ of total religions | Query religion data |
| **Favor tracking accuracy** | 100% (no missed events) | Unit test coverage |
| **Migration success rate** | 100% (no data loss) | Pre/post migration validation |
| **Performance overhead** | <5% CPU increase | Profiling benchmarks |
| **User satisfaction** | 4.5/5 stars | Community feedback |

---

## Post-Launch Monitoring

1. **Data Integrity Checks:**
   - Daily validation of domain references
   - Orphaned religion detection (domain deleted but religions exist)
   - Duplicate domain name detection

2. **Performance Monitoring:**
   - Track domain lookup times
   - Monitor favor tracker CPU usage
   - Alert on >100 custom domains per server

3. **Content Moderation:**
   - Admin dashboard for reviewing custom domains
   - Automated profanity detection reports
   - Community flagging system

4. **Feature Usage Analytics:**
   - Track custom domain creation rate
   - Most popular activity configurations
   - Custom blessing adoption rate

---

## Future Enhancements (Post-v1.0)

1. **Domain Alliances:**
   - Allow domain creators to form alliances
   - Shared blessing pools
   - Cross-domain favor bonuses

2. **Domain Prestige:**
   - Track domain popularity (# of religions using it)
   - Award prestige to domain creators
   - Domain leaderboards

3. **Advanced Favor Configurations:**
   - Time-of-day multipliers (e.g., night hunting bonus)
   - Weather-based modifiers
   - Location-based bonuses (e.g., mining in specific biomes)

4. **Domain Events:**
   - Domain-wide challenges (e.g., "Mine 1000 ore this week")
   - Reward all followers on completion
   - Seasonal events

5. **Blessing Marketplace:**
   - Players share custom blessing designs
   - Import/export blessing configurations
   - Community rating system

---

## Approval Checklist

Before proceeding with implementation:

- [ ] Architectural approach approved by maintainers
- [ ] Phase 1 implementation plan reviewed
- [ ] Migration strategy validated with test world data
- [ ] UI/UX mockups approved
- [ ] Performance benchmarks established
- [ ] Backward compatibility confirmed
- [ ] Test coverage requirements defined
- [ ] Documentation plan approved
- [ ] Community feedback gathered (optional)

---

## Appendix: Built-In Domain Migration Mapping

| Enum Value | Domain ID | Preserve Data |
|------------|-----------|---------------|
| `DeityDomain.Craft` | `builtin_craft` | ✓ |
| `DeityDomain.Wild` | `builtin_wild` | ✓ |
| `DeityDomain.Conquest` | `builtin_conquest` | ✓ |
| `DeityDomain.Harvest` | `builtin_harvest` | ✓ |
| `DeityDomain.Stone` | `builtin_stone` | ✓ |
| `DeityDomain.None` | `""` (empty) | ✓ (indicates no domain) |

**Migration SQL (Conceptual):**
```sql
UPDATE ReligionData
SET DomainId =
  CASE Domain
    WHEN 1 THEN 'builtin_craft'
    WHEN 2 THEN 'builtin_wild'
    WHEN 3 THEN 'builtin_conquest'
    WHEN 4 THEN 'builtin_harvest'
    WHEN 7 THEN 'builtin_stone'
    ELSE ''
  END
WHERE DomainId IS NULL OR DomainId = '';
```

---

**Document Version:** 1.0
**Last Updated:** 2026-01-18
**Author:** Claude Code
**Status:** Draft - Awaiting Review
