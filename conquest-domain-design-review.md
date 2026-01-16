# Conquest Domain Design Review

## Executive Summary

The Conquest domain **generally fits** the mod's design patterns but has **several design concerns** that should be addressed:

**✅ Strengths:**
- Follows established favor tracker patterns (follower cache, event-driven)
- Proper dual-path blessing structure (Offensive vs Defensive)
- Special effects are well-implemented and thematic
- Good prerequisite tree structure

**⚠️ Design Concerns:**
1. **Thematic overlap** with Wild domain (both reward killing)
2. **Favor calculation** creates perverse incentives (kill domesticated animals)
3. **Combat stat stacking** may be overpowered compared to utility domains
4. **Missing gameplay loop** - no conquest-specific activities beyond combat
5. **Philosophical tension** with mod's peaceful progression themes

---

## Detailed Analysis

### 1. Favor Tracker Implementation

#### ✅ Pattern Compliance
- **Follower caching**: Properly implemented with HashSet
- **Event subscription**: Uses `OnEntityDeath` event correctly
- **Cleanup**: Proper disposal and cache updates
- **Performance**: O(1) follower lookups

#### ⚠️ Combat Difficulty Calculation Issues

**Current Formula:**
```csharp
healthTier = maxHealth switch {
    >= 100 => 20,  // Boss-tier
    >= 50  => 15,  // Strong
    >= 25  => 10,  // Medium
    >= 10  => 7,   // Minor
    _      => 4    // Weak
}

weightBonus = weight switch {
    >= 200 => 5,
    >= 100 => 3,
    >= 50  => 1,
    _      => 0
}

favor = healthTier + weightBonus  // Total: 4-25 favor
```

**Problem 1: Perverse Incentives**

The tracker rewards **any entity tagged as combat-worthy**, which includes:
- `"hostile"`, `"monster"`, `"drifter"`, `"locust"` ✅ Good
- `"aggressive"` ⚠️ **Includes domesticated animals that attack when provoked**
- `"predator"` ⚠️ **Includes wolves, bears that may be tameable**

**Example Exploit:**
1. Player breeds chickens (Harvest domain activity)
2. Hits chicken to make it "aggressive"
3. Kills aggressive chicken → Earns 4-7 Conquest favor
4. Repeat with pigs, sheep, etc.

**Comparison to Wild Domain:**
- Wild uses `"huntable"` + `"animal"` tags (specific to hunting)
- Wild excludes hostile monsters (thematic separation)
- Conquest should exclude domesticated/tameable animals

**Problem 2: Weight Bonus Inflation**

Heavy creatures get **double-counted**:
- Health correlates with weight (bears: 100+ HP, 300+ kg)
- Weight bonus adds 5 favor to already-high 20 favor (25 total)
- Light but deadly creatures (drifters: 50 HP, 50 kg) get less favor (15 total)

**Better approach**: Use health **OR** weight as primary, not both.

**Problem 3: Missing Tag Filtering**

Should add **positive filtering** for combat-worthy tags:
```csharp
// Should ONLY reward killing:
- Drifters (undead enemies)
- Locusts (swarm enemies)
- Hostile mobs (mod-added enemies)
- Wild predators (wolves, bears in wild state)

// Should NOT reward:
- Domesticated animals (even if aggressive)
- Neutral wildlife (chickens, sheep unless wild)
- Player-owned entities (if ownership system exists)
```

---

### 2. Blessing Structure Analysis

#### ✅ Dual-Path Structure (CORRECT)

**Player Progression:**
```
Bloodthirst (Foundation)
├─ Berserker Rage (Offensive) → Warlord's Strike
└─ Iron Will (Defensive) → Unyielding Fortitude
    ↓
Avatar of Conquest (Capstone, requires both)
```

**Religion Progression:**
```
Warband → Conquering Legion → Conqueror's Banner → Pantheon of Conquest
```

This matches established patterns from other domains. ✅

#### ⚠️ Stat Progression Balance Issues

**Melee Damage Stacking:**

| Tier | Player Blessing | Religion Blessing | Total Bonus |
|------|----------------|-------------------|-------------|
| 1    | +10%           | +10%              | **+20%**    |
| 2    | +25% (total)   | +25% (total)      | **+50%**    |
| 3    | +45% (total)   | +45% (total)      | **+90%**    |
| 4    | +45% (total)   | +50% (total)      | **+95%**    |

