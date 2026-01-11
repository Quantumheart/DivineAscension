# Blessing Translation Implementation Plan

## Overview

Blessing display names and descriptions are currently hardcoded as English strings in `BlessingDefinitions.cs`. Unlike deity names, stat names, rank names, and UI strings which use the `LocalizationService`, blessing names bypass the translation system entirely. This means blessings always display in English regardless of the player's language setting.

**Goal:** Migrate all 40 blessing names and descriptions to use the localization system, enabling full translation support.

## Current State Analysis

### What IS Localized (Working Examples)

| Component | Key Pattern | Example |
|-----------|-------------|---------|
| Deity names | `divineascension:deity.<deity>.name` | `"Khoras"` |
| Stat names | `divineascension:stat.<stat_id>` | `"Mining Speed"` |
| Rank names | `divineascension:rank.favor.<rank>` | `"Disciple"` |
| UI strings | `divineascension:ui.<area>.<element>` | `"Unlock"` |

### What is NOT Localized (The Problem)

**File:** `/DivineAscension/Systems/BlessingDefinitions.cs`

```csharp
// Current implementation - hardcoded English strings
new(BlessingIds.KhorasCraftsmansTouch, "Craftsman's Touch", DeityType.Khoras)
    .WithDescription("Your crafted items have improved durability...")
```

**Consumers using hardcoded names:**
- `BlessingTooltipData.cs` - Copies `blessing.Name` directly
- `BlessingNodeRenderer.cs` - Uses `state.Blessing!.Name` for display
- `BlessingCommands.cs` - Passes `blessing.Name` to format strings

## Architecture

### Approach: Translation Key Storage

Store translation keys in the `Blessing` model instead of display strings. Resolve to localized text at render time.

**Rationale:**
- Blessings are created once at server startup and synced to clients
- Client language can differ from server language
- Resolving at render time ensures correct language per client

### Key Naming Convention

```
divineascension:blessing.<deity>.<blessing_id>.name
divineascension:blessing.<deity>.<blessing_id>.desc
```

**Examples:**
```
divineascension:blessing.khoras.craftsmans_touch.name
divineascension:blessing.khoras.craftsmans_touch.desc
divineascension:blessing.lysa.hunters_instinct.name
divineascension:blessing.lysa.hunters_instinct.desc
```

### Translation Key Derivation

Keys can be derived from `BlessingId` using a utility method:

```csharp
public static class BlessingLocalizationHelper
{
    public static string GetNameKey(string blessingId, DeityType deity)
    {
        var deityName = deity.ToString().ToLowerInvariant();
        var blessingKey = blessingId.Replace(".", "_").ToLowerInvariant();
        return $"divineascension:blessing.{deityName}.{blessingKey}.name";
    }

    public static string GetDescriptionKey(string blessingId, DeityType deity)
    {
        var deityName = deity.ToString().ToLowerInvariant();
        var blessingKey = blessingId.Replace(".", "_").ToLowerInvariant();
        return $"divineascension:blessing.{deityName}.{blessingKey}.desc";
    }
}
```

## Implementation Steps

### 1. Add Localization Keys to Constants

**File:** `/DivineAscension/Services/LocalizationKeys.cs`

Add 80 new constants (40 names + 40 descriptions) organized by deity:

```csharp
#region Blessing Names - Khoras

public const string BLESSING_KHORAS_CRAFTSMANS_TOUCH_NAME =
    "divineascension:blessing.khoras.craftsmans_touch.name";
public const string BLESSING_KHORAS_CRAFTSMANS_TOUCH_DESC =
    "divineascension:blessing.khoras.craftsmans_touch.desc";
// ... remaining Khoras blessings

#endregion

#region Blessing Names - Lysa
// ... Lysa blessings
#endregion

#region Blessing Names - Aethra
// ... Aethra blessings
#endregion

#region Blessing Names - Gaia
// ... Gaia blessings
#endregion
```

### 2. Add Translations to Language Files

**File:** `/DivineAscension/assets/divineascension/lang/en.json`

Add English translations (extracted from current hardcoded values):

```json
"divineascension:blessing.khoras.craftsmans_touch.name": "Craftsman's Touch",
"divineascension:blessing.khoras.craftsmans_touch.desc": "Your crafted items have improved durability. Gain +10% tool durability.",

"divineascension:blessing.khoras.masterwork_tools.name": "Masterwork Tools",
"divineascension:blessing.khoras.masterwork_tools.desc": "Tools you create are of superior quality. +15% mining speed.",
```

