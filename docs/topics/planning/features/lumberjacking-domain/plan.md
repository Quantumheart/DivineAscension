# Lumberjacking Domain Implementation Plan

## Overview

Add a new deity domain focused on forestry and woodworking: tree felling, log processing, wood crafting, and forest management. The domain rewards players who work with wood and trees.

**Domain Name:** `Timber`
**Deity Theme:** Forest/Woodworking deity (suggested name: "Sylvanus" or customizable)
**Enum Value:** `6` (between Husbandry=5 and Stone=7)
**Domain Color:** Deep forest green/brown - `Vector4(0.35f, 0.55f, 0.25f, 1.0f)`

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
    Husbandry = 5,
    Timber = 6,  // NEW
    Stone = 7
}
```

#### Step 1.2: Update DomainHelper

**File:** `DivineAscension/GUI/UI/Utilities/DomainHelper.cs`

Add to `DomainNames` array:
```csharp
public static readonly string[] DomainNames = { "Craft", "Wild", "Conquest", "Harvest", "Husbandry", "Timber", "Stone" };
```

Add color case:
```csharp
"Timber" => new Vector4(0.35f, 0.55f, 0.25f, 1.0f),  // Forest green/brown
```

Add title case:
```csharp
DeityDomain.Timber => "Domain of Timber",
```

#### Step 1.3: Update DeityInfoHelper

**File:** `DivineAscension/GUI/UI/Utilities/DeityInfoHelper.cs`

Add domain info:
```csharp
DeityDomain.Timber => new DomainInfo(
    Name: "Timber",
    Description: "The domain of Timber and Forestry. Followers are rewarded for felling trees, " +
                 "crafting with wood, and managing forest resources sustainably."
),
```

---

### Phase 2: Favor Trackers

Create favor trackers for lumberjacking activities.

#### Step 2.1: TreeFellingFavorTracker

**File:** `DivineAscension/Systems/Favor/TreeFellingFavorTracker.cs`

**Purpose:** Awards favor when players chop down trees.

**Favor Values:**
| Tree Type | Favor |
|-----------|-------|
| Small tree (1-3 logs) | 2 |
| Medium tree (4-8 logs) | 5 |
| Large tree (9-15 logs) | 10 |
| Giant tree (16+ logs) | 20 |
| Rare wood types (kapok, aged) | +50% bonus |

**Events to Hook:**
- Block break events for log blocks
- Need to detect "tree fell" vs "single log break"
- Vintage Story has `TreeFelling` behavior that can be hooked

**Implementation Pattern:**
```csharp
public class TreeFellingFavorTracker : IFavorTracker, IDisposable
{
    public DeityDomain DeityDomain => DeityDomain.Timber;

    private readonly HashSet<string> _timberFollowers = new();
    private readonly Dictionary<string, int> _pendingTreeFells = new();  // Track multi-block fells

    public void Initialize()
    {
        _eventService.OnBreakBlock(OnBlockBroken);
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, float dropQuantityMultiplier)
    {
        var block = _worldService.GetBlock(blockSel.Position);
        if (!IsLogBlock(block)) return;
        if (!_timberFollowers.Contains(player.PlayerUID)) return;

        // Detect if this is part of a tree fell or single log
        var logsInTree = CountConnectedLogs(blockSel.Position);
        var favor = CalculateTreeFellFavor(block, logsInTree);

        _favorSystem.AwardFavor(player.PlayerUID, favor, "Tree Felling");
    }