Compare to **Craft domain** (melee damage from smithing):
- No direct melee damage blessings
- Focus on tool durability, mining speed, ore yield
- Combat power comes from **better gear**, not stat buffs

Compare to **Wild domain** (ranged combat):
- +15% ranged damage (Avatar of Wild only)
- +15% ranged accuracy (Avatar of Wild only)
- Total: +30% ranged effectiveness at max tier
- **3x less powerful than Conquest melee**

**Problem**: Conquest provides **pure combat power scaling** that other domains don't match.

**Critical Hit Stacking:**

| Tier | Player | Religion | Total |
|------|--------|----------|-------|
| Tier 2 | +5% | - | +5% |
| Tier 3 | +15% | - | +15% |
| Tier 4 | +15% | +5% | **+20%** |

With **+15% crit damage** (Tier 3), effective damage = base × 1.95 × 1.20 = **+134% total**

No other domain provides this level of damage multiplication.

#### ⚠️ Health/Defense Imbalance

**Defensive Path Scaling:**
- +35% max health (player)
- +15% additional health (religion)
- +25% damage reduction (player + religion)

Compare to **Stone domain** (defensive focus):
- +15% max health (Avatar of Earth)
- +10% armor effectiveness
- +15% armor durability
- No damage reduction

**Conquest gets more survivability than the "construction/permanence" domain.**

---

### 3. Special Effects Analysis

#### ✅ BattleFury (Well-Designed)

**Mechanism:**
- Stacks +5% damage per kill (max 5 stacks = +25%)
- 30-second duration, refreshes on kill
- Encourages aggressive combat chains

**Fits Patterns:**
- State tracking: Per-player `FuryState` dictionary
- OnTick cleanup: Checks expiry
- Feedback: Chat notifications
- **Similar to**: Blessed Meals (duration-based buff)

**Balance**: Fair - requires continuous combat to maintain, high-risk gameplay.

#### ✅ Bloodlust (Simple but Effective)

**Mechanism:**
- Heal 5% max health on kill
- No cooldown, no state tracking

**Fits Patterns:**
- Event-driven (OnEntityDeath)
- Immediate effect, no duration
- **Similar to**: RareForageChance (instant trigger)

**Balance**: Fair - rewards successful kills, enables sustained combat.

#### ✅ LastStand (Dynamic Defense)

**Mechanism:**
- +20% damage reduction when health < 25%
- OnTick activation/deactivation
- Per-player boolean state

**Fits Patterns:**
- Conditional stat modifier
- State machine (active/inactive)
- **Similar to**: None (unique mechanic)

**Balance**: Fair - comeback mechanic, doesn't prevent damage, just mitigates.

#### ⚠️ Warcry (Weak Religion Effect)

**Mechanism:**
- Passive -15% animal seeking range
- Applied on blessing unlock, removed on loss

**Problem 1: Already Exists in Wild Domain**
- Wild's Avatar provides -20% animal seeking range (better than Conquest)
- Thematic overlap: Wild is about harmony with nature, Conquest is about domination

**Problem 2: Low Impact**
- Doesn't help against drifters/locusts (main Conquest targets)
- Only affects animals, which Conquest shouldn't focus on

**Better Alternative**:
- "+10% damage to hostile creatures" (simple multiplier)
- "Intimidation aura: Enemies flee when health < 50%" (thematic)
- "+5% damage reduction for all members" (defensive coordination)

---

### 4. Thematic Analysis

#### ⚠️ "Conquest" vs Mod Philosophy

**Mod's Core Themes (from other domains):**
1. **Craft**: Honest labor, craftsmanship, tool mastery
2. **Wild**: Harmony with nature, sustainable hunting/foraging
3. **Harvest**: Agriculture, feeding communities, prosperity
4. **Stone**: Construction, permanence, civilization building

**All existing domains emphasize:**
- Peaceful progression (mining, farming, building)
- Creation over destruction
- Cooperation (religion bonuses support communities)
- Long-term investment (passive tool repair, food preservation, blessed meals)

**Conquest Theme:**
- Combat-focused (kill-centric progression)
- Destruction over creation
- Solo-oriented (most benefits are individual combat power)
- Short-term rewards (immediate favor on kills, temporary buffs)

#### ⚠️ Overlap with Wild Domain

**Both domains reward killing entities:**

| Activity | Wild Favor | Conquest Favor |
|----------|-----------|----------------|
| Kill deer (10 kg, huntable) | 3-6 favor | 7-8 favor (if aggressive) |
| Kill bear (300 kg, predator) | 10-15 favor | 25 favor |
| Kill wolf (50 kg, predator) | 5-8 favor | 16-18 favor |

