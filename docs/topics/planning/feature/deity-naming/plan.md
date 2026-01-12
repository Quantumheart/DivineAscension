# DeityType Rename and Custom Deity Naming Feature

## Overview

This document outlines the plan to refactor `DeityType` to `DeityDomain` and add a required custom deity naming feature to religions.

### Goals

1. **Rename `DeityType` enum to `DeityDomain`** - Better reflects that the enum represents domains of influence, not deity identities
2. **Rename enum values to domain names** - Change from deity names (Khoras, Lysa, etc.) to domain names (Craft, Wild, etc.)
3. **Add required `DeityName` field** - Allow religions to name their deity for roleplay opportunities

### Motivation

Currently, the `DeityType` enum values serve dual purposes:
- **Domain/Archetype** - Determines game mechanics (blessings, favor, abilities)
- **Display Name** - Shown to players in the UI

By separating these concerns:
- **Domain** (enum) - Controls game mechanics
- **DeityName** (string) - Display text chosen by the religion founder

This enables richer roleplay where different religions following the same domain can worship differently-named deities.

---

## Enum Changes

### Before

```csharp
public enum DeityType
{
    None = 0,
    Khoras = 1,   // God of the Forge & Craft
    Lysa = 2,     // Goddess of the Hunt & Wild
    Aethra = 4,   // Goddess of Agriculture & Light
    Gaia = 7      // Goddess of Pottery & Clay
}
```

### After

```csharp
public enum DeityDomain
{
    None = 0,
    Craft = 1,    // Mining, smithing, smelting, tool durability
    Wild = 2,     // Hunting, foraging, wilderness survival
    Harvest = 4,  // Farming, cooking, cultivation
    Stone = 7     // Pottery, clay forming, kilns
}
```

### Value Mapping

| Before | After | Domain Focus |
|--------|-------|--------------|
| `DeityType.None` | `DeityDomain.None` | No deity |
| `DeityType.Khoras` | `DeityDomain.Craft` | Forge & Craft |
| `DeityType.Lysa` | `DeityDomain.Wild` | Hunt & Wild |
| `DeityType.Aethra` | `DeityDomain.Harvest` | Agriculture & Light |
| `DeityType.Gaia` | `DeityDomain.Stone` | Pottery & Clay |

**Note:** Integer values (0, 1, 2, 4, 7) remain unchanged for save data compatibility.

---

## Data Model Changes

### ReligionData

```csharp
// Property rename
[ProtoMember(3)]
public DeityDomain Domain { get; set; }  // Was: DeityType Deity

// New required field
[ProtoMember(18)]
public string DeityName { get; set; }    // Required - custom deity name
```

### Validation Rules

```csharp
// DeityName validation
- Required (cannot be empty or whitespace)
- Min length: 2 characters
- Max length: 48 characters
- Allowed characters: letters, spaces, apostrophes, hyphens
```

---

## Command Syntax Changes

### Before

```bash
/religion create "Religion Name" <deity> [public|private]
```

Example:
```bash
/religion create "Forge Masters" Khoras public
```

### After

```bash
/religion create "Religion Name" <domain> "Deity Name" [public|private]
```

Examples:
```bash
/religion create "Forge Masters" Craft "Khoras the Eternal Smith" public
/religion create "Wild Hunters" Wild "Lysa of the Forest" public
/religion create "Golden Fields" Harvest "Aethra the Bountiful" public
/religion create "Stone Shapers" Stone "Gaia the Potter" public
```

---

## UI Changes

### Create Religion Tab

```
┌─────────────────────────────────────────────────┐
│ Create Religion                                 │
├─────────────────────────────────────────────────┤
│ Religion Name                                   │
│ ┌─────────────────────────────────────────────┐ │
│ │ Forge Masters                               │ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
│ Select Domain                                   │
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐    │
│ │ Craft  │ │  Wild  │ │Harvest │ │ Stone  │    │
│ │  [icon]│ │ [icon] │ │ [icon] │ │ [icon] │    │
│ └────────┘ └────────┘ └────────┘ └────────┘    │
│                                                 │
│ Deity Name (required)                           │
│ ┌─────────────────────────────────────────────┐ │
│ │ Khoras the Eternal Smith                    │ │
│ └─────────────────────────────────────────────┘ │
│ Name the deity your religion worships           │
│                                                 │
│ Visibility                                      │
│ ○ Public  ○ Private                            │
│                                                 │
│              [ Create Religion ]                │
└─────────────────────────────────────────────────┘
```