    private bool IsLogBlock(Block block)
    {
        return block.Code.Path.Contains("log-") ||
               block.BlockMaterial == EnumBlockMaterial.Wood;
    }
}
```

#### Step 2.2: WoodcraftingFavorTracker

**File:** `DivineAscension/Systems/Favor/WoodcraftingFavorTracker.cs`

**Purpose:** Awards favor for crafting wood-based items.

**Favor Values:**
| Craft | Favor |
|-------|-------|
| Planks (per stack) | 1 |
| Wooden tools | 2 |
| Wooden furniture | 4 |
| Wooden containers (chests, barrels) | 5 |
| Complex wood items (boats, carts) | 10 |
| Charcoal production | 3 |

**Events to Hook:**
- Grid crafting completion for wood recipes
- Sawing station outputs
- Charcoal pit completion

#### Step 2.3: SawmillFavorTracker

**File:** `DivineAscension/Systems/Favor/SawmillFavorTracker.cs`

**Purpose:** Awards favor for processing logs at sawing stations.

**Favor Values:**
| Activity | Favor |
|----------|-------|
| Log to planks | 2 |
| Log to boards | 3 |
| Firewood splitting | 1 |

**Events to Hook:**
- Block entity state changes for sawing blocks
- Need Harmony patch for saw usage completion

---

### Phase 3: Blessings

#### Step 3.1: Create Blessing Definition File

**File:** `DivineAscension/assets/divineascension/config/blessings/timber.json`

```json
{
  "domain": "Timber",
  "version": 1,
  "blessings": [
    {
      "blessingId": "sylvanus_woodsmans_arm",
      "name": "Woodsman's Arm",
      "description": "+15% axe damage to trees, +10% chopping speed.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "axe-swing",
      "requiredFavorRank": 0,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": [],
      "statModifiers": {
        "axeTreeDamage": 0.15,
        "choppingSpeed": 0.10
      },
      "specialEffects": []
    },
    {
      "blessingId": "sylvanus_timber_yield",
      "name": "Bountiful Timber",
      "description": "+20% log drops from felled trees.",
      "kind": "Player",
      "category": "Production",
      "iconName": "log-pile",
      "requiredFavorRank": 1,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["sylvanus_woodsmans_arm"],
      "statModifiers": {
        "logDropRate": 0.20
      },
      "specialEffects": []
    },
    {
      "blessingId": "sylvanus_effortless_saw",
      "name": "Effortless Saw",
      "description": "+25% sawing speed, -15% saw durability loss.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "saw-blade",
      "requiredFavorRank": 2,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["sylvanus_timber_yield"],
      "statModifiers": {
        "sawingSpeed": 0.25,
        "sawDurability": 0.15
      },
      "specialEffects": []
    },
    {
      "blessingId": "sylvanus_forest_sense",
      "name": "Forest Sense",
      "description": "Nearby trees are highlighted. Can sense rare wood types at distance.",
      "kind": "Player",
      "category": "Utility",
      "iconName": "tree-vision",
      "requiredFavorRank": 2,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["sylvanus_woodsmans_arm"],
      "statModifiers": {},
      "specialEffects": ["tree_detection_aura"]
    },
    {
      "blessingId": "sylvanus_mighty_fell",
      "name": "Mighty Fell",
      "description": "Chance to fell entire tree with one swing. Works on larger trees.",
      "kind": "Player",
      "category": "Production",
      "iconName": "falling-tree",
      "requiredFavorRank": 3,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["sylvanus_timber_yield"],
      "statModifiers": {
        "instantTreeFellChance": 0.15
      },
      "specialEffects": ["enhanced_tree_felling"]
    },
    {
      "blessingId": "sylvanus_master_carpenter",
      "name": "Master Carpenter",
      "description": "+30% wood crafting yield, wooden items have +20% durability.",
      "kind": "Player",
      "category": "Production",
      "iconName": "carpenter-tools",
      "requiredFavorRank": 4,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["sylvanus_effortless_saw", "sylvanus_mighty_fell"],
      "statModifiers": {
        "woodCraftYield": 0.30,
        "woodenItemDurability": 0.20
      },
      "specialEffects": []
    },
    {
      "blessingId": "sylvanus_sacred_grove",
      "name": "Sacred Grove",
      "description": "Saplings planted by religion members grow 50% faster.",
      "kind": "Religion",
      "category": "Production",
      "iconName": "growing-sapling",
      "requiredFavorRank": 0,
      "requiredPrestigeRank": 2,
      "prerequisiteBlessings": [],
      "statModifiers": {
        "saplingGrowthRate": 0.50
      },
      "specialEffects": []
    },
    {
      "blessingId": "sylvanus_living_wood",
      "name": "Living Wood",
      "description": "Wooden structures built by religion resist decay and fire.",
      "kind": "Religion",
      "category": "Defense",
      "iconName": "enchanted-wood",
      "requiredFavorRank": 0,
      "requiredPrestigeRank": 3,
      "prerequisiteBlessings": ["sylvanus_sacred_grove"],
      "statModifiers": {
        "woodDecayResistance": 0.75,
        "woodFireResistance": 0.50
      },
      "specialEffects": []
    }
  ]
}
```

#### Step 3.2: Update BlessingLoader

**File:** `DivineAscension/Services/BlessingLoader.cs`

```csharp
private static readonly string[] DomainFiles = { "craft", "wild", "conquest", "harvest", "husbandry", "timber", "stone" };
```

---

### Phase 4: Effect Handlers

#### Step 4.1: Create SylvanusEffectHandlers

**File:** `DivineAscension/Systems/BlessingEffects/Handlers/SylvanusEffectHandlers.cs`

```csharp
public class TreeDetectionAuraEffect : ISpecialEffectHandler
{
    public string EffectId => "tree_detection_aura";

