# Religion-Only Blessing System Redesign

**Document Version:** 1.0
**Created:** November 20, 2025
**Status:** Planning Phase
**Impact:** Major Architecture Change

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

**2. Prospector's Eye**
- +50% prospecting pick detection range
- +25% prospecting pick accuracy (clearer readings)
- *Pure: Better ore finding, less wandering*

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

**6. Temporal Anchor**
- -30% temporal stability drain rate
- +15% temporal stability recovery rate when stable
- *Hybrid: Safer temporal storm exploration + faster recovery*

**Trading & Economy:**

**7. Shrewd Trader**
- -12% prices when buying from traders
- +12% prices when selling to traders
- *Pure: Economic advantage on both sides*

**Survival & Defense:**

**8. Hardy Constitution**
- +15% max health
- +20% body heat generation (clothing effectiveness bonus)
- *Hybrid: Tougher + better temperature survival*

---

### Tier 2 - Specialization (10 blessings available, pick 3)

**Mining & Resources:**
1. Smelting Efficiency +25%
2. Heat Resistance (lava immunity)
3. Tool Repair Speed +50%

**Farming & Food:**
4. Animal Taming Speed +30%
5. Hunger Reduction 20%
6. Food Preservation +40%

**Crafting & Building:**
7. Reduced Material Costs 15%
8. Building Speed +30%
9. Blueprint Learning +25%

**Exploration & Movement:**
10. Cave Detection (see caves on map)
11. Fall Damage Reduction 30%
12. Swim Speed +40%

**Trading & Economy:**
13. Trade Bonus (better prices) +15%
14. Fast Travel (waypoint system)
15. Shared Resources (collective storage)

**Survival & Defense:**
16. Armor Effectiveness +25%
17. Damage Reduction 10%
18. Healing Effectiveness +30%

---

### Tier 3 - Mastery (10 blessings available, pick 3)

**Mining & Resources:**
1. Fortune (10% double ore drops)
2. Ore Vein Detection (x-ray vision for ores)
3. Instant Ore Breaking (cooldown)

**Farming & Food:**
4. Crop Growth Speed +50%
5. Weather Immunity (crops grow in any weather)
6. Animal Breeding 2x Speed

**Crafting & Building:**
7. Quality Crafting (+1 tier to crafted items)
8. Auto-Repair Tools (passive regeneration)
9. Mass Production (craft 2x at once)

**Exploration & Movement:**
10. Treasure Sense (detect rare loot through walls)
11. Danger Sense (detect hostiles)
12. Stealth +50% (reduced aggro range)

**Trading & Economy:**
13. Merchant Network (trade from anywhere)
14. Coin Find (bonus money drops)
15. Shared Experience (XP bonus when near members)

**Survival & Defense:**
16. Passive Regeneration (slow health regen)
17. Thorns (10% damage reflect)
18. Temporal Storm Resistance 50%

---

### Tier 4 - Legendary (12 blessings available, pick 1)

**Mining & Resources:**
1. Master Prospector (see all ore within 100 blocks, permanent)
2. Instant Smelting (no fuel needed, instant results)

**Farming & Food:**
3. Instant Crop Growth (right-click to instantly grow, long cooldown)
4. Infinite Sustenance (never hungry, passive food generation)

**Crafting & Building:**
5. Instant Crafting (no craft time for any recipe)
6. Legendary Forge (craft items 1 quality tier higher)

**Exploration & Movement:**
7. Shadow Step (short-range teleport, moderate cooldown)
8. Map Everything (reveal entire explored region to all members)

**Trading & Economy:**
9. Economic Dominance (+50% to all economic activities)
10. Trade Empire (free fast travel between trading posts)

**Survival & Defense:**
11. Death Cheat (survive lethal hit once per day, auto-revive)
12. Avatar of Earth (40% all stats, massive health regen)

---

## Implementation Plan

### Phase 1: Core Redesign (Week 1-2)

