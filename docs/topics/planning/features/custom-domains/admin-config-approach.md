# Admin-Only Config-Based Custom Domains

## Overview

This document outlines a **simplified approach** to custom domains where only server admins can create domains by adding JSON configuration files to `assets/divineascension/config/domains/`. This eliminates the need for player-facing UI, network layer, and CRUD operations.

## Comparison: Player-Created vs Admin-Only

| Aspect | Player-Created | Admin-Only Config |
|--------|----------------|-------------------|
| **Domain Creation** | GUI + Commands + Network | JSON file in assets |
| **Icon Management** | Upload system + validation | Static files in assets |
| **Blessing Creation** | Custom blessing UI | JSON file (same as built-in) |
| **Permission System** | Creator UID tracking | No tracking needed |
| **Network Layer** | 4 packet types + handler | None |
| **GUI Components** | 3 renderers + state | None |
| **Commands** | create/delete/list/info | list/info only |
| **Persistence** | World save data | Assets only |
| **Implementation Effort** | 8-12 weeks | **2-3 weeks** |
| **Files Modified** | ~45 files | **~15 files** |

---

## What Gets ELIMINATED

### ❌ Completely Removed Components

1. **Phase 3 (UI & Polish)** - Entire phase eliminated
   - No domain creation GUI
   - No custom blessing creation UI
   - No icon upload system
   - No runtime localization registration

2. **Network Layer** - No packets needed
   - `CreateDomainRequestPacket` / `CreateDomainResponsePacket`
   - `DeleteDomainRequestPacket` / `DeleteDomainResponsePacket`
   - `DomainNetworkHandler`
   - Client-side domain sync

3. **CRUD Operations**
   - `DomainRegistry.CreateCustomDomain()`
   - `DomainRegistry.DeleteCustomDomain()`
   - No profanity filtering needed
   - No name uniqueness validation (admin responsibility)

4. **Permission System**
   - No `CreatorUID` tracking
   - No `CreatedAtTicks` timestamp
   - No "only creator can delete" logic

