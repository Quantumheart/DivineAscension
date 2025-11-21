# Religion-Only Blessing System Redesign

**Document Version:** 2.1
**Created:** November 20, 2025
**Updated:** November 21, 2025
**Status:** MVP 1 In Progress (75% Complete), Phase 2 Cleanup Planned
**Impact:** Major Architecture Change

---

## Implementation Progress

### MVP 1 Week 1-2: Core Cleanup ✅ COMPLETE
- [x] Remove `/deity` and `/favor` commands
- [x] Remove `DeityCommands.cs`, `FavorCommands.cs`
- [x] Remove `FavorSystem.cs`, `PvPManager.cs`
- [x] Remove `DeitySelectionDialog.cs`, `FavorHudElement.cs`
- [x] Simplify `PlayerReligionData` (removed Favor, FavorRank, UnlockedBlessings)
- [x] Update `IPlayerReligionDataManager` interface
- [x] Update GUI components for religion-only display
- [x] Fix all tests (792 passing)

### MVP 1 Week 3: First Utility Blessings ✅ COMPLETE
- [x] Add `VintageStoryStats` constants (`miningSpeedMul`, `hungerrate`)
- [x] Add universal utility blessing IDs
- [x] Implement 8 stat-based blessings (4 Tier 1, 4 Tier 2)

### Implemented Blessings

**Tier 1 - Starter (Fledgling, pick 2):**
| ID | Name | Effect |
|----|------|--------|
| `efficient_miner` | Efficient Miner | +15% mining speed |
| `swift_traveler` | Swift Traveler | +10% movement speed |
| `hardy_constitution` | Hardy Constitution | -15% hunger rate |
| `bountiful_harvest` | Bountiful Harvest | +2 max health |

**Tier 2 - Established (500 prestige, pick 3):**
| ID | Name | Effect |
|----|------|--------|
| `master_crafter` | Master Crafter | +20% mining, +1 health |
| `natures_larder` | Nature's Larder | -25% hunger, +15% healing |
| `iron_will` | Iron Will | +3 health, +10% armor |
| `quick_hands` | Quick Hands | +15% move speed, +10% mining |

### Remaining MVP 1 Tasks
- [ ] Religion creation blessing selection UI
- [ ] Prestige milestone blessing unlock UI
- [ ] In-game testing and balance

### Phase 2: Legacy Code Cleanup (Planned)
- [ ] Remove BlessingKind, FavorRank, DevotionRank enums
- [ ] Remove legacy Blessing properties (Kind, RequiredFavorRank)
- [ ] Delete or convert 30 deity-specific blessings
- [ ] Remove legacy data models (PlayerFavorProgress, PlayerDeityData)
- [ ] Clean up GUI components and network packets
- **Status:** Planning complete, execution pending
- **Estimated Effort:** 11 hours (6-7 execution + 2 validation + 2 documentation)
- **Impact:** ~1,550+ lines of code affected across ~16 files
- **See:** "Phase 2: Legacy Code Cleanup Plan" section below for full details

---

## Executive Summary

This document outlines a fundamental redesign of the PantheonWars blessing system, shifting from a dual progression (personal deity worship + religion) to a **religion-only community progression system**. This change better aligns with Vintage Story's core identity as a cooperative survival-crafting game.

### Core Philosophy Change

**From:**
- Individual deity worship with personal blessings
- Religion provides additional group bonuses
- Dual progression (Favor + Prestige)
- Combat-focused PvP favor earning

**To:**
- Pure community-based progression through religions
- All blessings are religion-wide (no personal trees)
- Single progression (Prestige only)
- Utility/economy/resource-focused blessing pool
- Progressive blessing selection as religion grows

---

## What's Being Removed

### 1. Personal Deity Worship System
- ❌ Individual deity selection
- ❌ Favor rank progression (Initiate → Disciple → Zealot → Champion → Exalted)
- ❌ Personal blessing trees (6 blessings per player)
- ❌ `/deity` commands
- ❌ `/favor` commands
- ❌ Personal favor earning from PvP

### 2. Deity Archetypes
- ❌ Aethra (Light) with predefined blessings
- ❌ Gaia (Nature) with predefined blessings
- ❌ Morthen (Shadow & Death) with predefined blessings
- ❌ Deity relationships and favor multipliers
- ❌ Fixed deity blessing trees (3 × 10 = 30 blessings)

### 3. Combat-Focused Design
- ❌ PvP as primary favor source
- ❌ Combat-heavy special effects (execute, death aura, etc.)
- ❌ "Kill enemies to gain power" progression loop

---

## What's Being Kept

### 1. Religion System (Core Preserved)
- ✅ Religion creation, joining, leaving
- ✅ Founder privileges (management, choices)
- ✅ Public/private religions
- ✅ Invitation system
- ✅ 7-day switching cooldown
- ✅ Religion descriptions
- ✅ Member management (kick, ban)

### 2. Prestige Progression
- ✅ Prestige ranks (Fledgling → Established → Renowned → Legendary → Mythic)
- ✅ Prestige earning through collective activities
- ✅ Rank-up notifications
- ✅ Religion-wide progression tracking

### 3. Technical Infrastructure
- ✅ Blessing model (stat modifiers + special effects)
- ✅ BlessingRegistry system
- ✅ BlessingEffectSystem for applying bonuses
- ✅ Special effect handler architecture
- ✅ Network synchronization
- ✅ Persistence/save system
- ✅ Command infrastructure
- ✅ GUI dialog systems

---

## New Architecture

### 1. Religion-Only Blessing System

**Core Concept:** Religions select blessings from a universal pool as they progress, creating specialized communities.

```
Religion Creation
    ↓
Choose 2 Starter Blessings (Tier 1)
    ↓
Members Contribute (mining, farming, crafting, trading, exploring)
    ↓
Hit Prestige Milestones
    ↓
Unlock Choice Points → Founder Picks New Blessings
    ↓
All Members Receive ALL Religion Blessings
    ↓
Religion Identity Emerges Organically
```

### 2. Blessing Pool Design

**Universal Pool:** 40 blessings organized by category and tier

#### Blessing Categories (6 categories):
1. **Mining & Resources** - Ore detection, mining speed, smelting
2. **Farming & Food** - Crop yield, animal taming, hunger reduction
3. **Crafting & Building** - Crafting speed, tool durability, construction
4. **Exploration & Movement** - Movement speed, fall reduction, cave detection
5. **Trading & Economy** - Carry capacity, trade bonuses, storage
6. **Survival & Defense** - Health, armor, temperature resistance

#### Tier Structure:
- **Tier 1 (0-499 prestige):** Foundation - Basic 15% bonuses
- **Tier 2 (500-1999 prestige):** Specialization - 20-25% bonuses + utility
- **Tier 3 (2000-4999 prestige):** Mastery - 30-35% bonuses + special effects
- **Tier 4 (5000+ prestige):** Legendary - 40-50% bonuses + game-changing effects

### 3. Progressive Selection System

**Religion Creation (Tier 1):**
- Founder names the religion
- Picks **2 starter blessings** from Tier 1 pool
- Sets initial direction without permanent commitment

**Tier 2 Unlock (500 prestige):**
- Religion hits milestone through collective contributions
- Founder chooses **3 more blessings** from Tier 2 pool
- Can continue initial specialization or branch out

**Tier 3 Unlock (2000 prestige):**
- Founder chooses **3 more blessings** from Tier 3 pool
- Specialization becomes clear (mining guild, farming commune, etc.)

**Tier 4 Unlock (5000 prestige):**
- Founder chooses **1 capstone blessing** from Tier 4 pool
- Single legendary ability that defines the religion's server role

**Total per Religion:** 9 blessings (2 + 3 + 3 + 1)

---

## Resource Tracking & Milestones

### Prestige Earning Sources

#### 1. Resource Gathering (40% of prestige)
- **Mining:** Ore blocks mined, rare ores discovered
- **Foraging:** Plants harvested, berries collected
- **Hunting:** Animals killed (PvE), meat gathered
- **Logging:** Trees chopped, wood collected

**Example Milestones:**
- 1,000 ore blocks mined collectively → 50 prestige
- 500 crops harvested collectively → 30 prestige
- 100 animals hunted collectively → 20 prestige

#### 2. Crafting & Building (30% of prestige)
- **Crafting:** Complex items created, tools forged
- **Building:** Blocks placed, structures completed
- **Smelting:** Ingots produced, alloys created

**Example Milestones:**
- 500 items crafted collectively → 40 prestige
- 10,000 blocks placed collectively → 60 prestige
- 1,000 ingots smelted collectively → 35 prestige

#### 3. Trading & Economy (20% of prestige)
- **Player Trading:** Successful trades between players
- **Market Participation:** Items bought/sold
- **Resource Exchange:** Materials traded with other religions

**Example Milestones:**
- 100 player trades completed → 25 prestige
- 50,000 coins traded collectively → 40 prestige

#### 4. Exploration & Discovery (10% of prestige)
- **World Exploration:** New chunks discovered by members
- **Ruin Discovery:** Structures/dungeons found
- **Long-Distance Travel:** Total distance traveled

**Example Milestones:**
- 1,000 chunks explored collectively → 30 prestige
- 10 ruins discovered collectively → 20 prestige
- 100,000 blocks traveled collectively → 25 prestige

#### 5. Optional: PvE Combat (Bonus Source)
- **Drifter/Locust Kills:** PvE enemy elimination
- **Dangerous Creature Kills:** Bears, wolves, etc.

**Example Milestones:**
- 500 drifters killed collectively → 30 prestige bonus
- 100 dangerous creatures killed → 20 prestige bonus

---

## Example Religion Progression Stories

### "The Iron Brotherhood" - Mining Guild

**Week 1 - Creation (Tier 1, 0 prestige):**
- Founder picks: Mining Speed +15%, Ore Detection +20%
- 5 members join, focus on copper/tin mining
- Members collectively mine 1,200 ore blocks → 60 prestige

**Week 3 - Growth (Tier 2, 500 prestige):**
- Hit 500 prestige milestone
- Founder chooses: Smelting Efficiency +25%, Tool Durability +30%, Heat Resistance
- Religion solidifies as mining specialists
- Trade ore/ingots to farming communes for food

**Week 6 - Specialization (Tier 3, 2,050 prestige):**
- Hit 2,000 prestige milestone
- Founder picks: Fortune (10% double ore drops), Storage +50%, Carry Capacity +40%
- Now the server's dominant mining operation
- Other players join to benefit from bonuses

