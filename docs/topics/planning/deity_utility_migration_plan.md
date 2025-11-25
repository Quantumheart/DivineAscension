# Deity System Migration Plan: Combat to Utility Focus

## Executive Summary

This plan details the migration from an 8-deity combat-focused system to a 4-deity utility-focused system based on the `deity_reference_utility.md` specification. The migration follows a **phased, one-deity-at-a-time approach** to minimize risk and allow for thorough testing between phases.

**Key Decisions:**
- ✅ Remove 4 unused deities completely (Morthen, Umbros, Tharos, Vex)
- ✅ Fresh start - no data migration needed
- ✅ One deity at a time implementation
- ✅ Full implementation of activity-based favor earning systems

**Implementation Order:**
1. **Phase 0**: Cleanup - Remove unused deities (3 days)
2. **Phase 1**: Khoras - Forge/Craft (2-3 weeks)
3. **Phase 2**: Lysa - Hunt/Wild (2 weeks)
4. **Phase 3**: Aethra - Agriculture/Light (2 weeks)
5. **Phase 4**: Gaia - Earth/Stone (1.5 weeks)

**Total Timeline**: 8-9 weeks

---

## Phase 0: Remove Unused Deities

**Duration**: 3 days
**Goal**: Clean removal of 4 unused deities to establish clean foundation

### Tasks

#### 1. Update DeityType Enum
**File**: `PantheonWars/Models/Enum/DeityType.cs`

Remove lines 11, 13, 14, 16:
```csharp
// REMOVE:
Morthen = 3,  // Death
Umbros = 5,   // Shadows
Tharos = 6,   // Storms
Vex = 8       // Madness

// KEEP:
None = 0,
Khoras = 1,   // War → Forge (will update in Phase 1)
Lysa = 2,     // Hunt → Wild (will update in Phase 2)
Aethra = 4,   // Light → Agriculture (will update in Phase 3)
Gaia = 7      // Earth → Stone (will update in Phase 4)
```

#### 2. Remove Blessing Definitions
**File**: `PantheonWars/Systems/BlessingDefinitions.cs`

- Delete methods: `GetMorthenBlessings()`, `GetUmbrosBlessings()`, `GetTharosBlessings()`, `GetVexBlessings()`
- Update `GetAllBlessings()` to only call 4 deity methods
- Delete approximately 1100 lines of blessing definitions for removed deities

#### 3. Update BlessingIds Constants
**File**: `PantheonWars/Constants/BlessingIds.cs`

- Remove all blessing ID constants for Morthen, Umbros, Tharos, Vex
- Keep only Khoras, Lysa, Aethra, Gaia blessing IDs (will update per phase)

#### 4. Archive Documentation
- Move `docs/topics/reference/deity_reference.md` → `deity_reference_combat_legacy.md`
- Move `docs/topics/reference/blessing_reference.md` → `blessing_reference_combat_legacy.md`
- Add "DEPRECATED - Combat System" header to archived docs

#### 5. Search and Clean
- Search codebase for any references to removed deities
- Update GUI components that may reference all 8 deities
- Update tests that reference removed deities

### Verification
- [ ] `dotnet build` completes with 0 errors
- [ ] All existing tests pass
- [ ] Search for "Morthen|Umbros|Tharos|Vex" returns 0 results

---

## Phase 1: Khoras - God of Forge & Craft

**Duration**: 2-3 weeks
**Goal**: Complete first utility-focused deity with activity-based favor tracking

**New Identity**: God of the Forge & Craft (not War)
**Focus**: Tool durability, ore efficiency, cold resistance, crafting

### Part A: Redesign Blessings

#### Player Blessings (6 Total)

**Tier 1 - Craftsman's Touch** (0-499 favor)
- Tools/weapons lose durability 10% slower
- +10% ore yield when mining
- +3°C cold resistance

**Tier 2A - Masterwork Tools** (500-1999, Utility Path)
- Tools last 15% longer (total: 25%)
- Mining/chopping speed +8%
- Tool repair costs -15% materials

**Tier 2B - Forgeborn Endurance** (500-1999, Survival Path)
- +5°C cold resistance (total: 8°C)
- +10% max health
- +10% armor from metal equipment