**Problem**:
- Wild players kill animals for **food/resources** (hunting theme)
- Conquest players kill animals for **favor** (combat theme)
- Same activity, different framing

**Thematic Confusion**:
- Is a bear kill "harmony with nature" (Wild) or "conquest" (Conquest)?
- Should wolves be hunted (Wild) or conquered (Conquest)?

**Better Separation**:
- **Wild**: Kills animals for resources, earns favor from hunting/foraging
- **Conquest**: Kills **monsters/undead/hostile mobs only**, earns favor from defending settlements

---

### 5. Gameplay Loop Analysis

#### ❌ Missing Conquest-Specific Activities

**Other domains have 2-3 distinct activities:**
- **Craft**: Mining + Smithing + Smelting
- **Wild**: Hunting + Foraging (mushrooms, berries, flowers)
- **Harvest**: Planting + Harvesting + Cooking
- **Stone**: Stone Gathering + Clay Forming + Pit Kiln + Construction

**Conquest has ONE activity:**
- Kill stuff

**Problem**:
- No progression diversity
- Farming monsters becomes repetitive
- No "conquest" activities (territory control, raiding, PvP)

**Missing Opportunities**:
1. **Territory Control**: Place "conquest markers" in biomes to claim territory (favor for time held)
2. **Monster Slaying Quests**: Track unique monster kills (first drifter kill, first locust kill)
3. **Raiding**: Destroy temporal gears/drifter bases for favor
4. **PvP Integration**: Tie into existing PvPManager for conquest-themed rewards

**Current State**:
- "Conquest" is just "combat" with better stats
- No strategic or social layer

---

### 6. Balance Assessment

#### Combat Power Comparison

**Conquest (Full Progression):**
- +95% melee damage
- +20% crit chance, +15% crit damage
- +50% max health
- +25% damage reduction
- +10% movement speed
- Heal on kill (5% per kill)
- Battle Fury stacks (+25% additional damage)

**Wild (Full Progression):**
- +30% ranged effectiveness
- +10% movement speed
- -20% animal seeking range
- No health/defense bonuses
- No heal on kill

**Craft (Full Progression):**
- +20% tool durability
- +20% mining speed
- +20% ore yield
- +15% max health (indirect via Forgeborn)
- Passive tool repair
- No combat bonuses