**Week 10 - Mastery (Tier 4, 5,200 prestige):**
- Hit 5,000 prestige milestone
- Founder chooses: Master Prospector (see all ore within 100 blocks)
- Legendary status: THE mining authority
- Economic powerhouse, supplies entire server

**Server Role:** Ore suppliers, metal traders, economic backbone

---

### "Harvest Moon Collective" - Farming Commune

**Week 1 - Creation (Tier 1):**
- Picks: Crop Yield +20%, Faster Harvesting +25%
- 8 members focus on flax, wheat, vegetables

**Week 4 - Growth (Tier 2, 600 prestige):**
- Picks: Animal Taming Speed +30%, Hunger Reduction 20%, Food Preservation +40%
- Becomes self-sufficient food producers

**Week 8 - Specialization (Tier 3, 2,400 prestige):**
- Picks: Crop Growth Speed +50%, Weather Immunity, Storage +60%
- Mass food production, trading surplus

**Week 12 - Mastery (Tier 4, 5,800 prestige):**
- Picks: Instant Crop Growth (right-click to grow, long cooldown)
- Feeds the entire server, controls food economy

**Server Role:** Food suppliers, sustenance providers

---

### "The Night Watch" - Explorer Guild

**Week 1 - Creation (Tier 1):**
- Picks: Night Vision, Movement Speed +15%
- 4 members explore at night, map terrain

**Week 5 - Growth (Tier 2, 550 prestige):**
- Picks: Cave Detection, Ruin Detection, Fall Damage -30%
- Professional scouts and treasure hunters

**Week 9 - Specialization (Tier 3, 2,200 prestige):**
- Picks: Treasure Sense (detect rare loot), Danger Sense, Stealth +50%
- Elite dungeon delvers, sell treasure maps

**Week 14 - Mastery (Tier 4, 5,500 prestige):**
- Picks: Shadow Step (short-range teleport, moderate cooldown)
- Ultimate explorers, discover everything first

**Server Role:** Scouts, cartographers, treasure hunters

---

## 40 Blessing Pool Design (Draft)

### Tier 1 - Foundation (8 blessings available, pick 2)

**Mining & Resources:**

**1. Efficient Miner**
- +10% block break speed when mining
- -15% tool durability consumption on pickaxes/hammers
- *Hybrid: Speed + efficiency for dedicated miners*

**2. Ore Seeker's Fortune**
- 10% chance to discover additional ore when mining ore blocks
- +5% mining speed for ore blocks only
- *Hybrid: Resource multiplication + faster ore extraction*
- **API Note:** Uses Block.OnBlockBroken() hook + RNG check (fully feasible)

**Farming & Food:**

**3. Bountiful Harvest**
- +20% crop harvest quantity (extra drops from harvesting)
- -10% crop break time when harvesting
- *Hybrid: More food + faster gathering*

**4. Efficient Metabolism**
- +25% food satiety restoration from all food sources
- -10% satiety drain rate (hunger slower)
- *Hybrid: Food lasts longer both ways*

**Crafting & Building:**

**5. Master Toolsmith**
- +20% crafted tool/weapon durability
- -15% tool durability consumption when using tools
- *Hybrid: Make better tools + they last longer in your hands*

**Exploration & Movement:**

**6. Drifter's Bane**
- -40% damage taken from temporal creatures (drifters, locusts)
- +20% damage dealt to temporal creatures
- Temporal creatures have -25% aggro range toward you
- *Triple hybrid: Defense + offense + safety against temporal threats*
- **API Note:** Entity.ReceiveDamage() with creature type check + damage modifier (fully feasible, proven pattern)

**Trading & Economy:**

**7. Lucky Scavenger**
- 8% chance to find bonus loot when opening containers (chests, vessels, pots)
- 5% chance to find rare items when foraging wild plants
- +15% chance to discover mushrooms when breaking logs
- *Triple hybrid: Container loot + foraging + wood gathering economy*
- **API Note:** Container open event + Block.OnBlockBroken() for plants/logs + RNG (fully feasible)

**Survival & Defense:**

**8. Hardy Constitution**
- +15% max health
- +20% body heat generation (clothing effectiveness bonus)
- *Hybrid: Tougher + better temperature survival*

---

### Tier 2 - Specialization (10 blessings available, pick 3)

**Mining & Resources:**

**1. Deep Extraction**
- +15% mining speed for all stone/ore blocks
- -10% chance for tools to take durability damage when mining
- *Hybrid: Speed + tool preservation for heavy mining*
- **API Note:** Uses EntityStats "miningSpeed" + CollectibleObject.DamageItem() hook (fully feasible)

**2. Efficient Smelting**
- -25% fuel consumption in furnaces/bloomeries
- +20% smelting speed
- *Hybrid: Fuel efficiency + faster metal production*

**Farming & Food:**

**3. Animal Husbandry**
- -30% animal breeding cooldowns
- +20% animal growth speed
- *Hybrid: Faster livestock production cycle*

**4. Food Preservation**
- -40% food spoilage rate
- +15% crop harvest quantity
- *Hybrid: Less waste + more initial yield*

**Crafting & Building:**

**5. Rapid Builder**
- +30% block placement speed
- +20% ladder/scaffold climbing speed
- -15% fall damage when building
- *Triple hybrid: Fast construction + mobility + safety*
- **API Note:** EntityStats for placement/climbing, Entity.ReceiveDamage() for fall damage (fully feasible)

**6. Masterwork Smithing**
- All smithed tools/weapons/armor have +25% durability
- -20% anvil work time
- Smithed items have a small chance to glow with quality particles (cosmetic)
- *Triple hybrid: Quality crafting + speed + visual prestige*
- **API Note:** Anvil crafting hook + durability modifier + particle effects (fully feasible)

**Exploration & Movement:**

**7. Night Runner**
- +35% movement speed during night hours (includes sprint)
- +20% mining/harvesting speed during night
- Night vision potion effects last 50% longer
- *Triple hybrid: Speed + productivity + potion economy during night*
- **API Note:** Time-conditional EntityStats + potion duration extension (fully feasible)

**8. Gourmet Cook**
- 15% chance cooked meals grant temporary buffs (minor health regen, temp resistance, etc.)
- -25% fuel consumption in cooking pots/ovens
- *Hybrid: Special effects + efficiency*

**Trading & Economy:**

**9. Resource Magnet**
- +25% chance to find extra resources when harvesting any resource
- Applies to: crops, ore, wood, stone, clay
- *Pure: General resource multiplication*

**Survival & Defense:**

**10. Hearty Vitality**
- +25% max health
- +15% natural health regeneration rate
- *Hybrid: Tankier + passive healing*

---

### Tier 3 - Mastery (10 blessings available, pick 3)

**Mining & Resources:**

**1. Deep Miner**
- +35% mining speed for all blocks
- No movement speed penalty when mining (maintain full speed)
- *Hybrid: Fast extraction + mobility while mining*

**2. Gemcutter's Eye**
- 2x chance to find gems/crystals when mining
- Gems found have 15% chance to be "pristine quality" (larger stack size)
- +10% mining speed when mining in deep caves (below Y=0)
- *Triple hybrid: Gem finding + quality + deep mining efficiency*
- **API Note:** Block.OnBlockBroken() + RNG + stack size modifier + depth check (fully feasible)

**Farming & Food:**

**3. Agricultural Mastery**
- +40% crop harvest yield (stacks with Bountiful Harvest)
- 15% chance for harvested crops to drop rare seeds
- -30% hunger rate for all religion members
- *Triple hybrid: Massive yield + seed economy + sustenance*
- **API Note:** Block.OnBlockBroken() for yield/seeds, EntityStats "hungerrate" (fully feasible)

**4. Rancher's Touch**
- Animals produce resources 50% faster (milk, wool, eggs)
- -50% animal feed consumption
- *Hybrid: Resource production + efficiency*

**5. Master Cook**
- Cooked meals restore 50% more satiety
- Meals you cook have 40% longer spoilage time
- 10% chance cooked meals grant random temporary buff (speed/health regen/mining speed, 5min)
- Can cook special "hearty meals" that grant +15% max health for 1 hour
- *Quadruple hybrid: Satiety + preservation + random buffs + special recipes*
- **API Note:** Satiety modifier + spoilage rate + RNG buffs via EntityStats + special recipe (fully feasible, no religion-wide sync needed)

**Crafting & Building:**

**6. Reinforced Construction**
- All crafted armor has +30% durability
- +25% protection value on crafted armor pieces
- *Hybrid: Longer-lasting + more effective armor*

**7. Master Metallurgist**
- All metal tools/weapons/armor you craft have +30% durability
- Smelting has 10% chance to yield bonus ingots
- -30% fuel consumption for all metalworking (bloomeries, crucibles, furnaces)
- Metal items you craft can be repaired for 25% less material cost
- *Quadruple hybrid: Crafting quality + smelting economy + fuel efficiency + repair savings*
- **API Note:** Crafting hooks + smelting yield RNG + fuel modifier + repair cost hook (fully feasible, all proven patterns)

**Exploration & Movement:**

**8. Windrunner**
- +40% sprint speed, stamina never depletes while sprinting
- Take no damage from exhaustion effects
- *Hybrid: Speed + endurance*

**9. Temporal Adept**
- -50% temporal stability drain rate
- Immune to temporal storm damage
- *Hybrid: Safe storm exploration*

**Trading & Economy:**

**10. Merchant's Fortune**
- +20% chance to find "treasure" items when opening any container
- Traders visiting your area stay 50% longer (more time to trade)
- You can see trader names/types from 2x normal distance
- +15% chance traders restock with rare items each visit
- *Quadruple hybrid: Loot finding + trader availability + detection + inventory variety*
- **API Note:** Container loot RNG + trader visit duration + nameplate detection + restock RNG (fully feasible, avoids price modification)

---

### Tier 4 - Legendary (12 blessings available, pick 1)

**⚠️ STATUS: NEEDS REWORK - Tier 4 capstone blessings to be redesigned**

**Design Requirements:**
- 12 legendary capstone blessings (pick 1)
- Game-changing abilities that define religion's server identity
- Should feel truly legendary and unique
- Enable distinct playstyles or server roles
- Must fit Vintage Story's cooperative survival identity
- Avoid pure combat/PvP focus

**Categories to cover (2 blessings each):**
- Mining & Resources
- Farming & Food
- Crafting & Building
- Exploration & Movement
- Trading & Economy
- Survival & Defense

**TODO:** Design 12 capstone blessings with concrete VS mechanics

---

## Realistic Implementation Plan (MVP-Driven)