**Tier 3A - Legendary Smith** (2000-4999, Utility Specialization)
- Tools last 20% longer (total: 45%)
- +15% ore yield (total: 25%)
- 10% chance to save materials when smithing
- Tool repairs restore +25% more durability

**Tier 3B - Unyielding** (2000-4999, Survival Specialization)
- +7°C cold resistance (total: 15°C)
- +15% max health (total: 25%)
- +15% armor from all equipment (total: 25%)
- Hunger/satiety depletes 8% slower

**Tier 4 - Avatar of the Forge** (5000+, Capstone)
- Tools repair 1 durability per 5 minutes in inventory
- -10% material costs for smithing
- Mining/chopping speed +12% (total: 20% if Path A)
- Requires both Tier 3A and 3B

#### Religion Blessings (4 Total)

**Tier 1 - Shared Workshop** (0-499 prestige)
- All members: +8% tool durability, +8% ore yield

**Tier 2 - Guild of Smiths** (500-1999 prestige)
- All members: +12% tool durability, +12% ore yield, +4°C cold resistance

**Tier 3 - Master Craftsmen** (2000-4999 prestige)
- All members: +18% tool durability, +15% ore yield, +6°C cold resistance, -10% repair costs

**Tier 4 - Pantheon of Creation** (5000+ prestige)
- All members: +25% tool durability, +20% ore yield, +8°C cold resistance, +10% mining/chopping speed, passive repair (1/10min)

#### Implementation Tasks
- [ ] Replace `GetKhorasBlessings()` method in `BlessingDefinitions.cs`
- [ ] Update blessing IDs in `BlessingIds.cs`
- [ ] Add new stat modifiers to `VintageStoryStats.cs`:
  - `ToolDurability`, `OreYield`, `ColdResistance`, `MiningSpeed`, `ChoppingSpeed`
  - `RepairCostReduction`, `RepairEfficiency`, `SmithingCostReduction`
  - `MetalArmorBonus`, `HungerRate`
- [ ] Update `BlessingEffectSystem.cs` to apply new stat types
- [ ] Create blessing unit tests

### Part B: Activity-Based Favor Tracking

**New Favor Sources:**
1. Mining ore: 2 favor per ore block
2. Smithing items: 5-15 favor per craft
3. Repairing tools: 1-5 favor per repair
4. PvP kills: Keep existing (10 favor base)
5. Passive: Reduce from 2.0/hour → 0.5/hour

#### Architecture

Create new favor tracking interface and implementation:

**New File**: `PantheonWars/Systems/Favor/IFavorTracker.cs`
```csharp
public interface IFavorTracker
{
    void Initialize();
    DeityType DeityType { get; }
}
```

**New File**: `PantheonWars/Systems/Favor/KhorasFavorTracker.cs`

Key responsibilities:
- Hook into `BlockBreakEvent` to detect ore mining
- Maintain whitelist of ore block codes (copper, tin, iron, silver, gold, meteorite)
- Award 2 favor per ore block
- Hook into crafting completion events for smithing
- Calculate favor based on item complexity (5-15 range)
- Hook into tool repair events
- Only process events for players following Khoras

Implementation details:
```csharp
private readonly HashSet<string> _oreBlocks = new()
{
    "ore-poor-copper", "ore-medium-copper", "ore-rich-copper",
    "ore-poor-tin", "ore-medium-tin", "ore-rich-tin",
    "ore-poor-iron", "ore-medium-iron", "ore-rich-iron",
    "ore-poor-silver", "ore-medium-silver", "ore-rich-silver",
    "ore-poor-gold", "ore-medium-gold", "ore-rich-gold",
    "ore-meteorite"
};

private void OnBlockBroken(IServerPlayer player, int oldBlockId, BlockSelection blockSel)
{
    var religionData = _playerReligionDataManager.GetPlayerReligionData(player.PlayerUID);
    if (religionData?.ActiveDeity != DeityType.Khoras) return;

    var block = _sapi.World.GetBlock(oldBlockId);
    if (IsOreBlock(block))
    {
        _favorSystem.AwardFavorForAction(player, "mining ore", 2);
    }
}
```

#### FavorSystem Integration

**File**: `PantheonWars/Systems/FavorSystem.cs`

- Add deity-specific tracker registration
- Create `AwardFavorForAction()` public method
- Reduce `BASE_FAVOR_PER_HOUR` constant from 2.0 to 0.5
- Initialize Khoras tracker in `Initialize()` method