    public void ActivateForPlayer(IServerPlayer player)
    {
        // Add particle effects or UI indicators for nearby trees
        // Highlight rare wood types
    }

    public void OnTick(float deltaTime)
    {
        // Scan for trees within range and update highlights
    }
}

public class EnhancedTreeFellingEffect : ISpecialEffectHandler
{
    public string EffectId => "enhanced_tree_felling";

    public void OnBlockBreak(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier)
    {
        // Check for instant tree fell proc
        // If proc, break all connected log blocks
    }
}
```

---

### Phase 5: Harmony Patches

#### Step 5.1: TreeFellingPatches

**File:** `DivineAscension/Systems/Patches/TreeFellingPatches.cs`

Hook into tree felling mechanics:

```csharp
[HarmonyPatch]
public static class TreeFellingPatches
{
    public static event Action<IServerPlayer, Block, int>? OnTreeFelled;

    // Patch the tree felling behavior to capture full tree info
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockBehaviorUnstableFalling), "OnBlockBroken")]
    public static void Postfix_OnBlockBroken(
        BlockBehaviorUnstableFalling __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer)
    {
        if (byPlayer is IServerPlayer serverPlayer)
        {
            // Count logs that fell
            // Raise OnTreeFelled event
        }
    }
}
```

#### Step 5.2: WoodcraftingPatches

**File:** `DivineAscension/Systems/Patches/WoodcraftingPatches.cs`

Hook into crafting completion for wood recipes:

```csharp
[HarmonyPatch]
public static class WoodcraftingPatches
{
    public static event Action<IServerPlayer, ItemStack, int>? OnWoodItemCrafted;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GridRecipe), "ConsumeInput")]  // or appropriate method
    public static void Postfix_ConsumeInput(GridRecipe __instance, IPlayer byPlayer, ItemSlot[] inputSlots)
    {
        if (!IsWoodRecipe(__instance)) return;
        if (byPlayer is IServerPlayer serverPlayer)
        {
            OnWoodItemCrafted?.Invoke(serverPlayer, __instance.Output.ResolvedItemstack, 1);
        }
    }
}
```

#### Step 5.3: SawingPatches

**File:** `DivineAscension/Systems/Patches/SawingPatches.cs`

Hook into saw station processing.

---

### Phase 6: FavorSystem Integration

#### Step 6.1: Update FavorSystem

**File:** `DivineAscension/Systems/FavorSystem.cs`

Add tracker fields:
```csharp
private TreeFellingFavorTracker? _treeFellingFavorTracker;
private WoodcraftingFavorTracker? _woodcraftingFavorTracker;
private SawmillFavorTracker? _sawmillFavorTracker;
```

Initialize in `Initialize()`:
```csharp
_treeFellingFavorTracker = new TreeFellingFavorTracker(...);
_treeFellingFavorTracker.Initialize();

_woodcraftingFavorTracker = new WoodcraftingFavorTracker(...);
_woodcraftingFavorTracker.Initialize();

_sawmillFavorTracker = new SawmillFavorTracker(...);
_sawmillFavorTracker.Initialize();