**Tasks:**
1. Remove deity selection system
   - Delete `/deity` commands
   - Remove deity selection dialog
   - Remove deity from player data

2. Remove personal favor system
   - Delete `/favor` commands
   - Remove favor rank progression
   - Remove personal blessing trees
   - Remove favor earning from combat

3. Update data models
   - Remove `PlayerDeityData` (deprecated)
   - Remove `Favor` and `FavorRank` from `PlayerReligionData`
   - Keep only `ReligionData` with prestige tracking
   - Add `SelectedBlessingsByTier` to `ReligionData`

4. Simplify progression
   - Single progression: Prestige only
   - Remove dual blessing validation (player vs religion)
   - Update rank requirements to prestige-only

---

### Phase 2: Blessing Pool Creation (Week 3)

**Tasks:**
1. Design 40 universal blessings
   - 8 Tier 1 (foundation) - pick 2
   - 10 Tier 2 (specialization) - pick 3
   - 10 Tier 3 (mastery) - pick 3
   - 12 Tier 4 (legendary) - pick 1

2. Implement utility-focused special effects
   - Fortune (double drops)
   - Auto-repair
   - Treasure sense
   - Instant actions (with cooldowns)
   - Passive regeneration
   - Thorns/reflect damage

3. Update `BlessingDefinitions.cs`
   - Remove 3 deity-specific trees
   - Create single universal pool
   - Tag blessings by category and tier
   - Remove deity requirements

---

### Phase 3: Progressive Selection System (Week 4)

**Tasks:**
1. Create `ChoicePointManager` system
   - Track when religions unlock tiers
   - Notify founders of available choices
   - Validate blessing selections
   - Lock in choices permanently

2. Update religion creation flow
   - Show Tier 1 blessing pool (12 options)
   - Allow founder to pick 2 starters
   - Save selections to `ReligionData`

3. Implement choice point UI
   - Dialog showing available blessings
   - Preview blessing effects
   - Confirm selection (permanent)
   - Show already-chosen blessings

4. Add tier unlock notifications
   - Alert founder when milestone hit
   - Show blessing picker dialog
   - Count down remaining choices

---

### Phase 4: Resource Tracking (Week 5)

**Tasks:**
1. Create `ResourceTracker` system
   - Hook into mining events (block break)
   - Hook into farming events (harvest, animal)
   - Hook into crafting events (item craft)
   - Hook into trading events (player trade)
   - Hook into exploration events (chunk discovery)

2. Implement milestone tracking
   - Define milestones per category
   - Award prestige on milestone completion
   - Broadcast achievements to religion
   - Persist milestone progress

3. Add `/religion progress` command
   - Show current prestige
   - Show next milestone targets
   - Show member contributions
   - Show unlocked tiers

---

### Phase 5: UI Updates (Week 6)

**Tasks:**
1. Update religion management dialog
   - Show selected blessings
   - Show active effects
   - Show next choice point
   - Show milestone progress

2. Create blessing selection overlay
   - Grid of available blessings
   - Hover for details
   - Click to select (multi-select)
   - Confirm button with validation

3. Update HUD element
   - Remove favor display
   - Show prestige only
   - Show religion blessings active
   - Show milestone progress bar

---

### Phase 6: Testing & Balance (Week 7-8)

**Tasks:**
1. Unit test coverage
   - Choice point logic
   - Milestone tracking
   - Blessing application
   - Prestige earning

2. Integration testing
   - Full religion progression flow
   - Multiple religions on server
   - Blessing conflicts/overlaps
   - Performance testing

3. Balance tuning
   - Blessing power levels
   - Milestone difficulty
   - Prestige earning rates
   - Tier unlock pacing

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

## Document History

- **v1.2 (Nov 20, 2025):** Finalized design decisions (respec, religion size, inactive members, exclusivity, PvP)
- **v1.1 (Nov 20, 2025):** Updated progression to single capstone design (2+3+3+1), expanded pool to 40 blessings
- **v1.0 (Nov 20, 2025):** Initial planning document created