#### Implementation Tasks
- [ ] Create `IFavorTracker` interface
- [ ] Create `KhorasFavorTracker.cs`
- [ ] Implement ore block detection and favor award
- [ ] Research Vintage Story crafting events API
- [ ] Implement smithing favor calculation
- [ ] Research tool repair event hooks
- [ ] Add `AwardFavorForAction()` to `FavorSystem.cs`
- [ ] Reduce passive favor rate
- [ ] Add favor award chat messages with deity name

### Part C: Special Effects System

Create extensible system for blessing special effects that go beyond stat modifiers.

**New File**: `PantheonWars/Systems/BlessingEffects/SpecialEffectRegistry.cs`

Handle special effects like:
- `passive_tool_repair_1per5min` - Tools repair over time
- `material_save_chance_10` - Chance to save materials when crafting

**New File**: `PantheonWars/Systems/BlessingEffects/Handlers/KhorasEffectHandlers.cs`

Implement Khoras-specific effects:
- Passive tool repair (tick every 5 minutes, restore 1 durability)
- Material saving on smithing (10% chance hook)

#### Implementation Tasks
- [ ] Create `SpecialEffectRegistry.cs`
- [ ] Create effect handler architecture
- [ ] Implement passive tool repair system
- [ ] Implement material saving system
- [ ] Hook into game tick for passive effects
- [ ] Test all special effects

### Part D: Testing

#### Unit Tests
- [ ] `KhorasFavorTrackerTests.cs`:
  - Ore mining awards 2 favor
  - Non-ore blocks award 0 favor
  - Non-Khoras players get 0 favor
  - All ore types work correctly
- [ ] `KhorasBlessingTests.cs`:
  - All blessings apply correct stat modifiers
  - Bonuses stack additively
  - Prerequisites enforce correctly
  - Capstone requires both Tier 3 paths
- [ ] `KhorasEffectHandlerTests.cs`:
  - Passive repair works correctly
  - Material saving triggers at expected rate

#### Integration Tests
- [ ] Full progression test: mine ore → earn favor → unlock blessings → bonuses apply
- [ ] Religion blessing test: all members receive bonuses
- [ ] Stacking test: player + religion bonuses combine correctly

#### Manual Testing
- [ ] Mine various ore types, verify favor awards
- [ ] Craft smithing items, verify favor awards
- [ ] Progress through all blessing tiers
- [ ] Verify stat bonuses apply in-game
- [ ] Test passive tool repair
- [ ] Test material saving
- [ ] Performance test: mine 1000 blocks, check for lag

### Risks & Mitigation

**Risk**: Block break event performance overhead
**Mitigation**: Early return if not Khoras follower, cache player data, profile performance

**Risk**: Favor farming exploits (spam craft nails)
**Mitigation**: Implement diminishing returns per item type per hour, lower favor for cheap items

**Risk**: Vintage Story may not have crafting/repair events
**Mitigation**: Research API thoroughly, create custom event hooks if needed, defer repair favor to Phase 2 if necessary

---

## Phase 2: Lysa - Goddess of Hunt & Wild

**Duration**: 2 weeks
**Goal**: Second utility deity with hunting/gathering focus

**New Identity**: Goddess of the Hunt & Wild (expansion of current Hunt theme)
**Focus**: Foraging, hunting, movement, wilderness survival

### Part A: Redesign Blessings

Follow same structure as Khoras but with hunting/gathering themes:

#### Player Blessings (6)
- Tier 1: Hunter's Instinct (double harvest chance, movement speed, animal tracking)
- Tier 2A: Master Forager (gathering path)
- Tier 2B: Apex Predator (hunting path)
- Tier 3A: Abundance of the Wild
- Tier 3B: Silent Death
- Tier 4: Avatar of the Wild (both paths required)

#### Religion Blessings (4)
- Tier 1: Hunting Party
- Tier 2: Wilderness Tribe
- Tier 3: Children of the Forest
- Tier 4: Pantheon of the Hunt