_logger.Notification("[DivineAscension] Initialized 16 favor trackers");  // Update count (13 + 3)
```

Dispose in `Dispose()`:
```csharp
_treeFellingFavorTracker?.Dispose();
_woodcraftingFavorTracker?.Dispose();
_sawmillFavorTracker?.Dispose();
```

---

### Phase 7: Assets

#### Step 7.1: Domain Icon

**File:** `DivineAscension/assets/divineascension/textures/icons/deities/timber.png`

32x32 PNG icon representing forestry theme (tree, axe, log stack, etc.)

#### Step 7.2: Blessing Icons

**Directory:** `DivineAscension/assets/divineascension/textures/icons/perks/timber/`

Create icons for each blessing:
- `axe-swing.png`
- `log-pile.png`
- `saw-blade.png`
- `tree-vision.png`
- `falling-tree.png`
- `carpenter-tools.png`
- `growing-sapling.png`
- `enchanted-wood.png`

---

### Phase 8: Localization

#### Step 8.1: Update Language Files

**File:** `DivineAscension/assets/divineascension/lang/en.json`

Add entries for:
- Domain name and description
- All blessing names and descriptions
- Favor activity messages ("Tree Felling", "Woodcrafting", "Sawing")

---

## Testing Requirements

### Unit Tests

1. **TreeFellingFavorTrackerTests**
   - Verify favor awarded on tree fell
   - Verify tree size calculation (logs counted)
   - Verify rare wood bonus
   - Verify no favor for non-followers

2. **WoodcraftingFavorTrackerTests**
   - Verify favor for wood item crafting
   - Verify different item types award different favor
   - Verify follower filtering

3. **SawmillFavorTrackerTests**
   - Verify favor for log processing
   - Verify different output types

4. **TimberBlessingTests**
   - Verify blessing definitions load correctly
   - Verify prerequisite chains
   - Verify stat modifiers apply

### Integration Tests

1. Religion creation with Timber domain
2. Civilization formation with Timber religion
3. Blessing unlock flow for Timber blessings
4. Tree detection aura effect

---

## Files Changed Summary

| Category | Files | Action |
|----------|-------|--------|
| Enum | `Models/Enum/DeityDomain.cs` | Modify |
| UI Helpers | `GUI/UI/Utilities/DomainHelper.cs` | Modify |
| UI Helpers | `GUI/UI/Utilities/DeityInfoHelper.cs` | Modify |
| Favor Trackers | `Systems/Favor/TreeFellingFavorTracker.cs` | Create |
| Favor Trackers | `Systems/Favor/WoodcraftingFavorTracker.cs` | Create |
| Favor Trackers | `Systems/Favor/SawmillFavorTracker.cs` | Create |
| Systems | `Systems/FavorSystem.cs` | Modify |
| Blessings | `assets/.../config/blessings/timber.json` | Create |
| Services | `Services/BlessingLoader.cs` | Modify |
| Effect Handlers | `Systems/BlessingEffects/Handlers/SylvanusEffectHandlers.cs` | Create |
| Patches | `Systems/Patches/TreeFellingPatches.cs` | Create |
| Patches | `Systems/Patches/WoodcraftingPatches.cs` | Create |
| Patches | `Systems/Patches/SawingPatches.cs` | Create |
| Assets | `assets/.../textures/icons/deities/timber.png` | Create |
| Assets | `assets/.../textures/icons/perks/timber/*.png` | Create (8) |
| Localization | `assets/.../lang/en.json` | Modify |
| Tests | `Tests/Systems/Favor/TreeFellingFavorTrackerTests.cs` | Create |
| Tests | `Tests/Systems/Favor/WoodcraftingFavorTrackerTests.cs` | Create |
| Tests | `Tests/Systems/Favor/SawmillFavorTrackerTests.cs` | Create |

**Total:** ~21 files (9 new, 12 modified)

---

## Risks & Considerations

1. **Tree Felling Detection**: Vintage Story's tree felling can be complex (physics-based falling, multiple log types). Need careful detection of "complete tree" vs "partial harvest."

2. **Performance**: Tree felling can cascade many block breaks - ensure tracker handles batched events efficiently.

3. **Balance vs Harvest Domain**: Timber focuses on wood/trees while Harvest focuses on crops. Clear distinction:
   - Timber: Trees, logs, planks, wooden items
   - Harvest: Crops, cooking, farming

4. **Mod Compatibility**: May conflict with tree-modifying mods. Test with popular forestry mods.

5. **Rare Wood Types**: Need to catalog all rare wood types in Vintage Story for bonus calculations:
   - Kapok
   - Aged wood
   - Petrified wood
   - Possibly mod-added woods

---

## Comparison: Timber vs Wild Domain

To avoid overlap with the Wild domain (which covers foraging/hunting):

| Activity | Wild Domain | Timber Domain |
|----------|-------------|---------------|
| Hunting animals | ✓ | ✗ |
| Foraging berries/mushrooms | ✓ | ✗ |
| Chopping trees | ✗ | ✓ |
| Processing logs | ✗ | ✓ |
| Crafting wood items | ✗ | ✓ |
| Gathering sticks | ✓ | ✗ |
| Planting saplings | ✗ | ✓ |

Wild is about harvesting from nature without changing it; Timber is about actively managing and processing forest resources.
