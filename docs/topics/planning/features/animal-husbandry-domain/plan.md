# Animal Husbandry Domain Implementation Plan

## Overview

Add a new deity domain focused on animal husbandry activities: breeding, taming, caring for livestock, and animal product collection (milk, wool, eggs). The domain rewards players who maintain and grow animal populations rather than hunting them.

**Domain Name:** `Husbandry`
**Deity Theme:** Pastoral/Shepherd deity (suggested name: "Pastura" or customizable)
**Enum Value:** `5` (filling the gap between Harvest=4 and Stone=7)
**Domain Color:** Warm tan/cream (pastoral theme) - `Vector4(0.85f, 0.75f, 0.55f, 1.0f)`

---

## Implementation Steps

### Phase 1: Core Domain Infrastructure

#### Step 1.1: Add DeityDomain Enum Value

**File:** `DivineAscension/Models/Enum/DeityDomain.cs`

```csharp
public enum DeityDomain
{
    None = 0,
    Craft = 1,
    Wild = 2,
    Conquest = 3,
    Harvest = 4,
    Husbandry = 5,  // NEW
    Stone = 7
}
```

#### Step 1.2: Update DomainHelper

**File:** `DivineAscension/GUI/UI/Utilities/DomainHelper.cs`

Add to `DomainNames` array:
```csharp
public static readonly string[] DomainNames = { "Craft", "Wild", "Conquest", "Harvest", "Husbandry", "Stone" };
```

Add color case:
```csharp
"Husbandry" => new Vector4(0.85f, 0.75f, 0.55f, 1.0f),  // Warm tan
```

Add title case:
```csharp
DeityDomain.Husbandry => "Domain of Husbandry",
```

#### Step 1.3: Update DeityInfoHelper

**File:** `DivineAscension/GUI/UI/Utilities/DeityInfoHelper.cs`

Add domain info:
```csharp
DeityDomain.Husbandry => new DomainInfo(
    Name: "Husbandry",
    Description: "The domain of Animal Husbandry. Followers are rewarded for breeding animals, " +
                 "collecting animal products, and maintaining healthy livestock populations."
),
```

---

### Phase 2: Favor Trackers

Create favor trackers for husbandry activities.

#### Step 2.1: BreedingFavorTracker

**File:** `DivineAscension/Systems/Favor/BreedingFavorTracker.cs`

**Purpose:** Awards favor when animals breed successfully.

**Favor Values:**
| Activity | Favor |
|----------|-------|
| Successful breeding (any animal) | 8 |
| Breeding rare/difficult animals | 15 |
| First breeding of a species (per player) | 25 |

**Events to Hook:**
- Entity spawn events where parent entities exist
- Need Harmony patch for `EntityBehaviorMultiply` or similar breeding behavior

**Implementation Pattern:**
```csharp
public class BreedingFavorTracker : IFavorTracker, IDisposable
{
    public DeityDomain DeityDomain => DeityDomain.Husbandry;

    private readonly HashSet<string> _husbandryFollowers = new();

    public void Initialize()
    {
        // Subscribe to entity birth events
        // Hook into EntityBehaviorMultiply.OnGameTick or similar
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
    }

    private void OnAnimalBorn(Entity baby, Entity parent1, Entity parent2, string breederPlayerUID)
    {
        if (!_husbandryFollowers.Contains(breederPlayerUID)) return;

        var favor = CalculateBreedingFavor(baby);
        _favorSystem.AwardFavor(breederPlayerUID, favor, "Breeding");
    }
}
```

#### Step 2.2: AnimalProductFavorTracker

**File:** `DivineAscension/Systems/Favor/AnimalProductFavorTracker.cs`

**Purpose:** Awards favor for collecting animal products without killing.

**Favor Values:**
| Product | Favor |
|---------|-------|
| Milk collection | 3 |
| Wool shearing | 4 |
| Egg collection | 2 |
| Honey harvesting | 5 |

**Events to Hook:**
- Block interaction with milk pails, shears on sheep
- Item collection from nest blocks
- Need Harmony patches for relevant interactions

#### Step 2.3: TamingFavorTracker

**File:** `DivineAscension/Systems/Favor/TamingFavorTracker.cs`

**Purpose:** Awards favor for taming wild animals.

**Favor Values:**
| Activity | Favor |
|----------|-------|
| Taming small animal | 10 |
| Taming medium animal | 15 |
| Taming large/difficult animal | 25 |

**Events to Hook:**
- `EntityBehaviorTameable` state changes
- Need Harmony patch for taming completion

---

### Phase 3: Blessings

#### Step 3.1: Create Blessing Definition File

**File:** `DivineAscension/assets/divineascension/config/blessings/husbandry.json`