#### Implementation Tasks
- [ ] Replace `GetLysaBlessings()` in `BlessingDefinitions.cs`
- [ ] Update blessing IDs
- [ ] Add stat modifiers: `DoubleHarvestChance`, `MovementSpeed`, `AnimalDamage`, `AnimalDrops`, `FoodSpoilage`, `Satiety`, `TemperatureResistance`

### Part B: Activity-Based Favor Tracking

**New File**: `PantheonWars/Systems/Favor/LysaFavorTracker.cs`

**New Favor Sources:**
1. Hunting animals: 3-20 favor (wolf=12, deer=8, rabbit=3)
2. Foraging plants: 0.5 favor per harvest
3. Exploring new chunks: 2 favor per chunk
4. PvP kills: Existing system
5. Passive: 0.5/hour

#### Implementation Complexity
- Animal kill detection (hook into entity death events)
- Animal type identification (maintain animal favor table)
- Plant harvest detection (hook into block harvest events)
- Chunk exploration tracking (maintain visited chunks per player)

#### Implementation Tasks
- [ ] Create `LysaFavorTracker.cs`
- [ ] Implement animal kill detection
- [ ] Create animal favor value table
- [ ] Implement foraging detection (berries, mushrooms, etc.)
- [ ] Create chunk exploration tracker
- [ ] Hook into movement/chunk load events
- [ ] Register tracker in `FavorSystem.cs`

### Part C: Special Effects
- Animal tracking highlights
- Compass always visible
- Food spoilage reduction
- Temperature resistance (both hot and cold)

#### Implementation Tasks
- [ ] Create `LysaEffectHandlers.cs`
- [ ] Implement animal tracking visualization
- [ ] Implement compass visibility override
- [ ] Implement food spoilage modifier
- [ ] Implement temperature resistance system

### Part D: Testing
- [ ] Unit tests for all favor sources
- [ ] Integration tests for progression
- [ ] Manual testing: hunt animals, forage, explore
- [ ] Performance testing with many entities

---

## Phase 3: Aethra - Goddess of Light & Agriculture

**Duration**: 2 weeks
**Goal**: Third utility deity with farming/cooking focus

**New Identity**: Goddess of Light & Agriculture (expanded from just Light)
**Focus**: Crop yields, cooking, food satiety, heat resistance

### Part A: Redesign Blessings

#### Player Blessings (6)
- Tier 1: Sun's Blessing
- Tier 2A: Bountiful Harvest (agriculture path)
- Tier 2B: Baker's Touch (cooking path)
- Tier 3A: Master Farmer
- Tier 3B: Divine Kitchen
- Tier 4: Avatar of Abundance

#### Religion Blessings (4)
- Tier 1: Community Farm
- Tier 2: Harvest Festival
- Tier 3: Land of Plenty
- Tier 4: Pantheon of Light (includes Sacred Granary structure)

#### Implementation Tasks
- [ ] Replace `GetAethraBlessings()`
- [ ] Add stat modifiers: `CropYield`, `SeedDropChance`, `CookingYield`, `HeatResistance`, `RareCropChance`
- [ ] Update blessing IDs

### Part B: Activity-Based Favor Tracking

**New File**: `PantheonWars/Systems/Favor/AethraFavorTracker.cs`

**New Favor Sources:**
1. Harvesting crops: 1 favor per harvest
2. Cooking meals: 3-8 favor per meal
3. Planting crops: 0.5 favor per plant
4. PvP kills: Existing
5. Passive: 0.5/hour

#### Implementation Complexity
- Crop harvest detection (hook into block harvest)
- Crop type identification (wheat, flax, vegetables, etc.)
- Cooking detection (meal crafting events)
- Meal complexity calculation (simple bread vs complex stew)
- Planting detection (farmland interaction events)

#### Implementation Tasks
- [ ] Create `AethraFavorTracker.cs`
- [ ] Implement crop harvest detection
- [ ] Create crop favor table
- [ ] Implement cooking detection
- [ ] Calculate meal complexity favor
- [ ] Implement planting detection
- [ ] Register tracker

### Part C: Special Effects
- Blessed meals (temporary buffs)
- Never malnourished
- Rare crop variant finding
- Sacred Granary structure (religion-wide)

#### Implementation Tasks
- [ ] Create `AethraEffectHandlers.cs`
- [ ] Implement blessed meal system
- [ ] Implement malnutrition prevention
- [ ] Implement rare crop discovery
- [ ] Design Sacred Granary structure