**Assessment**:
- Conquest is **2-3x more powerful in direct combat** than any other domain
- Other domains provide **utility, economy, sustainability** (can't be measured in DPS)
- **Risk**: PvP will be dominated by Conquest followers
- **Risk**: PvE becomes trivial for max-level Conquest players

#### Favor Earning Rate

**Estimates (per hour of focused activity):**

| Domain | Activity | Favor/Hour |
|--------|----------|------------|
| **Conquest** | Kill monsters (10 favor avg, 6/min) | **3600** |
| Craft | Mine ore (2 favor avg, 10/min) | 1200 |
| Wild | Hunt animals (5 favor avg, 5/min) | 1500 |
| Harvest | Harvest crops (1 favor, 20/min) | 1200 |
| Stone | Clay forming (5 favor avg, 8/min) | 2400 |

**Problem**:
- Conquest can earn **2-3x more favor** by farming monster spawns
- Other domains require resource scarcity (ore veins deplete, crops take time to grow)
- Monsters respawn infinitely (especially drifters/locusts)

**Risk**:
- Fastest progression domain
- Players may grind monsters instead of engaging with other systems
- Devalues other domains' progression curves

---

## Recommendations

### Priority 1: Fix Favor Tracker (Critical)

**Issue**: Perverse incentives, overlap with Wild domain

**Solution**:
```csharp
private bool IsCombatWorthy(Entity entity)
{
    if (entity is not EntityAgent) return false;

    // ONLY reward killing these specific threats:
    if (entity.HasTags("hostile", "monster", "drifter", "locust"))
        return true;

    // Exclude predators/aggressive animals (leave those to Wild domain)
    return false;
}
```

**Additional**:
- Remove weight bonus (use health only for scaling)
- Reduce favor range to 3-15 (down from 4-25)
- Add 2-second cooldown per player to prevent AoE farming

### Priority 2: Nerf Combat Stat Scaling (High)

**Issue**: 95% melee damage is excessive, overshadows other domains

**Solution**:
```csharp
// Player Blessings - Reduce by ~40%
Bloodthirst: +10% → +8%
Berserker Rage: +15% → +10%
Warlord's Strike: +20% → +12%

// Religion Blessings - Reduce by ~40%
Warband: +10% → +8%
Conquering Legion: +15% → +10%
Conqueror's Banner: +20% → +12%

// Final Total: +50% melee (down from +95%)
```

**Rationale**:
- Still provides significant combat advantage
- Leaves room for other domains to contribute utility
- Matches Wild's +30% ranged effectiveness better

### Priority 3: Replace Warcry with Unique Effect (Medium)

**Issue**: Overlap with Wild domain, low impact

**Solution Option A** (Simple):
```csharp
// "Intimidation" - Enemies flee when low health
// When hostile entity reaches <30% HP near a Conquest follower,
// apply "feared" status (entity runs away for 5 seconds)
```

**Solution Option B** (Thematic):
```csharp
// "Banner of Conquest" - Religion members gain damage near each other
// +5% damage per nearby ally (max 3 stacks)
// Encourages group combat, fits religion blessing theme
```

### Priority 4: Add Conquest-Specific Activities (Medium)

**Issue**: Only one activity (combat), no depth

**Solution** (Phase 2 feature):
1. **Temporal Rift Destruction**: Destroy temporal rifts for 50 favor (rare event)
2. **Drifter Base Raiding**: Clear drifter spawners for 100 favor (dangerous)
3. **Monster Milestone Tracking**: First kill of each monster type (one-time 50 favor)

**Rationale**:
- Adds variety to Conquest progression
- Creates memorable "conquest" moments
- Reduces grind, increases strategic play

### Priority 5: Reduce Favor Earning Rate (Low)

**Issue**: 3600 favor/hour is 2-3x faster than other domains

**Solution**:
- Reduce favor range to 3-15 (from 4-25)
- Add 2-second cooldown per player
- Cap favor at 10/kill for boss-tier entities

**Expected Rate**: ~1500 favor/hour (matches Wild/Harvest)

---

## Alternative: Rename/Reframe Domain

**Option**: Change "Conquest" → **"War"** or **"Battle"**

**Reasoning**:
- "Conquest" implies territory control, raiding, domination (features not in mod)
- "War" is more neutral, focuses on combat itself
- "Battle" emphasizes individual combat prowess

**Impact**:
- No mechanical changes needed
- Better sets player expectations
- Fits mod's theme better (war is a fact of life in Vintage Story, not necessarily aggressive)

**Counterpoint**:
- PR already calls it "Conquest"
- Renaming would require another refactor commit
- "War" was the original name (already refactored once)

---

## Conclusion

### Should Conquest Domain Be Merged?

**Arguments FOR merging:**
- ✅ Follows established code patterns
- ✅ Well-implemented special effects
- ✅ Adds combat-focused progression (currently missing)
- ✅ Some players will enjoy combat-heavy playstyle

**Arguments AGAINST merging (as-is):**
- ❌ Thematic overlap with Wild domain
- ❌ Perverse favor calculation (kill domestic animals)
- ❌ Overpowered combat scaling (95% damage)
- ❌ Fastest progression domain (3600 favor/hour)
- ❌ Philosophical tension with mod's peaceful themes
- ❌ Missing conquest-specific activities (only killing)

### Recommendation: **Conditional Merge with Fixes**

**Merge IF:**
1. Priority 1 fixes applied (favor tracker filtering)
2. Priority 2 fixes applied (stat scaling nerf)
3. Priority 3 fixes applied (replace Warcry)

**Post-merge TODO:**
- Priority 4: Add conquest activities (Phase 2)
- Priority 5: Monitor favor earning rates, adjust if needed
- Gather player feedback on balance

**Timeline**:
- Fixes: 2-3 hours
- Testing: 1-2 hours
- Documentation: 30 minutes

**Risk**: Low (fixes are small, well-defined)

---

## Summary of Issues

| Issue | Severity | Fix Complexity | Priority |
|-------|----------|----------------|----------|
| Favor tracker allows domestic animal kills | High | Low (10 lines) | P1 |
| Combat stat scaling too high | High | Low (change numbers) | P1 |
| Warcry overlaps with Wild domain | Medium | Medium (new effect) | P2 |
| Only one activity (combat) | Medium | High (new features) | P3 |
| Favor earning rate too fast | Low | Low (adjust values) | P4 |
| Thematic tension with mod | Low | N/A (design decision) | Discussion |

**Total estimated fix time**: 3-4 hours for P1-P2 issues.