**Total Timeline:** 20-25 weeks (5-6 months) for full implementation
**Approach:** Incremental MVP releases, each playable and testable

---

### MVP 1: Foundation (Weeks 1-4) - "Core System Migration"

**Goal:** Remove deity system, establish religion-only progression, 6 basic blessings working

**Deliverables:**
- Religion-only progression (no more deities)
- 2 Tier 1 blessings (starter set)
- 4 Tier 2 blessings (mid-game)
- Basic prestige tracking (mining, farming only)
- Religion creation with blessing selection

**Milestones:**

**Week 1-2: Core Cleanup** ✅ COMPLETE
- [x] Remove `/deity` and `/favor` commands
- [x] Update data models (ReligionData, PlayerReligionData)
- [x] Remove deity selection UI
- [x] Remove personal blessing application logic
- [x] Update save/load systems
- [x] Run all existing tests, fix breakages (792 passing)

**Week 3: First 8 Blessings** ✅ COMPLETE
- [x] Implement stat-based blessings using VS EntityStats API
- [x] Efficient Miner (+15% mining via `miningSpeedMul`)
- [x] Swift Traveler (+10% movement via `walkspeed`)
- [x] Hardy Constitution (-15% hunger via `hungerrate`)
- [x] Bountiful Harvest (+2 health via `maxhealthExtraPoints`)
- [x] Master Crafter (+20% mining, +1 health)
- [x] Nature's Larder (-25% hunger, +15% healing)
- [x] Iron Will (+3 health, +10% armor)
- [x] Quick Hands (+15% move, +10% mining)

**Week 4: Integration & Testing**
- [ ] Create blessing selection UI (Tier 1 only: 4 options, pick 2)
- [ ] Update religion creation flow
- [ ] Basic prestige tracker (mining ore = +1, harvest crop = +1)
- [ ] Tier 2 unlock at 500 prestige
- [ ] Test full flow: create → earn prestige → unlock Tier 2
- [ ] In-game testing and balance

**MVP 1 Success Criteria:**
- ⏳ Players can create religions and pick 2 starter blessings
- ⏳ Mining/farming awards prestige
- ⏳ Unlocking Tier 2 works
- ✅ All 8 blessings defined and apply correctly via EntityStats
- ✅ No deity system remains (favor, FavorRank removed)
- ✅ 792 tests passing

**Release:** Internal testing build

---

### MVP 2: Expanded Blessing Pool (Weeks 5-9) - "Full Tier 1-2"

**Goal:** Complete all Tier 1-2 blessings (18 total), robust prestige tracking

**Deliverables:**
- All 8 Tier 1 blessings implemented
- All 10 Tier 2 blessings implemented
- Full prestige tracking (5 categories)
- `/religion progress` command
- Updated UI for all selections

**Milestones:**