### Part D: Testing
- [ ] Unit tests for crop/cooking favor
- [ ] Integration tests
- [ ] Manual: farm crops, cook meals, plant seeds
- [ ] Test blessed meal buffs

---

## Phase 4: Gaia - Goddess of Earth & Stone

**Duration**: 1.5 weeks
**Goal**: Final utility deity with mining/building focus

**New Identity**: Goddess of Earth & Stone (same theme)
**Focus**: Stone/clay yields, building, physical resilience, fall damage reduction

### Part A: Redesign Blessings

#### Player Blessings (6)
- Tier 1: Earthen Foundation
- Tier 2A: Quarryman (resource path)
- Tier 2B: Mountain's Endurance (survival path)
- Tier 3A: Master Quarryman
- Tier 3B: Unshakeable
- Tier 4: Avatar of Earth

#### Religion Blessings (4)
- Tier 1: Stone Circle
- Tier 2: Earth Wardens
- Tier 3: Mountain's Children
- Tier 4: Pantheon of Stone

#### Implementation Tasks
- [ ] Replace `GetGaiaBlessings()`
- [ ] Add stat modifiers: `StoneYield`, `ClayYield`, `PickDurability`, `FallDamageReduction`, `RareStoneChance`
- [ ] Update blessing IDs

### Part B: Activity-Based Favor Tracking

**New File**: `PantheonWars/Systems/Favor/GaiaFavorTracker.cs`

**New Favor Sources:**
1. Mining stone/clay: 0.3 favor per block
2. Building structures: 2-10 favor per structure
3. Quarrying: 1 favor per stone type
4. PvP kills: Existing
5. Passive: 0.5/hour

#### Implementation Complexity
- Stone/clay detection (many block variants)
- Building detection (structure placement complexity)
- Quarrying vs regular mining distinction

#### Implementation Tasks
- [ ] Create `GaiaFavorTracker.cs`
- [ ] Implement stone/clay mining detection
- [ ] Create stone type whitelist (granite, basalt, etc.)
- [ ] Implement building structure detection
- [ ] Register tracker

### Part C: Special Effects
- Fall damage reduction
- Overburdened immunity (first tier)
- Rare stone finding
- Health/armor bonuses

#### Implementation Tasks
- [ ] Create `GaiaEffectHandlers.cs`
- [ ] Implement fall damage modifier
- [ ] Implement overburdened immunity
- [ ] Implement rare stone discovery

### Part D: Testing
- [ ] Unit tests for stone/building favor
- [ ] Integration tests
- [ ] Manual: mine stone, build structures, test fall damage
- [ ] Performance testing

---

## Cross-Cutting Concerns

### Stat Modifier System Expansion

**File**: `PantheonWars/Constants/VintageStoryStats.cs`

Add new stat constants for all utility bonuses. Current system uses Vintage Story's stat modifier API.

New stats needed:
- Tool/equipment: `ToolDurability`, `RepairCostReduction`, `RepairEfficiency`
- Mining/gathering: `OreYield`, `MiningSpeed`, `ChoppingSpeed`, `StoneYield`, `ClayYield`
- Farming: `CropYield`, `SeedDropChance`, `RareCropChance`, `CookingYield`
- Hunting: `DoubleHarvestChance`, `AnimalDamage`, `AnimalDrops`
- Survival: `ColdResistance`, `HeatResistance`, `HungerRate`, `FoodSpoilage`, `FallDamageReduction`
- Combat: Keep existing melee/armor stats for PvP relevance

### Blessing Effect System Architecture

**Files**:
- `PantheonWars/Systems/BlessingEffects/SpecialEffectRegistry.cs`
- `PantheonWars/Systems/BlessingEffects/Handlers/*.cs`

Create registry that maps effect IDs to handler functions:
```csharp
_effectHandlers["passive_tool_repair_1per5min"] = RepairToolsPassively;
_effectHandlers["material_save_chance_10"] = SaveMaterialsOnCraft;
_effectHandlers["compass_always_visible"] = ShowCompass;
// etc.
```

Integrate with `BlessingEffectSystem.cs` to automatically invoke handlers when blessings are unlocked.

### Documentation Updates