```json
{
  "domain": "Husbandry",
  "version": 1,
  "blessings": [
    {
      "blessingId": "pastura_gentle_touch",
      "name": "Gentle Touch",
      "description": "+10% animal trust gain rate when taming.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "gentle-hand",
      "requiredFavorRank": 0,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": [],
      "statModifiers": {
        "animalTrustGain": 0.10
      },
      "specialEffects": []
    },
    {
      "blessingId": "pastura_fertile_flock",
      "name": "Fertile Flock",
      "description": "+15% breeding success rate.",
      "kind": "Player",
      "category": "Production",
      "iconName": "breeding-pair",
      "requiredFavorRank": 1,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["pastura_gentle_touch"],
      "statModifiers": {
        "breedingSuccessRate": 0.15
      },
      "specialEffects": []
    },
    {
      "blessingId": "pastura_abundant_yield",
      "name": "Abundant Yield",
      "description": "+20% animal product yield (milk, wool, eggs).",
      "kind": "Player",
      "category": "Production",
      "iconName": "overflowing-basket",
      "requiredFavorRank": 2,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["pastura_fertile_flock"],
      "statModifiers": {
        "animalProductYield": 0.20
      },
      "specialEffects": []
    },
    {
      "blessingId": "pastura_shepherds_call",
      "name": "Shepherd's Call",
      "description": "Animals follow you more readily and from greater distance.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "shepherds-crook",
      "requiredFavorRank": 2,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["pastura_gentle_touch"],
      "statModifiers": {
        "animalFollowRange": 0.50
      },
      "specialEffects": ["animal_attraction_aura"]
    },
    {
      "blessingId": "pastura_herd_vitality",
      "name": "Herd Vitality",
      "description": "Your animals have +25% health and recover faster.",
      "kind": "Religion",
      "category": "Defense",
      "iconName": "healthy-herd",
      "requiredFavorRank": 0,
      "requiredPrestigeRank": 2,
      "prerequisiteBlessings": [],
      "statModifiers": {
        "ownedAnimalHealth": 0.25,
        "ownedAnimalRegen": 0.15
      },
      "specialEffects": []
    },
    {
      "blessingId": "pastura_beast_whisperer",
      "name": "Beast Whisperer",
      "description": "Can tame normally untameable creatures. Wild animals are less aggressive.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "beast-communion",
      "requiredFavorRank": 4,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["pastura_shepherds_call", "pastura_abundant_yield"],
      "statModifiers": {
        "wildAnimalAggression": -0.30
      },
      "specialEffects": ["tame_exotic_animals"]
    }
  ]
}
```

#### Step 3.2: Update BlessingLoader

**File:** `DivineAscension/Services/BlessingLoader.cs`

```csharp
private static readonly string[] DomainFiles = { "craft", "wild", "conquest", "harvest", "husbandry", "stone" };
```

---

### Phase 4: Effect Handlers

#### Step 4.1: Create PasturaEffectHandlers

**File:** `DivineAscension/Systems/BlessingEffects/Handlers/PasturaEffectHandlers.cs`

```csharp
public class AnimalAttractionAuraEffect : ISpecialEffectHandler
{
    public string EffectId => "animal_attraction_aura";

    public void ActivateForPlayer(IServerPlayer player)
    {
        // Increase follow range for nearby tamed animals
    }
}

public class TameExoticAnimalsEffect : ISpecialEffectHandler
{
    public string EffectId => "tame_exotic_animals";

    public void ActivateForPlayer(IServerPlayer player)
    {
        // Allow taming of normally untameable creatures
    }
}
```

---

### Phase 5: Harmony Patches

#### Step 5.1: BreedingPatches

**File:** `DivineAscension/Systems/Patches/BreedingPatches.cs`

Hook into `EntityBehaviorMultiply` to detect successful breeding:

```csharp
[HarmonyPatch(typeof(EntityBehaviorMultiply))]
public static class BreedingPatches
{
    public static event Action<Entity, Entity, Entity, string>? OnAnimalBred;

    [HarmonyPostfix]
    [HarmonyPatch("TryBeginPregnancy")]  // or equivalent method
    public static void Postfix_TryBeginPregnancy(EntityBehaviorMultiply __instance, bool __result)
    {
        if (__result)
        {
            // Determine which player initiated breeding
            // Raise OnAnimalBred event
        }
    }
}
```

#### Step 5.2: AnimalProductPatches

**File:** `DivineAscension/Systems/Patches/AnimalProductPatches.cs`

Hook into milk pail, shearing, egg collection interactions.

#### Step 5.3: TamingPatches

**File:** `DivineAscension/Systems/Patches/TamingPatches.cs`

Hook into `EntityBehaviorTameable` state changes.

---

### Phase 6: FavorSystem Integration

#### Step 6.1: Update FavorSystem