**Other language files to update:**
- `fr.json` - French translations
- `es.json` - Spanish translations
- `de.json` - German translations
- `ru.json` - Russian translations

### 3. Create BlessingLocalizationHelper Utility

**New File:** `/DivineAscension/GUI/UI/Utilities/BlessingLocalizationHelper.cs`

```csharp
public static class BlessingLocalizationHelper
{
    public static string GetLocalizedName(Blessing blessing)
    {
        var key = GetNameKey(blessing.BlessingId, blessing.Deity);
        return LocalizationService.Instance.Get(key);
    }

    public static string GetLocalizedDescription(Blessing blessing)
    {
        var key = GetDescriptionKey(blessing.BlessingId, blessing.Deity);
        return LocalizationService.Instance.Get(key);
    }

    public static string GetNameKey(string blessingId, DeityType deity)
    {
        var deityName = deity.ToString().ToLowerInvariant();
        var blessingKey = NormalizeBlessingId(blessingId);
        return $"divineascension:blessing.{deityName}.{blessingKey}.name";
    }

    public static string GetDescriptionKey(string blessingId, DeityType deity)
    {
        var deityName = deity.ToString().ToLowerInvariant();
        var blessingKey = NormalizeBlessingId(blessingId);
        return $"divineascension:blessing.{deityName}.{blessingKey}.desc";
    }

    private static string NormalizeBlessingId(string blessingId)
    {
        // Convert "khoras.craftsmans_touch" to "craftsmans_touch"
        var parts = blessingId.Split('.');
        return parts.Length > 1 ? parts[1] : blessingId;
    }
}
```

### 4. Update Blessing Consumers

**File:** `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingTooltipData.cs`

```csharp
// Before
Name = blessing.Name,
Description = blessing.Description,

// After
Name = BlessingLocalizationHelper.GetLocalizedName(blessing),
Description = BlessingLocalizationHelper.GetLocalizedDescription(blessing),
```

**File:** `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingNodeRenderer.cs`

```csharp
// Before
var blessingName = state.Blessing!.Name;

// After
var blessingName = BlessingLocalizationHelper.GetLocalizedName(state.Blessing!);
```

**File:** `/DivineAscension/Commands/BlessingCommands.cs`

```csharp
// Before
LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_UNLOCKED, blessing.Name)

// After
var localizedName = BlessingLocalizationHelper.GetLocalizedName(blessing);
LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_UNLOCKED, localizedName)
```

### 5. Optional: Keep Blessing.Name/Description as Fallback

The `Blessing` model can retain `Name` and `Description` properties as English fallbacks. If a translation key is missing, the helper can return the fallback:

```csharp
public static string GetLocalizedName(Blessing blessing)
{
    var key = GetNameKey(blessing.BlessingId, blessing.Deity);
    if (LocalizationService.Instance.HasKey(key))
        return LocalizationService.Instance.Get(key);
    return blessing.Name; // Fallback to hardcoded English
}
```

## Data Flow

```
BlessingDefinitions.cs
  ↓ Creates Blessing with BlessingId, DeityType
  ↓
BlessingRegistry.cs
  ↓ Stores all blessings
  ↓
BlessingNetworkHandler / Network Sync
  ↓ Sends blessing data to client
  ↓
Client UI (BlessingNodeRenderer, BlessingTooltipData)
  ↓ Calls BlessingLocalizationHelper.GetLocalizedName(blessing)
  ↓
BlessingLocalizationHelper
  ↓ Derives key: "divineascension:blessing.khoras.craftsmans_touch.name"
  ↓
LocalizationService.Instance.Get(key)
  ↓ Returns localized string from en.json/fr.json/etc.
```

## Critical Files

### New Files (1):

1. `/DivineAscension/GUI/UI/Utilities/BlessingLocalizationHelper.cs`

### Modified Files:

1. `/DivineAscension/Services/LocalizationKeys.cs` - Add 80 blessing key constants
2. `/DivineAscension/assets/divineascension/lang/en.json` - Add 80 English translations
3. `/DivineAscension/assets/divineascension/lang/fr.json` - Add 80 French translations
4. `/DivineAscension/assets/divineascension/lang/es.json` - Add 80 Spanish translations
5. `/DivineAscension/assets/divineascension/lang/de.json` - Add 80 German translations
6. `/DivineAscension/assets/divineascension/lang/ru.json` - Add 80 Russian translations
7. `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingTooltipData.cs` - Use helper
8. `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingNodeRenderer.cs` - Use helper
9. `/DivineAscension/Commands/BlessingCommands.cs` - Use helper

## Blessing Inventory