---

## Save Data Compatibility

| Change | Compatible? | Reason |
|--------|-------------|--------|
| Enum type rename (`DeityType` → `DeityDomain`) | Yes | ProtoBuf uses integer values |
| Enum value rename (`Khoras` → `Craft`, etc.) | Yes | Integer values (1,2,4,7) unchanged |
| Property rename (`Deity` → `Domain`) | Yes | Keep same ProtoMember(3) |
| New `DeityName` field | Yes | ProtoMember(18), defaults to empty |

### Migration for Existing Religions

Existing religions will have an empty `DeityName` field after the update.

**Recommended Approach:** Auto-generate default names with founder notification

1. **On world load:** Detect religions with empty `DeityName`
2. **Auto-generate:** Set `DeityName` to the legacy deity name based on domain:
   - `Craft` → "Khoras"
   - `Wild` → "Lysa"
   - `Harvest` → "Aethra"
   - `Stone` → "Gaia"
3. **Notify founders:** Send a one-time message when founder logs in explaining they can customize via
   `/religion setdeityname "New Name"`
4. **New command:** Add `/religion setdeityname` for founders to update the deity name

**Alternative Approach (Not Recommended):** Force founders to set name on login

- Blocks religion functionality until name is set
- Poor UX for returning players
- Requires additional UI modal implementation

---

## Scope Summary

| Phase     | Description                                           | Files Affected |
|-----------|-------------------------------------------------------|----------------|
| Phase 1   | Rename `DeityType` → `DeityDomain` with value changes | ~117 files     |
| Phase 2   | Add required `DeityName` field + migration + UI edit  | ~50 files      |
| **Total** |                                                       | ~165 files     |

---

## Risk Assessment

| Risk                    | Level  | Mitigation                                              |
|-------------------------|--------|---------------------------------------------------------|
| Save data compatibility | Low    | ProtoMember IDs unchanged; new field defaults to empty  |
| Test suite breakage     | Medium | Global find-replace, verify all tests pass              |
| Display inconsistency   | Medium | Centralize logic in `DeityHelper.GetDeityDisplayName()` |
| Network protocol        | Medium | Multiple packets need DeityName (see packet list below) |
| Command breaking change | Medium | Document new syntax, consider transition period         |
| Script migration        | Medium | Existing automation using old command syntax will break |

### Network Packets Requiring Updates

The following packets will need `DeityName` field added in Phase 2:

- `CreateReligionRequestPacket.cs` - Client sends deity name when creating
- `CreateReligionResponsePacket.cs` - Server confirms created religion
- `ReligionDetailResponsePacket.cs` - Full religion info for detail view
- `ReligionListResponsePacket.cs` - Religion list for browse view
- `PlayerReligionDataPacket.cs` - Player's current religion info
- `ReligionStateChangedPacket.cs` - Broadcast when religion state changes

**New packets for deity name editing:**

- `SetDeityNameRequestPacket.cs` - Client requests deity name change (from UI or command)
- `SetDeityNameResponsePacket.cs` - Server confirms/rejects the change

---

## Implementation Phases

### Phase 1: Rename DeityType to DeityDomain

Mechanical refactor to rename the enum type and values across the codebase.

**Find/Replace Operations:**
1. `DeityType.Khoras` → `DeityDomain.Craft`
2. `DeityType.Lysa` → `DeityDomain.Wild`
3. `DeityType.Aethra` → `DeityDomain.Harvest`
4. `DeityType.Gaia` → `DeityDomain.Stone`
5. `DeityType.None` → `DeityDomain.None`
6. `DeityType` → `DeityDomain`

### Phase 2: Add Required DeityName Field

Feature implementation to add custom deity naming with required field validation and migration support.

**Key Changes:**
1. Add `DeityName` property to `ReligionData`
2. Update `ReligionManager.CreateReligion()` signature
3. Update network packets (6 packet files)
4. Update command parser
5. Add UI input field
6. Update display logic
7. Add validation

**Migration (for existing religions):**

1. Implement `MigrateEmptyDeityNames()` to auto-populate legacy deity names
2. Add `/religion setdeityname` command for founders
3. Add `/religion admin setdeityname` command for server admins
4. Add founder notification on first login after migration
5. Update tests

See `tasks.md` for detailed task breakdown.