**File:** `DivineAscension/Systems/FavorSystem.cs`

Add tracker fields:
```csharp
private BreedingFavorTracker? _breedingFavorTracker;
private AnimalProductFavorTracker? _animalProductFavorTracker;
private TamingFavorTracker? _tamingFavorTracker;
```

Initialize in `Initialize()`:
```csharp
_breedingFavorTracker = new BreedingFavorTracker(...);
_breedingFavorTracker.Initialize();

_animalProductFavorTracker = new AnimalProductFavorTracker(...);
_animalProductFavorTracker.Initialize();

_tamingFavorTracker = new TamingFavorTracker(...);
_tamingFavorTracker.Initialize();

_logger.Notification("[DivineAscension] Initialized 13 favor trackers");  // Update count
```

Dispose in `Dispose()`:
```csharp
_breedingFavorTracker?.Dispose();
_animalProductFavorTracker?.Dispose();
_tamingFavorTracker?.Dispose();
```

---

### Phase 7: Assets

#### Step 7.1: Domain Icon

**File:** `DivineAscension/assets/divineascension/textures/icons/deities/husbandry.png`

32x32 PNG icon representing pastoral/husbandry theme (sheep, shepherd's crook, barn, etc.)

#### Step 7.2: Blessing Icons

**Directory:** `DivineAscension/assets/divineascension/textures/icons/perks/husbandry/`

Create icons for each blessing:
- `gentle-hand.png`
- `breeding-pair.png`
- `overflowing-basket.png`
- `shepherds-crook.png`
- `healthy-herd.png`
- `beast-communion.png`

---

### Phase 8: Localization

#### Step 8.1: Update Language Files

**File:** `DivineAscension/assets/divineascension/lang/en.json`

Add entries for:
- Domain name and description
- All blessing names and descriptions
- Favor activity messages

---

## Testing Requirements

### Unit Tests

1. **BreedingFavorTrackerTests**
   - Verify favor awarded on successful breeding
   - Verify no favor for non-followers
   - Verify correct favor values by animal type

2. **AnimalProductFavorTrackerTests**
   - Verify favor for milk/wool/egg collection
   - Verify follower filtering

3. **TamingFavorTrackerTests**
   - Verify favor on taming completion
   - Verify difficulty-based favor scaling

4. **HusbandryBlessingTests**
   - Verify blessing definitions load correctly
   - Verify prerequisite chains
   - Verify stat modifiers apply

### Integration Tests

1. Religion creation with Husbandry domain
2. Civilization formation with Husbandry religion
3. Blessing unlock flow for Husbandry blessings

---

## Files Changed Summary

| Category | Files | Action |
|----------|-------|--------|
| Enum | `Models/Enum/DeityDomain.cs` | Modify |
| UI Helpers | `GUI/UI/Utilities/DomainHelper.cs` | Modify |
| UI Helpers | `GUI/UI/Utilities/DeityInfoHelper.cs` | Modify |
| Favor Trackers | `Systems/Favor/BreedingFavorTracker.cs` | Create |
| Favor Trackers | `Systems/Favor/AnimalProductFavorTracker.cs` | Create |
| Favor Trackers | `Systems/Favor/TamingFavorTracker.cs` | Create |
| Systems | `Systems/FavorSystem.cs` | Modify |
| Blessings | `assets/.../config/blessings/husbandry.json` | Create |
| Services | `Services/BlessingLoader.cs` | Modify |
| Effect Handlers | `Systems/BlessingEffects/Handlers/PasturaEffectHandlers.cs` | Create |
| Patches | `Systems/Patches/BreedingPatches.cs` | Create |
| Patches | `Systems/Patches/AnimalProductPatches.cs` | Create |
| Patches | `Systems/Patches/TamingPatches.cs` | Create |
| Assets | `assets/.../textures/icons/deities/husbandry.png` | Create |
| Assets | `assets/.../textures/icons/perks/husbandry/*.png` | Create (6) |
| Localization | `assets/.../lang/en.json` | Modify |
| Tests | `Tests/Systems/Favor/BreedingFavorTrackerTests.cs` | Create |
| Tests | `Tests/Systems/Favor/AnimalProductFavorTrackerTests.cs` | Create |
| Tests | `Tests/Systems/Favor/TamingFavorTrackerTests.cs` | Create |

**Total:** ~20 files (8 new, 12 modified)

---

## Risks & Considerations

1. **Vintage Story Animal API**: Need to verify exact hooks available for breeding/taming events
2. **Performance**: Breeding events may be frequent in large farms - ensure tracker is optimized
3. **Balance**: Husbandry activities may be less frequent than other domains - balance favor values accordingly
4. **Mod Compatibility**: May conflict with other animal mods that modify breeding behavior