**Week 5-6: Remaining Tier 1 Handlers**
- [ ] `OreFortuneHandler` (Ore Seeker's Fortune)
- [ ] `HungerRateHandler` (Efficient Metabolism)
- [ ] `TemporalCreatureDamageHandler` (Drifter's Bane)
- [ ] `ContainerLootBonusHandler` (Lucky Scavenger)
- [ ] `MaxHealthHandler` (Hardy Constitution)
- [ ] Test all 8 Tier 1 blessings in combination

**Week 7-8: Remaining Tier 2 Handlers**
- [ ] `FoodSpoilageHandler` (Food Preservation)
- [ ] `BlockPlacementSpeedHandler` + `LadderClimbingHandler` + `FallDamageReductionHandler` (Rapid Builder)
- [ ] `MasterworkSmithingHandler` (Masterwork Smithing)
- [ ] `NightProductivityHandler` (Night Runner)
- [ ] `MealBuffHandler` + `CookingFuelHandler` (Gourmet Cook)
- [ ] `ResourceMagnetHandler` (Resource Magnet)
- [ ] `HealthRegenHandler` (Hearty Vitality)

**Week 9: Prestige System Polish**
- [ ] Implement full prestige tracking:
  - Mining (ore, stone, gems)
  - Farming (crops, breeding, animal products)
  - Crafting (tools, armor, complex items)
  - Trading (NPC trades)
  - Exploration (chunks, ruins, treasure vessels)
- [ ] Add `/religion progress` command with full stats
- [ ] Prestige milestone notifications
- [ ] Top contributor tracking (optional)

**MVP 2 Success Criteria:**
- ✅ All 18 Tier 1-2 blessings functional
- ✅ Religions can fully specialize (mining guild, farming commune, etc.)
- ✅ Prestige earned from all major activities
- ✅ Progress command shows meaningful data
- ✅ Performance: 50+ simultaneous religions without lag

**Release:** Public alpha test (community server)

---

### MVP 3: Tier 3 Mastery (Weeks 10-15) - "Advanced Blessings"

**Goal:** Complete all 10 Tier 3 blessings, respec system, balance tuning

**Deliverables:**
- All 10 Tier 3 blessings implemented
- Blessing respec system
- Full religion management UI
- Balance pass on all 28 blessings

**Milestones:**

**Week 10-11: Tier 3 Mining/Farming Handlers**
- [ ] `DeepMinerHandler` (Deep Miner)
- [ ] `GemFindingEnhancedHandler` (Gemcutter's Eye)
- [ ] `AdvancedCropYieldHandler` (Agricultural Mastery)
- [ ] `RancherEfficiencyHandler` (Rancher's Touch)
- [ ] Test stacking with Tier 1-2 bonuses

**Week 12-13: Tier 3 Crafting/Combat Handlers**
- [ ] `MasterCookHandler` (Master Cook) - includes special recipe system
- [ ] `ArmorDurabilityHandler` + `ArmorProtectionHandler` (Reinforced Construction)
- [ ] `MasterMetallurgistHandler` (Master Metallurgist) - multi-effect system
- [ ] Test crafting economy balance

**Week 14: Tier 3 Movement/Economy Handlers**
- [ ] `SprintStaminaHandler` + `ExhaustionImmunityHandler` (Windrunner)
- [ ] `TemporalAdeptHandler` (Temporal Adept) - if feasible, else backup blessing
- [ ] `MerchantFortuneHandler` (Merchant's Fortune)

**Week 15: Respec System & Polish**
- [ ] Implement `BlessingRespecManager`
- [ ] Create respec UI with cost calculation
- [ ] Respec cooldown tracking (7 days)
- [ ] Respec notifications to all members
- [ ] Balance tuning pass on all 28 blessings

**MVP 3 Success Criteria:**
- ✅ All 28 Tier 1-3 blessings functional and balanced
- ✅ Respec system works with appropriate cost/cooldown
- ✅ Religions reach Tier 3 naturally through gameplay
- ✅ No dominant "meta" blessing combination
- ✅ All blessing categories feel viable

**Release:** Public beta test

---

### MVP 4: Tier 4 Legendary (Weeks 16-20) - "Capstone Abilities"

**Goal:** Design and implement 12 legendary Tier 4 blessings

**Deliverables:**
- 12 Tier 4 legendary blessings designed (2 per category)
- All Tier 4 handlers implemented
- Full UI polish
- Migration guide for existing saves

**Milestones:**

**Week 16: Tier 4 Design Sprint**
- [ ] Design 12 legendary blessings (validated against API)
- [ ] Community feedback on proposed Tier 4 abilities
- [ ] Finalize Tier 4 blessing pool
- [ ] Create detailed handler specifications

**Week 17-19: Tier 4 Implementation**
- [ ] Implement 6 Tier 4 handlers (Mining, Farming, Crafting categories)
- [ ] Implement 6 Tier 4 handlers (Exploration, Trading, Survival categories)
- [ ] Test each legendary ability for game balance
- [ ] Ensure Tier 4 feels "legendary" vs Tier 3

**Week 20: Final Polish**
- [ ] Full UI pass on all dialogs
- [ ] Performance optimization
- [ ] Complete all unit/integration tests
- [ ] Write migration guide for v1.x → v2.0
- [ ] Create changelog and documentation

**MVP 4 Success Criteria:**
- ✅ All 40 blessings (8+10+10+12) implemented
- ✅ Tier 4 feels meaningfully legendary
- ✅ Full system performs well at scale
- ✅ All tests passing (95%+ coverage)
- ✅ Documentation complete

**Release:** Full v2.0 release candidate

---

### Post-Launch: Refinement (Weeks 21-25) - "Community Feedback"

**Goal:** Balance tuning based on real server data

**Ongoing Tasks:**
- [ ] Monitor server metrics (blessing popularity, progression rates)
- [ ] Balance patches as needed
- [ ] Bug fixes from community reports
- [ ] Optional: Add new blessings based on community requests
- [ ] Optional: Implement blessing "seasons" or rotation system

---

## Implementation Priority Matrix

### P0: Critical Path (MVP 1-2)
**Timeline:** Weeks 1-9
**Handlers:** 18 (all Tier 1-2)
**Risk:** Low (all proven API patterns)
**Blockers:** None

**Must-Have Handlers:**
1. MiningSpeedHandler ⭐ (core gameplay)
2. CropYieldHandler ⭐ (core gameplay)
3. ToolDurabilityHandler ⭐ (quality of life)
4. HungerRateHandler ⭐ (survival impact)
5. MaxHealthHandler ⭐ (combat/survival)

### P1: Enhancement (MVP 3)
**Timeline:** Weeks 10-15
**Handlers:** 10 (all Tier 3)
**Risk:** Medium (multi-effect handlers, balance complexity)
**Blockers:** Requires MVP 2 completion

**High-Value Handlers:**
6. MasterCookHandler (special recipe system)
7. MasterMetallurgistHandler (multi-effect system)
8. GemFindingEnhancedHandler (economy impact)

### P2: Legendary (MVP 4)
**Timeline:** Weeks 16-20
**Handlers:** 12 (all Tier 4, TBD design)
**Risk:** High (game-changing abilities, balance critical)
**Blockers:** Requires community feedback on design

### P3: Post-Launch
**Timeline:** Weeks 21-25
**Handlers:** 0 (polish only)
**Risk:** Low
**Blockers:** None

---

## Resource Requirements

### Single Developer (40hr/week)
- **MVP 1:** 4 weeks (160 hours)
- **MVP 2:** 5 weeks (200 hours)
- **MVP 3:** 6 weeks (240 hours)
- **MVP 4:** 5 weeks (200 hours)
- **Total:** 20 weeks (800 hours)

### Two Developers (40hr/week each)
- **MVP 1:** 3 weeks
- **MVP 2:** 4 weeks
- **MVP 3:** 4 weeks
- **MVP 4:** 3 weeks
- **Total:** 14 weeks (560 hours each = 1,120 hours total)

### Risk Buffer
Add 25% contingency time for:
- Unexpected API limitations
- Balance iteration
- Bug fixing
- Community feedback integration

**Realistic Timeline:**
- 1 dev: 25 weeks (6 months)
- 2 devs: 17-18 weeks (4-4.5 months)

---

## Decision Points

### After MVP 1 (Week 4)
**Question:** Does religion-only progression feel good without deities?
- ✅ Yes → Continue to MVP 2
- ❌ No → Pause and redesign progression feel

### After MVP 2 (Week 9)
**Question:** Are Tier 1-2 blessings balanced and engaging?
- ✅ Yes → Continue to MVP 3
- ⚠️ Concerns → Balance pass before MVP 3

### After MVP 3 (Week 15)
**Question:** Do we need Tier 4 at all, or ship with 28 blessings?
- Option A: Ship v2.0 with Tier 1-3 only (28 blessings)
- Option B: Continue to MVP 4 (40 blessings)
- Decision based on: Community feedback, development capacity, server meta

### After MVP 4 (Week 20)
**Question:** Is v2.0 ready for full release?
- ✅ Yes → Release v2.0
- ⚠️ Needs work → Extended beta period

---

## Original Implementation Plan (Reference)

### Phase 1: Core Redesign & Cleanup (Week 1-2)

**1.1 Remove Deity System**
- [ ] Delete `/deity` commands and handlers
- [ ] Remove deity selection dialog UI
- [ ] Remove deity from all data models
- [ ] Delete `BlessingDefinitions.cs` deity-specific trees (Aethra, Gaia, Morthen)
- [ ] Remove deity validation from blessing system

**1.2 Remove Personal Favor/Blessing System**
- [ ] Delete `/favor` commands
- [ ] Remove `Favor` and `FavorRank` from `PlayerReligionData`
- [ ] Remove personal blessing selection/application
- [ ] Remove favor earning from combat kills
- [ ] Remove player-specific blessing UI elements

**1.3 Update Data Models**
```csharp
// ReligionData changes:
- Remove: Deity deity
- Keep: int Prestige, PrestigeRank rank
- Add: Dictionary<int, List<string>> SelectedBlessingsByTier
  // Key = tier (1-4), Value = list of blessing IDs

// PlayerReligionData changes:
- Remove: int Favor, FavorRank favorRank
- Remove: List<string> personalBlessings
- Keep: string religionId (reference only)
```

**1.4 Simplify Progression**
- [ ] Single progression metric: Prestige only
- [ ] Update prestige thresholds: 0/500/2000/5000
- [ ] Remove dual validation (player + religion blessings)
- [ ] All blessings come from religion, not player

---

### Phase 2: Blessing Implementation (Week 3-4)

**2.1 Create Universal Blessing Pool** ✅ Tier 1-3 Complete, Tier 4 Pending

**Status:**
- ✅ Tier 1: 8 blessings designed (pick 2 at religion creation)
- ✅ Tier 2: 10 blessings designed (pick 3 at 500 prestige)
- ✅ Tier 3: 10 blessings designed (pick 3 at 2000 prestige)
- ⚠️ Tier 4: 12 blessings (pick 1 at 5000 prestige) - **NEEDS DESIGN**

**2.2 Implement Required Special Effect Handlers**

Based on finalized Tier 1-3 blessings, implement these handlers:

**Tier 1 Handlers (100% Feasible):**
- [ ] `MiningSpeedHandler` - +% block break speed (EntityStats "miningSpeed") ✅ FEASIBLE
- [ ] `ToolDurabilityHandler` - -% durability consumption (CollectibleObject.DamageItem hook) ✅ FEASIBLE
- [ ] `OreFortuneHandler` - % chance for bonus ore drops (Block.OnBlockBroken + RNG) ✅ FEASIBLE
- [ ] `OreMiningSpeedHandler` - +% speed for ore blocks only (Block-specific check) ✅ FEASIBLE
- [ ] `CropYieldHandler` - +% crop harvest quantity (Block.OnBlockBroken dropQuantityMultiplier) ✅ FEASIBLE
- [ ] `HarvestSpeedHandler` - -% crop break time (BlockBehavior.GetMiningSpeedModifier) ✅ FEASIBLE
- [ ] `SatietyRestorationHandler` - +% food satiety value (Item consumption hook) ✅ FEASIBLE
- [ ] `HungerRateHandler` - -% hunger drain rate (EntityStats "hungerrate") ✅ FEASIBLE
- [ ] `CraftedDurabilityHandler` - +% durability on crafted items (GridRecipe.OnCraftingComplete hook) ✅ FEASIBLE
- [ ] `TemporalCreatureDamageHandler` - -% damage from drifters/locusts + aggro range (Entity.ReceiveDamage creature check) ✅ FEASIBLE
- [ ] `ContainerLootBonusHandler` - % chance for bonus loot in containers (container open hook + RNG) ✅ FEASIBLE
- [ ] `ForagingBonusHandler` - % chance for rare items from plants/logs (Block.OnBlockBroken + RNG) ✅ FEASIBLE
- [ ] `MaxHealthHandler` - +% max health (EntityStats "maxhealth") ✅ FEASIBLE
- [ ] `BodyHeatHandler` - +% clothing effectiveness (EntityStats "bodyHeat") ✅ FEASIBLE

**Tier 2 Handlers (100% Feasible):**
- [ ] `DeepMiningSpeedHandler` - +% mining speed for stone/ore (EntityStats) ✅ FEASIBLE
- [ ] `ToolPreservationHandler` - -% chance for durability damage (RNG check in DamageItem) ✅ FEASIBLE
- [ ] `SmeltingEfficiencyHandler` - fuel consumption + speed (BlockEntity hooks) ✅ FEASIBLE
- [ ] `AnimalBreedingHandler` - breeding cooldown modifier (EntityAgent behavior hook) ✅ FEASIBLE
- [ ] `AnimalGrowthHandler` - growth speed modifier (Entity tick hook) ✅ FEASIBLE
- [ ] `FoodSpoilageHandler` - spoilage rate modifier (CollectibleObject.Transitionable) ✅ FEASIBLE
- [ ] `BlockPlacementSpeedHandler` - +% placement speed (EntityStats) ✅ FEASIBLE
- [ ] `LadderClimbingHandler` - +% ladder/scaffold climbing (EntityStats) ✅ FEASIBLE
- [ ] `FallDamageReductionHandler` - -% fall damage (Entity.ReceiveDamage hook) ✅ FEASIBLE (already implemented!)
- [ ] `MasterworkSmithingHandler` - +% durability on smithed items + anvil speed (crafting hook + particles optional) ✅ FEASIBLE
- [ ] `AnvilWorkTimeHandler` - -% anvil crafting time (BlockEntity hook) ✅ FEASIBLE
- [ ] `NightProductivityHandler` - +% speed/mining during night + potion duration (time-conditional EntityStats) ✅ FEASIBLE
- [ ] `MealBuffHandler` - chance for temporary buffs from cooked food (item consumption + buff system) ✅ FEASIBLE
- [ ] `CookingFuelHandler` - -% cooking fuel consumption (BlockEntity hook) ✅ FEASIBLE
- [ ] `ResourceMagnetHandler` - +% bonus resources when harvesting (universal harvest hook + RNG) ✅ FEASIBLE
- [ ] `HealthRegenHandler` - passive health regeneration (EntityStats "healingeffectivness") ✅ FEASIBLE

**Tier 3 Handlers (100% Feasible):**
- [ ] `DeepMinerHandler` - +% mining speed, no movement penalty (EntityStats + movement flag) ✅ FEASIBLE
- [ ] `GemFindingEnhancedHandler` - 2x gem chance + pristine quality chance + deep mining speed (Block.OnBlockBroken + RNG + stack size + depth) ✅ FEASIBLE
- [ ] `AdvancedCropYieldHandler` - +% crop harvest + rare seed drops + hunger reduction (Block.OnBlockBroken multi-effects) ✅ FEASIBLE
- [ ] `RancherEfficiencyHandler` - animal resource speed + feed reduction (EntityAgent hooks) ✅ FEASIBLE
- [ ] `MasterCookHandler` - satiety bonus + spoilage + RNG buffs + special recipes (multi-system but all proven) ✅ FEASIBLE
- [ ] `ArmorDurabilityHandler` - +% armor durability (crafting hook + item type check) ✅ FEASIBLE
- [ ] `ArmorProtectionHandler` - +% protection value (EntityStats "armorProtection") ✅ FEASIBLE
- [ ] `MasterMetallurgistHandler` - metal durability + smelting yield + fuel efficiency + repair cost (multi-hook but all feasible) ✅ FEASIBLE
- [ ] `SprintStaminaHandler` - infinite stamina while sprinting (stamina drain hook) ✅ FEASIBLE
- [ ] `ExhaustionImmunityHandler` - no exhaustion damage (damage type check in ReceiveDamage) ✅ FEASIBLE
- [ ] `MerchantFortuneHandler` - container treasure + trader duration + detection + restock (multi-effects, all feasible) ✅ FEASIBLE

**2.3 Update BlessingDefinitions.cs**
```csharp
// New structure:
public static class BlessingDefinitions
{
    public static Dictionary<int, List<Blessing>> BlessingsByTier = new()
    {
        { 1, Tier1Blessings },
        { 2, Tier2Blessings },
        { 3, Tier3Blessings },
        { 4, Tier4Blessings }
    };

    // Each blessing needs:
    // - string Id
    // - string Name
    // - string Description
    // - int Tier
    // - BlessingCategory Category
    // - List<StatModifier> StatMods
    // - List<string> SpecialEffectIds
}
```

- [ ] Define all 28 finalized blessings (Tier 1-3)
- [ ] Add category tags (Mining, Farming, Crafting, Exploration, Trading, Survival)
- [ ] Remove deity requirements
- [ ] Add tier field to each blessing

---

### Phase 3: Progressive Selection System (Week 5)

**3.1 Create BlessingChoiceManager**
```csharp
public class BlessingChoiceManager
{
    // Check if religion has unlocked new tier
    bool HasUnlockedNewTier(ReligionData religion);

    // Get available blessings for tier
    List<Blessing> GetAvailableBlessings(int tier);

    // Validate selection (correct count, not already chosen)
    bool ValidateSelection(ReligionData religion, int tier, List<string> blessingIds);

    // Lock in blessing choices
    void CommitBlessingChoice(ReligionData religion, int tier, List<string> blessingIds);

    // Check what needs to be picked
    int GetPendingChoiceCount(ReligionData religion, int tier);
}
```

**3.2 Update Religion Creation Flow**
- [ ] Remove deity selection step
- [ ] Add Tier 1 blessing picker (8 options, pick 2)
- [ ] Show blessing preview cards with effects
- [ ] Validate 2 selections before allowing creation
- [ ] Save to `ReligionData.SelectedBlessingsByTier[1]`
- [ ] Immediately apply blessings to founder

**3.3 Implement Tier Unlock System**
- [ ] Monitor prestige changes in `ReligionManager`
- [ ] When crossing threshold (500/2000/5000), check for pending choices
- [ ] Send notification to founder: "Your religion has unlocked Tier X blessings!"
- [ ] Open blessing selection dialog
- [ ] Lock until selections made (or add "choose later" option)

**3.4 Create Blessing Selection UI**
```
Dialog Layout:
┌─────────────────────────────────────────┐
│ Tier 2 Blessing Selection (Pick 3)     │
├─────────────────────────────────────────┤
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│ │Ore   │ │Eff.  │ │Animal│ │Food  │   │
│ │Fort. │ │Smelt.│ │Husb. │ │Pres. │   │
│ └──────┘ └──────┘ └──────┘ └──────┘   │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│ │Eff.  │ │Qual. │ │Night │ │Gourm.│   │
│ │Const.│ │Smith.│ │Run.  │ │Cook  │   │
│ └──────┘ └──────┘ └──────┘ └──────┘   │
│ ┌──────┐ ┌──────┐                     │
│ │Res.  │ │Hearty│                     │
│ │Mag.  │ │Vital.│                     │
│ └──────┘ └──────┘                     │
├─────────────────────────────────────────┤
│ Selected: [Ore Fortune] [Animal Husb.] │
│           [Gourmet Cook]                │
│                                         │
│          [Cancel]  [Confirm (3/3)]      │
└─────────────────────────────────────────┘
```

- [ ] Grid layout with blessing cards
- [ ] Hover shows full description + mechanics
- [ ] Click to toggle selection
- [ ] Highlight selected (max based on tier)
- [ ] Confirm button validates count
- [ ] Show already-chosen blessings from other tiers (read-only)

---

### Phase 4: Resource Tracking & Prestige System (Week 6)

**4.1 Define Prestige Earning Events**

Based on VS mechanics, track these activities:

**Mining:**
- Break ore blocks: +1 prestige per ore
- Break stone/rock: +0.1 prestige per block (scaled)
- Smelt metal: +2 prestige per ingot

**Farming:**
- Harvest crops: +1 prestige per harvest
- Breed animals: +5 prestige per birth
- Collect animal resources: +1 prestige (milk/wool/eggs)

**Crafting:**
- Craft tools/weapons: +3 prestige
- Craft armor: +5 prestige
- Smith at anvil: +4 prestige

**Trading:**
- Trade with NPC trader: +2 prestige per transaction
- (Future: player trading if implemented)

**Exploration:**
- Discover new chunks: +1 prestige per chunk
- Find ruins/structures: +10 prestige
- Open treasure vessels: +5 prestige

**4.2 Implement PrestigeTracker**
```csharp
public class PrestigeTracker
{
    // Hook into game events
    void OnBlockBroken(IPlayer player, Block block);
    void OnItemCrafted(IPlayer player, ItemStack item);
    void OnAnimalBred(IPlayer player, Entity animal);
    void OnTradeComplete(IPlayer player, TradeProperties trade);
    void OnChunkExplored(IPlayer player, Vec2i chunkPos);

    // Award prestige to player's religion
    void AwardPrestige(IPlayer player, int amount, string reason);

    // Check for tier unlocks
    void CheckTierUnlocks(ReligionData religion);
}
```

- [ ] Hook into VS block break events
- [ ] Hook into crafting completion events
- [ ] Hook into animal system events
- [ ] Hook into trader interaction events
- [ ] Hook into world gen/exploration events
- [ ] Award prestige to religion (not player)
- [ ] Broadcast milestone notifications
- [ ] Trigger tier unlock checks

**4.3 Add `/religion progress` Command**
```
> /religion progress

Iron Brotherhood Progress
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Prestige: 1,247 / 2,000 (Tier 2 → Tier 3)

Next Milestone: 753 prestige remaining
Unlock: Tier 3 Mastery Blessings (pick 3)

Recent Contributions (Last 24h):
• Steve: +127 prestige (mining)
• Alex: +89 prestige (farming)
• Jordan: +56 prestige (crafting)

Active Blessings (5/9):
✓ Efficient Miner (Tier 1)
✓ Prospector's Eye (Tier 1)
✓ Ore Fortune (Tier 2)
✓ Efficient Smelting (Tier 2)
✓ Night Runner (Tier 2)
```

- [ ] Show current prestige and rank
- [ ] Show progress to next tier
- [ ] Show top contributors (optional)
- [ ] Show active blessings
- [ ] Show pending choices if any

---

### Phase 5: UI Updates (Week 7)

**5.1 Update Religion Management Dialog**
```
┌─────────────────────────────────────────┐
│ Iron Brotherhood                        │
│ Founded by Steve | 23 members           │
├─────────────────────────────────────────┤
│ Prestige: 1,247 / 2,000 (Tier 2)      │
│ [████████████░░░░░░░░] 62%             │
│                                         │
│ Active Blessings (5/9):                 │
│ ┌Tier 1────────────────────────────┐   │
│ │ • Efficient Miner                │   │
│ │ • Prospector's Eye               │   │
│ └──────────────────────────────────┘   │
│ ┌Tier 2────────────────────────────┐   │
│ │ • Ore Fortune                    │   │
│ │ • Efficient Smelting             │   │
│ │ • Night Runner                   │   │
│ └──────────────────────────────────┘   │
│                                         │
│ Next Unlock: Tier 3 at 2,000 prestige  │
│                                         │
│ [View Members] [Leave] [Manage (F)]     │
└─────────────────────────────────────────┘
```

- [ ] Replace deity display with prestige progress bar
- [ ] Show all active blessings grouped by tier
- [ ] Show next tier unlock requirement
- [ ] Hover blessing names for full description
- [ ] Add "Manage" button (founder only) for respec

**5.2 Update HUD Element**
```
Top-right HUD:
┌──────────────────────┐
│ ⛪ Iron Brotherhood  │
│ Prestige: 1,247      │
│ Blessings: 5 active  │
└──────────────────────┘
```

- [ ] Remove favor/rank display
- [ ] Show religion name + prestige
- [ ] Show active blessing count
- [ ] Click to open religion dialog
- [ ] Compact design to minimize screen space

**5.3 Update Creation Dialog**
- [ ] Remove deity selection step entirely
- [ ] Add description of religion system
- [ ] Add Tier 1 blessing selection (8 choices, pick 2)
- [ ] Preview selected blessings before confirming
- [ ] Show what progression looks like (tutorial)

---

### Phase 6: Respec System (Week 8)

**6.1 Implement Blessing Respecialization**

Based on design decision: YES - religions can respec at high cost.

```csharp
public class BlessingRespecManager
{
    // Calculate respec cost (50% of total earned prestige)
    int CalculateRespecCost(ReligionData religion);

    // Check if religion can afford respec
    bool CanAffordRespec(ReligionData religion);

    // Perform respec (reset all blessings, deduct cost)
    void ExecuteRespec(ReligionData religion);

    // Check cooldown (prevent spam)
    bool IsRespecAvailable(ReligionData religion);
}
```

**Respec Rules:**
- Cost: 50% of total earned prestige (permanent loss)
- Cooldown: 7 days between respecs
- Resets ALL blessing choices (all tiers)
- Religion keeps current prestige tier
- Must re-select blessings immediately (Tier 1-3)
- Tier 4 remains locked until 5000 prestige reached again

**6.2 Add Respec UI**
```
Respec Confirmation Dialog:
┌─────────────────────────────────────────┐
│ ⚠️  Respec Religion Blessings           │
├─────────────────────────────────────────┤
│ This will reset ALL blessing choices    │
│ for Iron Brotherhood.                    │
│                                         │
│ Cost: 623 prestige (50% of 1,247)       │
│ Remaining after: 624 prestige           │
│                                         │
│ You will lose:                           │
│ • Efficient Miner                       │
│ • Prospector's Eye                      │
│ • Ore Fortune                           │
│ • Efficient Smelting                    │
│ • Night Runner                          │
│                                         │
│ You will immediately re-select:         │
│ • 2 Tier 1 blessings                    │
│ • 3 Tier 2 blessings                    │
│                                         │
│ Cooldown: 7 days before next respec     │
│                                         │
│ [Cancel] [Confirm Respec]                │
└─────────────────────────────────────────┘
```

- [ ] Add "Respec Blessings" button (founder only)
- [ ] Show cost calculation and consequences
- [ ] Require typed confirmation ("RESPEC")
- [ ] Broadcast to all members
- [ ] Immediately open Tier 1 picker after respec

**6.3 Respec Notifications**
- [ ] Notify all members when founder initiates respec
- [ ] Show what blessings were lost
- [ ] Show what new blessings were chosen
- [ ] Add to religion activity log

---

### Phase 7: Testing & Balance (Week 9-10)

**7.1 Unit Testing**
- [ ] Test BlessingChoiceManager validation logic
- [ ] Test PrestigeTracker event handling
- [ ] Test tier unlock detection
- [ ] Test respec cost calculations
- [ ] Test blessing application/removal
- [ ] Test data persistence (save/load)

**7.2 Integration Testing**
- [ ] Full religion progression (0 → 5000 prestige)
- [ ] Multiple religions on same server
- [ ] Member join/leave during progression
- [ ] Founder transfer during tier unlock
- [ ] Respec during active play
- [ ] Server restart/reload

**7.3 Balance Testing**
```
Progression Testing Checklist:
[ ] Solo player: Can reasonably reach Tier 2 in X hours
[ ] 3-person religion: Can reach Tier 3 in Y hours
[ ] 10-person religion: Can reach Tier 4 in Z hours
[ ] Prestige earning feels rewarding, not grindy
[ ] Tier unlocks feel like meaningful milestones
[ ] Blessings create noticeable gameplay improvements
[ ] No single blessing is "mandatory" (variety is viable)
[ ] Different religion builds have distinct playstyles
```

**7.4 Performance Testing**
- [ ] Test with 50+ active religions
- [ ] Test prestige tracking overhead
- [ ] Test blessing effect handlers (1000+ calls/sec)
- [ ] Test UI rendering with many blessings
- [ ] Profile memory usage

**7.5 Balance Tuning**

Adjust based on playtesting:
- [ ] Blessing percentage values (too strong/weak?)
- [ ] Prestige earning rates (too fast/slow?)
- [ ] Tier thresholds (500/2000/5000 appropriate?)
- [ ] Respec cost (50% too punishing/lenient?)
- [ ] Special effect cooldowns (if applicable)

---

## Migration Strategy

### For Existing Worlds/Saves

**Option A: Clean Break (Recommended)**
- Major version bump (2.0.0)
- Incompatible with old saves
- Servers must reset or remove mod data
- Clear communication in changelog

**Option B: Migration Tool**
- Convert existing player data
- Grant equivalent prestige based on old favor
- Assign random starter blessings
- Requires complex migration logic

**Recommendation:** Option A - This is a fundamental redesign. Better to require fresh start than risk data corruption.

---

## Benefits of This Redesign

### 1. Fits Vintage Story Identity
- Emphasizes cooperation over individual power
- Focuses on crafting/survival, not combat
- Natural resource-gathering progression
- Community specialization feels authentic

### 2. Simpler System
- One progression instead of two
- No deity worship complexity
- No favor/prestige confusion
- Clearer value proposition

### 3. Emergent Gameplay
- Religions develop unique identities organically
- Server economies form naturally
- Specialization creates interdependence
- Stories emerge from choices

### 4. Better Server Meta
- Mining guilds trade with farming communes
- Explorer guilds sell maps/loot
- Crafting guilds make gear for everyone
- Natural cooperation incentives

### 5. Reduced Content Bloat
- No need for deity lore/relationships
- No combat-specific special effects
- Focus on core VS mechanics
- Easier to balance

---

## Risks & Mitigation

### Risk 1: Too Drastic a Change
**Mitigation:** This is a new design direction. Treat as major version 2.0, not an update. Existing players expecting deity worship may not like it, but new players will get a cohesive experience.

### Risk 2: Solo Players Left Out
**Mitigation:** Allow 1-person religions. Solo players can still create and progress their own "guild" with bonuses. Alternatively, provide small baseline bonuses for non-members.

### Risk 3: Founder Power Imbalance
**Mitigation:** Provide transparency - show blessing choices before joining. Allow democratic voting as optional setting. Members can leave if unhappy with founder's choices.

### Risk 4: Blessing Pool Balance
**Mitigation:** Extensive playtesting. Iterate on numbers. Gather community feedback. Make blessings interesting but not overpowered.

### Risk 5: Complexity of Resource Tracking
**Mitigation:** Start simple with obvious milestones (blocks mined, crops harvested). Expand based on what's trackable. Use existing VS events where possible.

---

## Success Criteria

### Core Metrics:
1. **Religion Creation Rate:** 50%+ of players create/join religions
2. **Specialization Diversity:** No single blessing combination dominates
3. **Server Interdependence:** Religions trading/cooperating regularly
4. **Retention:** Players stay engaged through tier progression
5. **Balance:** No blessing feels mandatory or useless

### Qualitative Goals:
- Religions have clear identities ("the miners", "the farmers")
- Server politics emerge around resource control
- Players roleplay their religion's specialty
- Cooperation feels rewarding, not forced
- System feels "Vintage Story" not "MMORPG"

---

## Next Steps

1. **Community Feedback:** Share this document for initial reactions
2. **Blessing Pool Design:** Finalize the 30 blessings with specific numbers
3. **Prototype:** Build progressive selection UI mockup
4. **Resource Tracking:** Identify trackable VS events for milestones
5. **Implementation:** Begin Phase 1 work (remove deity system)

---

## Design Decisions

### 1. Blessing Respecialization
**Decision:** YES - Religions can respec blessings at a high cost.

**Rationale:** Allows religions to adapt their specialization as their needs evolve or server meta changes, but the high cost ensures choices remain meaningful and prevents constant flip-flopping.

**Implementation Notes:**
- Respec should cost significant prestige (e.g., 50% of total earned prestige)
- Consider cooldown period to prevent abuse
- All members must be notified when founder initiates respec

---

### 2. Maximum Religion Size
**Decision:** NO - No hard cap on religion member count.

**Rationale:** Let player communities organize naturally. Social dynamics and coordination overhead will naturally limit mega-guilds without artificial restrictions.

**Implementation Notes:**
- Trust emergent gameplay to balance organization sizes
- Monitor server feedback but avoid premature optimization

---

### 3. Inactive Member Penalties
**Decision:** NO - Inactive members do NOT lose blessings.

**Rationale:** Life happens. Players shouldn't be punished for taking breaks. Blessings represent the religion's collective achievements, not individual contribution tracking.

**Implementation Notes:**
- Keep the system welcoming and low-pressure
- Religion progression depends on active contribution, but benefits are shared unconditionally

---

### 4. Blessing Exclusivity
**Decision:** NO - Blessings are NOT mutually exclusive across religions.

**Rationale:** Multiple religions can pick the same blessings. Natural server diversity will emerge from different strategic priorities and milestone timing, not artificial forced differentiation.

**Implementation Notes:**
- Let meta develop organically
- Different religions will naturally prioritize different blessing combinations based on their goals

---

### 5. PvP-Specific Blessings
**Decision:** NO - No separate PvP blessings or combat toggle.

**Rationale:** Keep the design focused on utility, economy, and cooperative survival. Combat blessings don't align with Vintage Story's identity or this mod's redesigned vision.

**Implementation Notes:**
- All blessings should provide utility/economy value
- Avoid creating "PvP meta" pressure that conflicts with VS's cooperative spirit

---

## Fully Feasible Alternative Blessings

**Context:** Several blessings have handlers marked ⚠️ (research needed/complex). Below are drop-in replacements that are 100% API-feasible with zero risk.

### Tier 1 Alternatives

**Current: Temporal Anchor** (⚠️ No temporal stability API found)
```
- -30% temporal stability drain rate
- +15% temporal stability recovery rate
```

**✅ ALTERNATIVE: Drifter's Bane**
```
- -40% damage taken from temporal creatures (drifters, locusts)
- +20% damage dealt to temporal creatures
- Temporal creatures have -25% aggro range toward you
```
**Implementation:** Entity.ReceiveDamage() hook with creature type check, already proven feasible.

---

**Current: Merchant's Favor** (⚠️ No trading API, workaround needed)
```
- +15% currency gained when selling
- +10% chance for rare goods
```

**✅ ALTERNATIVE: Lucky Scavenger**
```
- 8% chance to find bonus loot when opening containers (chests, vessels, pots)
- 5% chance to find rare items when foraging wild plants
- +15% chance to discover mushrooms when breaking logs
```
**Implementation:** Container open hook + Block.OnBlockBroken() for plants/logs, 100% feasible with RNG.

---

### Tier 2 Alternatives

**Current: Quality Smithing** (⚠️ Medium complexity, uncertain)
```
- 10% chance for +1 durability tier
- -20% anvil work time
```

**✅ ALTERNATIVE: Masterwork Smithing**
```
- All smithed tools/weapons/armor have +25% durability
- -20% anvil work time
- Smithed items have a small chance to glow (cosmetic particle effect)
```
**Implementation:** Crafting completion hook + durability modifier, remove RNG tier system. Particles are optional cosmetic flair.

---

**Current: Night Runner** (Includes creature detection ⚠️)
```
- +25% movement speed during night
- -50% creature detection range at night
```

**✅ ALTERNATIVE: Night Runner v2**
```
- +35% movement speed during night hours (includes sprint)
- +20% mining/harvesting speed during night
- Night vision potion effects last 50% longer
```
**Implementation:** Time-conditional EntityStats modifiers + potion duration extension, all feasible.

---

### Tier 3 Alternatives

**Current: Gemcutter's Eye** (⚠️ No trading API for price modification)
```
- 2x gem/crystal finding chance
- +40% gem sell prices
```

**✅ ALTERNATIVE: Gemcutter's Eye v2**
```
- 2x gem/crystal finding chance when mining
- Gems found have 15% chance to be "pristine quality" (bonus stack size or rarity)
- +10% mining speed when mining in deep caves (below Y=0)
```
**Implementation:** Block.OnBlockBroken() + RNG for quality + depth check, all feasible.

---

**Current: Master Cook** (⚠️ Feast system very complex, requires custom buff infrastructure)
```
- 50% stronger meal buffs
- 3x longer buff duration
- Can cook "feast meals" for religion-wide buffs (8hr cooldown)
```

**✅ ALTERNATIVE: Master Cook v2**
```
- Cooked meals restore 50% more satiety
- Meals you cook have 40% longer spoilage time
- 10% chance cooked meals grant random temporary buff (speed/health regen/mining speed, 5min duration)
- Can cook "hearty meals" that grant +15% max health for 1 hour (uses special recipe)
```
**Implementation:** Satiety modifier + spoilage modifier + RNG buff from EntityStats + special recipe, all feasible. Removes religion-wide complexity.

---

**Current: Master Metallurgist** (⚠️ Very complex custom item property system)
```
- Can alloy metals with special properties (lightweight, sharp, flexible)
- Alloyed items have +20% base stats
- First craft free (material refund)
```

**✅ ALTERNATIVE: Master Metallurgist v2**
```
- All metal tools/weapons/armor crafted have +30% durability
- Smelting yields 10% bonus ingots (chance for extra output)
- -30% fuel consumption for all metalworking (bloomeries, crucibles, furnaces)
- Metal items you craft can be repaired for 25% less material cost
```
**Implementation:** Crafting hooks + smelting yield bonus + fuel consumption modifier + repair cost modifier, all proven feasible patterns.

---

**Current: Trade Master** (⚠️ Multiple trading API issues)
```
- +25% currency when selling
- View trader inventory from double distance
- +10% carrying capacity
```

**✅ ALTERNATIVE: Merchant's Fortune**
```
- +20% chance to find "treasure" items when opening any container
- Traders visiting your area stay 50% longer (more time to trade)
- You can see trader names/types from 2x normal distance
- +15% chance traders restock with rare items each visit
```
**Implementation:** Container loot modifier + trader visit duration extension + nameplate detection + restock RNG, all feasible workarounds avoiding price modification.

---

## API Feasibility Validation

**⚠️ CRITICAL: Read API Research Document First**

Before implementing this redesign, review the comprehensive API feasibility analysis:
**`docs/topics/research/vs_api_blessing_handlers_feasibility.md`**

### Key Changes Based on API Research:

**Removed Blessings (API Impossible):**
1. ~~Prospector's Eye~~ (prospecting range) → Replaced with **Ore Seeker's Fortune** (ore duplication RNG)
2. ~~Shrewd Trader~~ (price modification) → Replaced with **Merchant's Favor** (sell bonus + inventory chance)
3. ~~Efficient Construction~~ (recipe cost reduction) → Replaced with **Rapid Builder** (placement speed + mobility)
4. ~~Agricultural Mastery~~ (crop growth speed) → Replaced with **Agricultural Mastery v2** (yield + seeds + hunger)
5. ~~Trade Master~~ (buy/sell prices) → Replaced with **Trade Master v2** (sell bonus + detection + capacity)

**API Feasibility Summary (UPDATED v1.4):**
- ✅ **Tier 1:** 8/8 blessings feasible (100%) - All problematic handlers replaced
- ✅ **Tier 2:** 10/10 blessings feasible (100%) - All problematic handlers replaced
- ✅ **Tier 3:** 10/10 blessings feasible (100%) - All problematic handlers replaced
- ❓ **Tier 4:** TBD - Must validate legendary effects against API before designing

**All 28 Tier 1-3 blessings are now 100% API-feasible with zero risk. Ready for implementation.**

**Implementation Priority:**
1. **P0 (Quick Wins):** Mining speed, walk speed, damage reduction, hunger rate, crop yield
2. **P1 (Standard):** Tool durability, animal breeding, food spoilage, health regen
3. **P2 (Complex):** Feast cooking, metal alloying, trader bonuses
4. **P3 (Deferred):** Temporal stability, creature detection (pending API research)

### Validation Protocol:

Before implementing ANY blessing handler:
1. Check feasibility status in research doc (✅/⚠️/❌)
2. If ⚠️ (research needed), prototype first to validate API access
3. If ❌ (impossible), use replacement handler from research doc
4. Update handler checklist with actual implementation results

---

---

## Phase 2: Legacy Code Cleanup Plan

**Status:** ⚠️ PLANNING - Not yet executed
**Impact:** High - Removes ~2,000+ lines of legacy code from dual-progression system
**Estimated Effort:** 6-7 hours
**Risk:** Medium - Requires careful removal to avoid breaking existing functionality

### Background

During MVP 1 implementation (Weeks 1-3), the personal deity/favor system was partially removed, but significant legacy code remains from the original Phase 1-2 dual-progression architecture. This creates technical debt and makes the codebase confusing, as it appears to be "a hack of the original mod" rather than a clean religion-only system.

**User Feedback (Nov 21, 2025):**
> "I feel our redesign is a hack of the original mod. A lot of the existing classes and pieces still exist."

**Specific Examples Identified:**
- `Blessing.Deity` property (line 47 in Blessing.cs) - Should be DeityType.None for universal blessings
- `Blessing.Kind` property (line 52) - Obsolete enum distinguishing Player vs Religion blessings
- `Blessing.RequiredFavorRank` property (line 62) - Replaced by RequiredPrestigeRank
- Multiple obsolete enums: BlessingKind, FavorRank, DevotionRank
- 30 deity-specific blessing definitions still using old progression model
- Legacy UI components, network packets, and data models

### Comprehensive Legacy Code Analysis

This section documents ALL legacy code identified in the codebase that must be removed or refactored to complete the religion-only architecture transition.

---

#### 1. Legacy Enums (Complete Removal)

**File: `PantheonWars/Models/Enum/BlessingKind.cs`**
- **Status:** Entire file should be deleted
- **Reason:** Distinguishes Player vs Religion blessings - obsolete in religion-only system
- **Impact:** All blessings are now Religion-level, no personal blessing trees
- **Dependencies:** Referenced in ~15 files, all must be updated

**File: `PantheonWars/Models/Enum/FavorRank.cs`**
- **Status:** Entire file should be deleted
- **Reason:** Personal progression ranks (Initiate → Exalted) replaced by PrestigeRank
- **Impact:** All favor-based progression removed
- **Dependencies:** Referenced in blessing definitions, tooltips, network packets

**File: `PantheonWars/Models/Enum/DevotionRank.cs`**
- **Status:** Entire file should be deleted (if exists)
- **Reason:** Related to personal deity devotion system
- **Impact:** Part of removed personal worship mechanics

---

#### 2. Legacy Blessing Model Properties

**File: `PantheonWars/Models/Blessing.cs` (Lines 43-62)**

**Properties to Remove/Refactor:**

```csharp
// Line 47: Deity property - PARTIAL REMOVAL
public DeityType Deity { get; set; } = DeityType.None;
```
- **Action:** Keep property but enforce `DeityType.None` for all universal blessings
- **Reason:** Currently used for filtering, but all new blessings should be universal
- **Long-term:** Could be removed entirely if deity-specific blessings are deleted

```csharp
// Line 52: Kind property - DELETE
public BlessingKind Kind { get; set; } = BlessingKind.Religion;
```
- **Action:** Remove property entirely
- **Reason:** No distinction between Player/Religion blessings in new architecture
- **Impact:** All blessings are religion-level by default

```csharp
// Line 62: RequiredFavorRank property - DELETE
public int RequiredFavorRank { get; set; }
```
- **Action:** Remove property entirely
- **Reason:** Replaced by `RequiredPrestigeRank`
- **Impact:** All progression now uses PrestigeRank thresholds

**Estimated Changes:** 3 property removals + constructor updates + validation updates

---

#### 3. Legacy Blessing Definitions

**File: `PantheonWars/Systems/BlessingDefinitions.cs` (Lines 154-697)**

**Issue:** 30 deity-specific blessings still use old progression model

**Current State:**
- Blessings organized by deity (Aethra, Gaia, Morthen)
- Use `BlessingKind.Player` and `RequiredFavorRank`
- Total: 543 lines of code using legacy properties

**Action Required:**
- **Option A (Aggressive):** Delete all 30 deity-specific blessings entirely
  - Keeps only universal utility blessings (8 implemented so far)
  - Clean break from combat-focused Phase 1 design
  - Aligns with religion-only architecture

- **Option B (Conversion):** Convert deity blessings to universal
  - Remove deity association (set to DeityType.None)
  - Replace RequiredFavorRank with RequiredPrestigeRank
  - Update Kind to BlessingKind.Religion (or remove after enum deletion)
  - Requires balancing 30 blessings against new utility pool

**Recommendation:** Option A - Delete deity-specific blessings
- Cleaner architecture
- Avoids balancing nightmares (combat vs utility)
- Consistent with redesign philosophy (no deity worship)
- Can reintroduce converted blessings in future if needed

**Estimated Changes:** 543 lines removed OR 543 lines refactored

---

#### 4. Legacy Constants and IDs

**File: `PantheonWars/Constants/BlessingIds.cs` (Lines 14-66)**

**Issue:** Comments reference "Player Blessings (6)" for deity-specific sections

**Current Structure:**
```csharp
// Aethra Player Blessings (6)
public const string HolySmiteBlessing = "holysmite";
// ... more deity blessings
```

**Action Required:**
- Update comments to reflect universal blessing pool
- Remove deity-specific blessing ID constants if blessings are deleted
- Add new universal blessing IDs (already started in MVP 1)
- Reorganize by category (Mining, Farming, etc.) instead of deity

**Estimated Changes:** ~50 lines of documentation updates + constant removals

---

#### 5. Legacy Data Models

**File: `PantheonWars/Models/PlayerFavorProgress.cs`**
- **Status:** Should be deleted entirely
- **Reason:** Tracked personal favor progression, replaced by religion-level PrestigeProgress
- **Dependencies:** Check for references in progression tracking systems
- **Impact:** Personal progression data no longer needed

**File: `PantheonWars/Data/PlayerDeityData.cs`**
- **Status:** Already marked `[Obsolete]`, scheduled for v2.0.0 removal
- **Reason:** Stored personal deity selection and favor data
- **Action:** Complete the deletion in Phase 2 cleanup
- **Impact:** No more personal deity data persistence

**Estimated Changes:** 2 files deleted (~200 lines)

---

#### 6. Legacy Interfaces

**File: `PantheonWars/Systems/Interfaces/IFavorSystem.cs`**
- **Status:** Orphaned interface with no implementation
- **Reason:** Interface for personal favor tracking system (already deleted)
- **Action:** Delete entire file
- **Impact:** No active references found

**Estimated Changes:** 1 file deleted (~30 lines)

---

#### 7. Legacy Blessing Tooltip Data

**File: `PantheonWars/Models/BlessingTooltipData.cs`**

**Properties to Remove:**

```csharp
// Line 28: Kind property - DELETE
public BlessingKind Kind { get; set; }

// Line 37-39: RequiredFavorRank property - DELETE
public int RequiredFavorRank { get; set; }

// Line 195-208: GetFavorRankName() method - DELETE
public string GetFavorRankName()
{
    // Converts FavorRank enum to display string
}
```

**Action Required:**
- Remove Kind property (all blessings are religion-level)
- Remove RequiredFavorRank property (use RequiredPrestigeRank instead)
- Remove GetFavorRankName() method (no longer needed)
- Update tooltip generation to show only PrestigeRank requirements

**Estimated Changes:** 3 property removals + 1 method deletion + tooltip format updates (~50 lines)

---

#### 8. Legacy GUI Components

**File: `PantheonWars/GUI/BlessingDialogManager.cs` (Lines 30-34)**

**Issue:** Favor-related fields marked "kept for interface compatibility"

**Current Code:**
```csharp
// Line 30-34: Legacy fields
private int CurrentFavorRank; // kept for interface compatibility
private int CurrentFavor;     // kept for interface compatibility
private int TotalFavorEarned; // kept for interface compatibility
```

**Action Required:**
- Delete all favor-related fields
- Remove any UI elements displaying favor
- Update dialog rendering to show only prestige/religion data
- Remove compatibility shims

**Estimated Changes:** 3 field removals + UI rendering updates (~100 lines)

---

#### 9. Legacy Network Packets

**File: `PantheonWars/Network/BlessingDataResponsePacket.cs`**

**Issue:** Network protocol includes favor-related fields

**Current Fields:**
```csharp
public FavorRank FavorRank { get; set; }
public int CurrentFavor { get; set; }
public int TotalFavorEarned { get; set; }
```

**Action Required:**
- Remove favor fields from packet definition
- Update packet serialization/deserialization
- Update client-side packet handlers
- **⚠️ Breaking Change:** Requires protocol version bump
- Add migration guide for multiplayer compatibility

**Estimated Changes:** 3 field removals + packet handler updates (~80 lines)

---

#### 10. Legacy Special Effects

**File: `PantheonWars/Systems/SpecialEffects/` (Various handlers)**

**Issue:** Combat-focused special effects from deity blessings

**Potentially Obsolete Handlers:**
- Execute mechanics (instant kill at low health)
- Death aura effects (PvP damage)
- Favor-on-kill mechanics
- Deity-specific particle effects

**Action Required:**
- **Option A:** Delete all combat-specific special effect handlers
- **Option B:** Keep handlers but remove blessing definitions that use them
- Audit all special effect handlers for legacy references

**Estimated Changes:** TBD - Requires audit of SpecialEffects directory

---

### Cleanup Execution Plan

This plan removes all legacy code in 4 phases, estimated at 6-7 hours total effort.

---

#### **Phase 1: Delete Enums & Simple Properties (2 hours)**

**Tasks:**
1. Delete `BlessingKind.cs` enum file
2. Delete `FavorRank.cs` enum file
3. Delete `DevotionRank.cs` enum file (if exists)
4. Remove `Blessing.Kind` property from Blessing.cs
5. Remove `Blessing.RequiredFavorRank` property from Blessing.cs
6. Remove favor-related properties from BlessingTooltipData.cs
7. Delete `GetFavorRankName()` method
8. Run build to find all broken references
9. Update all references to use PrestigeRank instead
10. Run all tests, fix breaking changes

**Expected Breakages:**
- ~15 files referencing BlessingKind
- ~10 files referencing FavorRank
- Tooltip generation methods
- Blessing definition constructors

**Success Criteria:**
- All enums deleted
- All simple properties removed
- Build succeeds with zero errors
- All tests passing

---

#### **Phase 2: Remove Legacy Blessing Definitions (3 hours)**

**Tasks:**
1. Audit all 30 deity-specific blessings in BlessingDefinitions.cs
2. **Decision Point:** Delete vs Convert
   - If DELETE: Remove lines 154-697 entirely
   - If CONVERT: Update each blessing to use universal properties
3. Update BlessingIds.cs constants and comments
4. Remove deity-specific blessing IDs if deleted
5. Reorganize remaining blessings by category
6. Update BlessingRegistry to remove deity filtering logic
7. Run all tests related to blessing loading
8. Verify only universal blessings are loaded

**Expected Breakages:**
- Tests expecting deity-specific blessings
- Special effect handlers referenced by deleted blessings
- Command output showing deity blessing counts

**Success Criteria:**
- Only universal blessings in BlessingDefinitions.cs
- No deity-specific blessing IDs remain
- BlessingRegistry loads only universal pool
- All tests passing

---

#### **Phase 3: Clean Up Systems & UI (4 hours)**

**Tasks:**
1. Delete `PlayerFavorProgress.cs` model
2. Delete `PlayerDeityData.cs` (complete obsolete removal)
3. Delete `IFavorSystem.cs` interface
4. Remove favor fields from BlessingDialogManager.cs
5. Update GUI rendering methods to remove favor display
6. Remove favor fields from BlessingDataResponsePacket.cs
7. Update packet handlers (client & server)
8. Bump network protocol version
9. Update all dialog UIs to show only religion/prestige
10. Audit SpecialEffects directory for combat handlers
11. Delete obsolete special effect handlers if blessings removed
12. Run full integration tests
13. Test multiplayer packet synchronization

**Expected Breakages:**
- Dialog rendering methods
- Network synchronization
- Client-side packet handling
- UI element positioning

**Success Criteria:**
- All legacy data models deleted
- No favor references in GUI
- Network packets use only religion/prestige
- UI shows clean religion-only interface
- Multiplayer synchronization works

---

#### **Phase 4: Documentation & Final Validation (2 hours)**

**Tasks:**
1. Update all inline code comments referencing favor/deities
2. Update XML documentation for modified methods
3. Search codebase for remaining "favor" references
4. Search codebase for remaining "deity" references (excluding DeityType enum)
5. Update this redesign document with completion status
6. Run full test suite (792+ tests)
7. Perform manual testing:
   - Create religion with universal blessings
   - Unlock Tier 2 blessings
   - View blessing list
   - Check GUI displays correctly
8. Write migration notes for v2.0 changelog
9. Document breaking changes for server operators

**Success Criteria:**
- No "favor" references remain (except in git history)
- No "deity" references in active code (DeityType.None allowed)
- All 792+ tests passing
- Manual testing confirms clean architecture
- Documentation updated

---

### Breaking Changes & Migration Impact

#### **For Server Operators:**

**⚠️ BREAKING: Save Data Incompatibility**
- Old save data using personal favor/deity will be incompatible
- Recommendation: Fresh start for v2.0 or manual data migration
- Network protocol version bump may require client updates

**Migration Options:**
1. **Clean Break (Recommended):** Start fresh with religion-only system
2. **Manual Migration:** Use admin commands to grant equivalent prestige
3. **Data Conversion Script:** TBD - would require significant effort

#### **For Mod Developers:**

**Removed APIs:**
- `BlessingKind` enum
- `FavorRank` enum
- `Blessing.Kind` property
- `Blessing.RequiredFavorRank` property
- `IFavorSystem` interface
- `PlayerFavorProgress` model
- Favor-related network packets

**Replacement APIs:**
- Use `Blessing.RequiredPrestigeRank` instead of `RequiredFavorRank`
- Use `ReligionData.PrestigeRank` instead of `PlayerDeityData.FavorRank`
- All blessings are now religion-level (no personal trees)

---

### Estimated Code Impact

| Category | Files Affected | Lines Removed | Lines Modified | Risk |
|----------|----------------|---------------|----------------|------|
| Enums | 3 files | ~100 | 0 | Low |
| Blessing Model | 3 files | ~100 | ~50 | Medium |
| Blessing Definitions | 1 file | ~543 | 0 | High |
| Data Models | 2 files | ~200 | 0 | Low |
| Interfaces | 1 file | ~30 | 0 | Low |
| GUI Components | 2 files | ~100 | ~100 | Medium |
| Network Packets | 2 files | ~50 | ~80 | High |
| Tooltips | 1 file | ~50 | ~50 | Low |
| Special Effects | TBD | TBD | TBD | Medium |
| Constants | 1 file | ~50 | ~50 | Low |
| **TOTAL** | **~16 files** | **~1,223 lines** | **~330 lines** | **Medium** |

**Total Code Changes:** ~1,550+ lines affected
**Total Effort:** 11 hours (6-7 hours execution + 2 hours validation + 2 hours documentation)

---

### Risk Mitigation

**Risk 1: Breaking Existing Tests**
- **Mitigation:** Run tests after each phase, fix incrementally
- **Fallback:** Git branch for cleanup, can revert if needed

**Risk 2: Unintended System Dependencies**
- **Mitigation:** Comprehensive search for legacy references before deletion
- **Fallback:** Keep deleted code in commented blocks initially

**Risk 3: Network Protocol Incompatibility**
- **Mitigation:** Version bump + clear migration guide
- **Fallback:** Provide backward compatibility layer (adds 2 hours)

**Risk 4: Lost Game Content (30 blessings)**
- **Mitigation:** Decision point - delete vs convert
- **Fallback:** Archive deleted blessing definitions for potential future use

---

### Success Metrics

**Code Quality:**
- [ ] Zero references to `BlessingKind` enum
- [ ] Zero references to `FavorRank` enum
- [ ] Zero favor-related properties in active code
- [ ] All deity-specific blessings removed or converted
- [ ] Clean architecture with single progression path

**Functionality:**
- [ ] All 792+ tests passing
- [ ] Religion creation works with universal blessings
- [ ] Blessing list command shows only universal pool
- [ ] GUI displays clean religion-only interface
- [ ] Network synchronization works in multiplayer

**Documentation:**
- [ ] All code comments updated
- [ ] Migration guide written
- [ ] Breaking changes documented
- [ ] Redesign document marked as complete

---

### Post-Cleanup Validation Checklist

After completing all 4 phases, validate the following:

**Code Validation:**
- [ ] Search codebase for "BlessingKind" → 0 results
- [ ] Search codebase for "FavorRank" → 0 results (except git history)
- [ ] Search codebase for "RequiredFavorRank" → 0 results
- [ ] Search codebase for "PlayerFavorProgress" → 0 results
- [ ] Search codebase for "IFavorSystem" → 0 results
- [ ] Build succeeds with zero warnings
- [ ] All unit tests pass (792+)

**Functional Validation:**
- [ ] Create new religion with 2 universal Tier 1 blessings
- [ ] Earn prestige through mining/farming
- [ ] Hit 500 prestige milestone
- [ ] Unlock 3 Tier 2 blessings
- [ ] View blessing list (shows only universal blessings)
- [ ] Check religion dialog (shows prestige, no favor)
- [ ] Test in multiplayer (client/server sync)
- [ ] Restart server (persistence works)

**Architecture Validation:**
- [ ] No dual-progression complexity remains
- [ ] Single prestige progression path clear
- [ ] Universal blessing pool clean
- [ ] Religion-only architecture consistent
- [ ] No "hacked on" feeling in codebase

---

## Document History

- **v2.1 (Nov 21, 2025):** Added comprehensive "Phase 2: Legacy Code Cleanup Plan" section documenting ~2,000+ lines of legacy code to be removed from dual-progression system. Includes 10-section analysis, 4-phase execution plan (11 hours estimated), breaking changes documentation, and validation checklists. Addresses user feedback about architecture feeling like "a hack of the original mod."
- **v1.5 (Nov 20, 2025):** Added realistic MVP-driven implementation plan with 4 release milestones. Total timeline: 20-25 weeks. Includes decision points, resource requirements, priority matrix, and success criteria for each MVP.
- **v1.4 (Nov 20, 2025):** MAJOR UPDATE - Replaced ALL problematic handlers (⚠️ research/complex) with 100% feasible alternatives. All 28 Tier 1-3 blessings are now API-validated and implementation-ready. Added comprehensive "Fully Feasible Alternative Blessings" section with drop-in replacements. Tier 1-3 now 100% feasible (previously 85%).
- **v1.3 (Nov 20, 2025):** Updated all blessings based on VS API feasibility research. Replaced 5 impossible handlers with API-feasible alternatives.
- **v1.2 (Nov 20, 2025):** Finalized design decisions (respec, religion size, inactive members, exclusivity, PvP)
- **v1.1 (Nov 20, 2025):** Updated progression to single capstone design (2+3+3+1), expanded pool to 40 blessings
- **v1.0 (Nov 20, 2025):** Initial planning document created