### Khoras (God of Forge & Craft) - 10 Blessings

| ID | Current Name | Key Suffix |
|----|--------------|------------|
| khoras.craftsmans_touch | Craftsman's Touch | craftsmans_touch |
| khoras.masterwork_tools | Masterwork Tools | masterwork_tools |
| khoras.forgeborn_endurance | Forgeborn Endurance | forgeborn_endurance |
| khoras.legendary_smith | Legendary Smith | legendary_smith |
| khoras.avatar_of_forge | Avatar of the Forge | avatar_of_forge |
| khoras.iron_will | Iron Will | iron_will |
| khoras.smelters_efficiency | Smelter's Efficiency | smelters_efficiency |
| khoras.anvil_mastery | Anvil Mastery | anvil_mastery |
| khoras.blessed_metalwork | Blessed Metalwork | blessed_metalwork |
| khoras.forge_communion | Forge Communion | forge_communion |

### Lysa (Goddess of the Hunt) - 10 Blessings

| ID | Current Name | Key Suffix |
|----|--------------|------------|
| lysa.hunters_instinct | Hunter's Instinct | hunters_instinct |
| lysa.swift_pursuit | Swift Pursuit | swift_pursuit |
| lysa.predators_focus | Predator's Focus | predators_focus |
| lysa.natures_bounty | Nature's Bounty | natures_bounty |
| lysa.avatar_of_hunt | Avatar of the Hunt | avatar_of_hunt |
| lysa.keen_senses | Keen Senses | keen_senses |
| lysa.stalkers_patience | Stalker's Patience | stalkers_patience |
| lysa.wild_endurance | Wild Endurance | wild_endurance |
| lysa.pack_tactics | Pack Tactics | pack_tactics |
| lysa.moonlit_strike | Moonlit Strike | moonlit_strike |

### Aethra (Goddess of Agriculture) - 10 Blessings

| ID | Current Name | Key Suffix |
|----|--------------|------------|
| aethra.green_thumb | Green Thumb | green_thumb |
| aethra.bountiful_harvest | Bountiful Harvest | bountiful_harvest |
| aethra.seasons_wisdom | Season's Wisdom | seasons_wisdom |
| aethra.fertile_touch | Fertile Touch | fertile_touch |
| aethra.avatar_of_growth | Avatar of Growth | avatar_of_growth |
| aethra.seed_blessing | Seed Blessing | seed_blessing |
| aethra.crop_resilience | Crop Resilience | crop_resilience |
| aethra.harvest_keeper | Harvest Keeper | harvest_keeper |
| aethra.nourishing_aura | Nourishing Aura | nourishing_aura |
| aethra.earth_communion | Earth Communion | earth_communion |

### Gaia (Goddess of Pottery & Earth) - 10 Blessings

| ID | Current Name | Key Suffix |
|----|--------------|------------|
| gaia.clay_shaping | Clay Shaping | clay_shaping |
| gaia.kiln_mastery | Kiln Mastery | kiln_mastery |
| gaia.earthen_resilience | Earthen Resilience | earthen_resilience |
| gaia.master_potter | Master Potter | master_potter |
| gaia.avatar_of_earth | Avatar of Earth | avatar_of_earth |
| gaia.steady_hands | Steady Hands | steady_hands |
| gaia.clay_abundance | Clay Abundance | clay_abundance |
| gaia.fired_perfection | Fired Perfection | fired_perfection |
| gaia.earthbound_strength | Earthbound Strength | earthbound_strength |
| gaia.terra_communion | Terra Communion | terra_communion |

## Edge Cases

1. **Missing translation key** - Fall back to English `Name`/`Description` property
2. **Server vs client language** - Resolution happens client-side at render time
3. **Network sync** - Blessing IDs are synced, not display names (keys derived from ID)
4. **Command output** - Server-side commands use server language (acceptable)
5. **New blessings added** - Must add keys and translations for each language file

## Testing

### Unit Tests

- `BlessingLocalizationHelper.GetNameKey()` returns correct key format
- `BlessingLocalizationHelper.GetLocalizedName()` returns translated string
- Fallback returns English name when key missing

### Manual Tests

1. Verify all 40 blessing names display correctly in English
2. Switch language to French, verify blessing names update
3. Test tooltip descriptions in multiple languages
4. Test `/blessing list` command output
5. Verify no missing translation warnings in console

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Translation quality | Start with English, add other languages incrementally |
| Missing keys | Fallback mechanism returns English |
| Performance | Key derivation is simple string manipulation |
| Breaking changes | Blessing model unchanged, only consumers updated |