- [ ] Update `deity_reference_utility.md` with implementation notes
- [ ] Create player-facing guide: "Getting Started with Deities"
- [ ] Document all favor earning rates and progression
- [ ] Create admin guide for balance adjustments
- [ ] Update changelog with major redesign notes

### Performance Optimization

Each favor tracker hooks into game events that fire frequently. Optimize:
- Early returns if player not following deity (check cached value)
- Batch favor awards (accumulate per player, award every N actions)
- Use efficient data structures (HashSet for block whitelists)
- Profile each tracker under load

### Balance Testing

After all phases complete:
- [ ] Progression pacing: verify players can reach Champion in reasonable time
- [ ] Favor source balance: activities vs PvP contribution
- [ ] Blessing power: verify bonuses meaningful but not overpowered
- [ ] Religion prestige: verify group progression feels rewarding
- [ ] Cross-deity balance: all deities equally attractive

---

## Success Criteria

### Functional
- [ ] 4 deities fully implemented with utility focus
- [ ] 40 blessings total (10 per deity) all functional
- [ ] Activity-based favor earning for all deities
- [ ] All special effects working
- [ ] Dual-path progression intact
- [ ] Player + Religion bonuses stack correctly

### Technical
- [ ] 0 compilation errors
- [ ] 90%+ unit test coverage on new code
- [ ] All integration tests passing
- [ ] No performance degradation (< 5% overhead)
- [ ] Clean git history (1 PR per phase)

### Documentation
- [ ] Player documentation complete
- [ ] Developer documentation with code comments
- [ ] Migration guide for server admins
- [ ] Changelog updated

---

## Risk Management

### Performance Risks
**Risk**: Event handlers cause server lag
**Mitigation**: Profile each tracker, optimize hot paths, implement caching

### Balance Risks
**Risk**: Favor farming exploits
**Mitigation**: Diminishing returns, rate limiting, favor caps per activity type

### Technical Risks
**Risk**: Vintage Story API may not have needed event hooks
**Mitigation**: Research API thoroughly before each phase, create custom hooks if needed

### Schedule Risks
**Risk**: Phases take longer than estimated
**Mitigation**: Buffer time in estimates, prioritize core features over nice-to-haves

---

## Critical Files Reference

### Files to Modify
1. `PantheonWars/Models/Enum/DeityType.cs` - Remove 4 deities
2. `PantheonWars/Systems/BlessingDefinitions.cs` - Replace all 40 blessings
3. `PantheonWars/Constants/BlessingIds.cs` - Update blessing ID constants
4. `PantheonWars/Systems/FavorSystem.cs` - Integrate deity trackers, reduce passive rate
5. `PantheonWars/Systems/BlessingEffectSystem.cs` - Apply new stat types
6. `PantheonWars/Constants/VintageStoryStats.cs` - Add utility stat constants

### Files to Create
1. `PantheonWars/Systems/Favor/IFavorTracker.cs` - Tracker interface
2. `PantheonWars/Systems/Favor/KhorasFavorTracker.cs` - Khoras activities
3. `PantheonWars/Systems/Favor/LysaFavorTracker.cs` - Lysa activities
4. `PantheonWars/Systems/Favor/AethraFavorTracker.cs` - Aethra activities
5. `PantheonWars/Systems/Favor/GaiaFavorTracker.cs` - Gaia activities
6. `PantheonWars/Systems/BlessingEffects/SpecialEffectRegistry.cs` - Effect registry
7. `PantheonWars/Systems/BlessingEffects/Handlers/KhorasEffectHandlers.cs`
8. `PantheonWars/Systems/BlessingEffects/Handlers/LysaEffectHandlers.cs`
9. `PantheonWars/Systems/BlessingEffects/Handlers/AethraEffectHandlers.cs`
10. `PantheonWars/Systems/BlessingEffects/Handlers/GaiaEffectHandlers.cs`

### Files to Archive
1. `docs/topics/reference/deity_reference.md` → `deity_reference_combat_legacy.md`
2. `docs/topics/reference/blessing_reference.md` → `blessing_reference_combat_legacy.md`

---

## Implementation Notes

- Each phase should be a separate git branch and PR
- Run full test suite before merging each phase
- Manual playtesting required for each deity before moving to next
- Balance adjustments expected after initial implementation
- Keep `deity_reference_utility.md` as single source of truth for all stat values