5. **Dynamic Persistence**
   - No `DomainWorldData` (domains don't persist separately)
   - No save/load logic
   - Domains are purely asset-based

6. **Icon Manager**
   - No upload validation
   - No resize logic
   - No dynamic storage

7. **Domain Creation Commands**
   - No `/da domain create`
   - No `/da domain delete`
   - Keep only `/da domain list` and `/da domain info` (read-only)

---

## What Gets SIMPLIFIED

### ✓ Simplified Components

#### 1. Domain Data Model

**Before (Player-Created):**
```csharp
[ProtoContract]
public record DomainData
{
    [ProtoMember(1)] public string DomainId { get; init; }
    [ProtoMember(2)] public string Name { get; init; }
    [ProtoMember(3)] public string Description { get; init; }
    [ProtoMember(4)] public string IconPath { get; init; }
    [ProtoMember(5)] public string ColorRGBA { get; init; }
    [ProtoMember(6)] public bool IsBuiltIn { get; init; }
    [ProtoMember(7)] public string CreatorUID { get; init; }      // ❌ Removed
    [ProtoMember(8)] public long CreatedAtTicks { get; init; }    // ❌ Removed
    [ProtoMember(9)] public string FavorConfigJson { get; init; }
    [ProtoMember(10)] public int DataVersion { get; init; }
}
```

**After (Admin-Only):**
```csharp
// No ProtoBuf needed - never persisted to world save
public record DomainData
{
    public string DomainId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string IconPath { get; init; }
    public string ColorRGBA { get; init; }
    public bool IsBuiltIn { get; init; }
    public FavorTrackingConfig FavorConfig { get; init; }  // Directly store config
}
```

#### 2. Domain Registry

**Before (Player-Created):**
- `CreateCustomDomain()` with validation, profanity check, persistence
- `DeleteCustomDomain()` with permission checks, in-use validation
- `SaveDomains()` / `LoadCustomDomains()` for persistence
- Lock-protected CRUD operations

**After (Admin-Only):**
```csharp
public class DomainRegistry : IDomainRegistry
{
    private readonly ICoreServerAPI _sapi;
    private readonly Dictionary<string, DomainData> _domains = new();

    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Loading domains...");
        LoadBuiltInDomains();
        LoadCustomDomainsFromAssets();  // ← Only change
        _sapi.Logger.Notification($"[DivineAscension] Loaded {_domains.Count} domains");
    }

    private void LoadCustomDomainsFromAssets()
    {
        // Load from assets/divineascension/config/domains/*.json
        var domainFiles = _sapi.Assets.GetMany("divineascension", "config/domains");

        foreach (var asset in domainFiles)
        {
            try
            {
                var json = asset.ToText();
                var domainDto = JsonSerializer.Deserialize<DomainConfigDto>(json);
                var domain = ConvertToDomainData(domainDto);
                _domains[domain.DomainId] = domain;
            }
            catch (Exception ex)
            {
                _sapi.Logger.Error($"[DivineAscension] Failed to load domain {asset.Name}: {ex.Message}");
            }
        }
    }

    // No CRUD methods needed - admins edit JSON files
}
```

#### 3. Commands

**Before:** 4 commands (create, delete, list, info)

**After:** 2 commands (list, info) - read-only

```csharp
public void RegisterCommands()
{
    _sapi.ChatCommands
        .GetOrCreate("da")
        .BeginSubCommand("domain")
            .WithDescription("View available domains")

            .BeginSubCommand("list")
                .WithDescription("List all available domains")
                .HandleWith(OnListDomains)
            .EndSubCommand()

            .BeginSubCommand("info")
                .WithArgs(_sapi.ChatCommands.Parsers.Word("domain_name"))
                .WithDescription("View details about a domain")
                .HandleWith(OnDomainInfo)
            .EndSubCommand()

        .EndSubCommand();
}
```

---

## What STAYS THE SAME

### ✓ Unchanged Components

1. **Phase 1 (Foundation)** - Core architecture remains
   - Domain registry pattern
   - Migration from enum to domain IDs
   - `ReligionData.DomainId` field
   - Backward compatibility strategy

2. **Favor Tracking Redesign**
   - Generic favor tracker
   - Activity → multiplier configuration
   - Config loaded from JSON (just source changes from world save to assets)

3. **Blessing System Updates**
   - `Blessing.DomainId` field
   - `BlessingLoader` supports custom domains
   - Blessing JSON files in `assets/divineascension/config/blessings/custom_*.json`

4. **Religion/Civilization Integration**
   - Domain ID validation
   - Domain uniqueness in civilizations
   - All existing game logic

5. **Migration Strategy**
   - Dual field pattern (`Domain` enum + `DomainId` string)
   - Built-in domain ID constants
   - Migration utilities

---

## JSON Configuration Format

### Domain Definition File

**Location:** `assets/divineascension/config/domains/my_custom_domain.json`

```json
{
  "domainId": "custom_technology",
  "name": "Technology",
  "description": "The domain of innovation and mechanical prowess",
  "iconPath": "divineascension:textures/icons/domains/technology.png",
  "colorRGBA": "0.2,0.6,0.9,1.0",
  "favorConfig": {
    "activityMultipliers": {
      "mining": 0.5,
      "smithing": 1.5,
      "smelting": 1.0,
      "construction": 1.0
    },
    "passiveFavorRate": 0.5
  },
  "version": 1
}
```

### Blessing File for Custom Domain

**Location:** `assets/divineascension/config/blessings/technology.json`

```json
{
  "domain": "custom_technology",
  "version": 1,
  "blessings": [
    {
      "id": "tech_efficient_smelting",
      "name": "Efficient Smelting",
      "description": "Your furnaces consume 20% less fuel",
      "kind": "Player",
      "category": "Minor",
      "favorCost": 50,
      "prestigeCost": 0,
      "unlocksAtRank": "Initiate",
      "statModifiers": {
        "smeltingFuelEfficiency": 0.2
      },
      "prerequisiteBlessings": []
    }
  ]
}
```

---

## Implementation Changes by Phase

### Phase 1: Foundation (MOSTLY UNCHANGED)

**Same:**
- Create `DomainData` (simplified - no creator tracking)
- Create `DomainRegistry`
- Migration strategy
- Update `ReligionData` with `DomainId`

**Changes:**
- Remove ProtoBuf from `DomainData` (not persisted)
- Remove `DomainWorldData` (not needed)
- `DomainRegistry.Initialize()` loads from assets instead of world save
- No persistence methods

**Effort:** 1-2 weeks (reduced from 2-3)

---

### Phase 2: System Integration (SIMPLIFIED)

**Eliminated:**
- Network layer (4 packets, 1 handler)
- CRUD operations
- Icon manager
- Domain creation commands

**Kept:**
- Generic favor tracker
- Blessing system updates
- Read-only commands (list/info)
- Favor configuration loading

**Changes:**
- Load favor configs from JSON instead of world save
- No validation logic (admin responsibility)
- Simpler error handling

**Effort:** 1 week (reduced from 3-4 weeks)

---

### Phase 3: UI & Polish (ELIMINATED)

**Everything removed:**
- No GUI development
- No icon upload
- No custom blessing UI
- No runtime localization

**Effort:** 0 weeks (reduced from 2-3 weeks)

---

## Revised File Count

### New Files

| File | Purpose |
|------|---------|
| `DivineAscension/Data/DomainData.cs` | Domain data model (simplified) |
| `DivineAscension/Models/FavorTrackingConfig.cs` | Favor configuration |
| `DivineAscension/Systems/DomainRegistry.cs` | Registry (simplified) |
| `DivineAscension/Systems/Interfaces/IDomainRegistry.cs` | Interface |
| `DivineAscension/Systems/Migrations/DomainMigration.cs` | Migration utils |
| `DivineAscension/Systems/Favor/GenericFavorTracker.cs` | Generic tracker |
| `DivineAscension/Commands/DomainCommands.cs` | Read-only commands |
| `DivineAscension/Services/DomainLoader.cs` | JSON loading service |

**Total new files:** ~8 (vs 30 in player-created approach)

### Modified Files

| File | Changes |
|------|---------|
| `DivineAscension/Data/ReligionData.cs` | Add `DomainId` field |
| `DivineAscension/Models/Blessing.cs` | Add `DomainId` field |
| `DivineAscension/Services/BlessingLoader.cs` | Support custom domain IDs |
| `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` | Initialize registry |
| `DivineAscension/Systems/FavorSystem.cs` | Use generic tracker |
| `DivineAscension/Systems/BlessingRegistry.cs` | Query by domain ID |
| `DivineAscension/GUI/UI/Utilities/DomainHelper.cs` | Support custom lookups |

**Total modified files:** ~7 (vs 15 in player-created approach)

---

## Admin Workflow

### Adding a Custom Domain

1. **Create domain JSON file:**
   ```
   assets/divineascension/config/domains/my_domain.json
   ```

2. **Add domain icon (optional):**
   ```
   assets/divineascension/textures/icons/domains/my_domain.png
   ```

3. **Create blessing file (optional):**
   ```
   assets/divineascension/config/blessings/my_domain.json
   ```

4. **Reload assets or restart server**
   - Domains are loaded at server startup
   - `/da domain list` to verify

### Example: Adding "Magic" Domain

**File:** `assets/divineascension/config/domains/magic.json`

```json
{
  "domainId": "custom_magic",
  "name": "Magic",
  "description": "The domain of arcane forces and mystical power",
  "iconPath": "divineascension:textures/icons/domains/magic.png",
  "colorRGBA": "0.6,0.2,0.9,1.0",
  "favorConfig": {
    "activityMultipliers": {
      "foraging": 1.0,
      "exploration": 1.5,
      "cooking": 0.5
    },
    "passiveFavorRate": 0.75
  },
  "version": 1
}
```

**Icon:** `assets/divineascension/textures/icons/domains/magic.png` (64x64 PNG)

**Blessings:** `assets/divineascension/config/blessings/magic.json`

```json
{
  "domain": "custom_magic",
  "version": 1,
  "blessings": [
    {
      "id": "magic_arcane_insight",
      "name": "Arcane Insight",
      "description": "Increased chance to find rare resources while exploring",
      "kind": "Player",
      "category": "Minor",
      "favorCost": 75,
      "prestigeCost": 0,
      "unlocksAtRank": "Devotee",
      "statModifiers": {
        "explorationLuckBonus": 0.15
      },
      "prerequisiteBlessings": []
    }
  ]
}
```

---

## Revised Implementation Timeline

| Phase | Components | Effort | Priority |
|-------|-----------|--------|----------|
| **Phase 1: Foundation** | Data models, registry, migration | 1-2 weeks | Critical |
| **Phase 2: Integration** | Favor tracking, blessing support, commands | 1 week | Critical |
| **Testing & Documentation** | Tests, admin guide | 1 week | High |

**Total: 2-3 weeks** (vs 8-12 weeks for player-created)

---

## Advantages of Admin-Only Approach

| Advantage | Benefit |
|-----------|---------|
| **Simplicity** | 67% reduction in code (8 new files vs 30) |
| **Stability** | No dynamic CRUD = fewer bugs |
| **Performance** | No network layer overhead |
| **Security** | No profanity filtering, no spam protection needed |
| **Flexibility** | Admins can edit configs directly, no GUI limitations |
| **Testing** | Much smaller test surface (no UI, no networking) |
| **Maintenance** | Fewer moving parts, easier to debug |

---

## Disadvantages of Admin-Only Approach

| Disadvantage | Impact |
|--------------|--------|
| **Server Access Required** | Players can't create domains without admin help |
| **No In-Game Preview** | Can't see domain before restart |
| **Less Dynamic** | Need server restart to add domains |
| **Limited Adoption** | Only tech-savvy admins can add domains |
| **No Ownership** | Players don't "own" domains they suggest |

---

## Hybrid Approach (Optional)

For best of both worlds:

1. **Launch with admin-only** (2-3 weeks)
2. **Validate the system** with real servers
3. **Add player-facing UI later** if demand exists (Phase 3 from original plan)

This allows:
- Fast initial release
- Proven architecture before investing in UI
- Community feedback on domain design
- Incremental complexity

---

## Decision Matrix

| If... | Then use... |
|-------|-------------|
| Server has active, technical admins | Admin-only |
| Want fast implementation (<1 month) | Admin-only |
| Community wants creative freedom | Player-created |
| Worried about spam/abuse | Admin-only |
| Want proven system first | Admin-only → Player-created later |

---

## Recommendation

**Start with admin-only approach:**

1. **67% less code** to write and maintain
2. **75% faster** implementation (2-3 weeks vs 8-12)
3. **Proven foundation** before adding complexity
4. **Easy upgrade path** to player-created if needed
5. **Lower risk** for initial release

Once the system is stable and community feedback is gathered, evaluate whether to invest in Phase 3 (player UI).

---

## JSON Schema Documentation

Admins will need clear documentation. Add to `docs/admin/custom-domains.md`:

```markdown
# Custom Domains Admin Guide

## Overview

Admins can create custom domains by adding JSON configuration files to the mod's assets directory.

## File Locations

- **Domain definitions:** `assets/divineascension/config/domains/*.json`
- **Domain icons:** `assets/divineascension/textures/icons/domains/*.png`
- **Domain blessings:** `assets/divineascension/config/blessings/<domain_id>.json`

## Domain JSON Schema

[Full schema documentation here...]
```

---

**Document Version:** 1.0
**Last Updated:** 2026-01-18
**Author:** Claude Code
**Status:** Draft - Comparison Analysis
