# Civilization Milestone Progression Implementation Plan

**Status**: In Progress (Phases 1-3 Complete, Phase 4 Partial)
**Created**: 2026-01-23
**Updated**: 2026-01-27
**Target Start**: After this planning phase

## Overview

Implementation of a milestone-based progression system for civilizations. Players naturally unlock milestones through collaborative gameplay, and each major milestone increases the civilization's **Rank** by 1. This provides cumulative benefits and a sense of progression without complex mechanics.

### Design Principles

- **Discoverable**: Players unlock milestones through natural gameplay
- **Meaningful**: Each milestone provides tangible rewards (rank bump, cosmetics, or stat bonuses)
- **Stackable**: Multiple milestone bonuses compound for real progression feel
- **Simple data model**: Just count completed milestones for rank
- **No complexity creep**: One-time payouts, no farming, no state bloat

---

## Core Concept

**Civilization Rank = Total number of major milestones completed**

Each completed major milestone:
- Increments civilization rank by +1
- Awards one-time prestige payout to founding religion
- Grants permanent benefit (holy site slot, blessing, or stat bonus)
- May provide cosmetic reward (title/icon display)

---

## Milestone Definitions

### Major Milestones (Rank-Advancing)

| Milestone ID | Name | Trigger | Rank Reward | Permanent Benefit | Notes |
|---|---|---|---|---|---|
| `first_alliance` | First Alliance | Add 2nd religion to civilization | +1 | +5% prestige generation for civ | Foundation of civilization |
| `holy_expansion` | Holy Expansion | Create 2 holy sites in civilization | +1 | +1 holy site slot (increases cap) | Multi-site coordination |
| `ritual_mastery` | Ritual Mastery | Complete 5 rituals across civ | +1 | Unlock civilization-wide blessing | Reward for cooperation |
| `united_front` | United Front | Get all 4 domains represented | +1 | +10% favor generation for civ | Max diversity bonus (civs cap at 4 religions) |
| `population_milestone` | Population Surge | Reach 25+ members across civ | +1 | +2% all prestige/favor rewards | Scale incentive |

### Minor Milestones (One-Time Payouts)

| Milestone ID | Name | Trigger | Prestige Payout | Duration | Notes |
|---|---|---|---|---|---|
| `tier_triumph` | Tier Triumph | Upgrade any holy site to Tier 2 | 300 prestige | One-time | First tier upgrade |
| `diplomatic_victory` | Diplomatic Victory | Form NAP/Alliance with another civ | 200 prestige | One-time | Diplomacy integration |
| `war_heroes` | War Heroes | Achieve 50 PvP kills during active wars | +5% conquest rewards | 7 days | Temporary buff, tracks cumulative war kills |
| `cultural_monument` | Cultural Monument | Unlock all 5 major milestones | Permanent +2% prestige multiplier | Permanent | Ultimate achievement |

---

## Data Model

### CivilizationData Updates

```csharp
[ProtoContract]
public class CivilizationData
{
    // Existing fields (ProtoMember 1-10 already used)...
    // 1: CivId, 2: Name, 3: FounderUID, 4: FounderReligionUID,
    // 5: _memberReligionIds, 6: CreatedDate, 7: DisbandedDate,
    // 8: MemberCount, 9: Icon, 10: Description

    [ProtoMember(11)]
    public int Rank { get; set; } = 0;

    [ProtoMember(12)]
    public HashSet<string> CompletedMilestones { get; set; } = new();

    [ProtoMember(13)]
    public int WarKillCount { get; set; } = 0;  // Cumulative PvP kills during wars

    [ProtoMember(14)]
    public HashSet<string> UnlockedBlessings { get; set; } = new();  // Civ-wide blessings
}
```

### Milestone Definitions (JSON)

```json
{
  "version": 1,
  "milestones": [
    {
      "id": "first_alliance",
      "name": "First Alliance",
      "type": "major",
      "trigger": {
        "type": "religion_count",
        "threshold": 2
      },
      "rankReward": 1,
      "permanentBenefit": {
        "type": "prestige_multiplier",
        "amount": 0.05
      },
      "prestigePayout": 250
    },
    {
      "id": "united_front",
      "name": "United Front",
      "type": "major",
      "trigger": {
        "type": "domain_count",
        "threshold": 4
      },
      "rankReward": 1,
      "permanentBenefit": {
        "type": "favor_multiplier",
        "amount": 0.10
      },
      "prestigePayout": 500
    },
    {
      "id": "ritual_mastery",
      "name": "Ritual Mastery",
      "type": "major",
      "trigger": {
        "type": "ritual_count",
        "threshold": 5
      },
      "rankReward": 1,
      "permanentBenefit": {
        "type": "unlock_blessing",
        "blessingId": "civilization_cooperation"
      },
      "prestigePayout": 400
    },
    {
      "id": "war_heroes",
      "name": "War Heroes",
      "type": "minor",
      "trigger": {
        "type": "war_kill_count",
        "threshold": 50
      },
      "temporaryBenefit": {
        "type": "conquest_multiplier",
        "amount": 0.05,
        "durationDays": 7
      },
      "prestigePayout": 300
    }
  ]
}
```

### Civilization Cooperation Blessing Definition

Add to `assets/divineascension/config/blessings/civilization.json`:

```json
{
  "domain": "civilization",
  "version": 1,
  "blessings": [
    {
      "id": "civilization_cooperation",
      "name": "Bond of Unity",
      "description": "The combined faith of allied religions strengthens all members.",
      "kind": "Civilization",
      "category": "Passive",
      "statModifiers": {
        "favorMultiplier": 0.03,
        "prestigeMultiplier": 0.03
      },
      "specialEffects": []
    }
  ]
}
```

---

## Implementation Phases

### Phase 1: Backend Data & Infrastructure (3-4 days)

**Goal**: Create data model, events infrastructure, and milestone detection system

#### Task 1.1: Update CivilizationData
**Files**:
- `Data/CivilizationData.cs` - Add `Rank`, `CompletedMilestones`, `WarKillCount`, `UnlockedBlessings` fields

**Changes**:
- Add ProtoBuf fields (members 11-14) for milestone tracking
- Persistence automatically handled by existing save/load

#### Task 1.2: Add CivilizationManager Events
**Files**:
- `Systems/CivilizationManager.cs` - Add new events
- `Systems/Interfaces/ICivilizationManager.cs` - Update interface

**New Events**:
```csharp
public event Action<string, string>? OnReligionAdded;    // civId, religionId
public event Action<string, string>? OnReligionRemoved;  // civId, religionId
```

**Changes**:
- Fire `OnReligionAdded` in `AcceptInvite()` after religion joins
- Fire `OnReligionRemoved` in `LeaveReligion()`, `KickReligion()`, and cascading cleanup
- These events enable milestone detection without tight coupling

#### Task 1.3: Create CivilizationMilestoneManager
**Files to Create**:
- `Systems/CivilizationMilestoneManager.cs` (new manager)
- `Systems/Interfaces/ICivilizationMilestoneManager.cs` (interface)
- `Services/MilestoneDefinitionLoader.cs` (load JSON definitions)
- `Models/MilestoneDefinition.cs` (data model for milestones)
- `Models/MilestoneTrigger.cs` (trigger configuration)
- `Models/MilestoneBenefit.cs` (benefit configuration)

**Responsibilities**:
- Track milestone completion per civilization
- Detect milestone triggers via event subscriptions
- Apply rewards (rank bump, prestige payout, stat bonuses)
- Query civilization progress and active bonuses
- Fire events on milestone unlock
- Subscribe to `OnCivilizationDisbanded` for cleanup

**Initialization Order**: Insert at **step 5.5** (after CivilizationManager, before PlayerMessengerService)

**Public API**:
```csharp
public interface ICivilizationMilestoneManager
{
    // Check if milestone is completed
    bool IsMilestoneCompleted(string civId, string milestoneId);

    // Get civilization rank
    int GetCivilizationRank(string civId);

    // Get completed milestones
    IReadOnlySet<string> GetCompletedMilestones(string civId);

    // Get active bonuses for a civilization
    CivilizationBonuses GetActiveBonuses(string civId);

    // Get milestone progress (for UI display)
    MilestoneProgress GetMilestoneProgress(string civId, string milestoneId);

    // Trigger milestone checks (called by other systems)
    void CheckMilestones(string civId);

    // Record war kill for war_heroes tracking
    void RecordWarKill(string civId);

    // Events
    event Action<string, string>? OnMilestoneUnlocked; // civId, milestoneId
    event Action<string, int>? OnRankIncreased; // civId, newRank
}
```

#### Task 1.4: Create CivilizationBonusSystem
**Files to Create**:
- `Systems/CivilizationBonusSystem.cs` - Apply civ-wide multipliers

**Responsibilities**:
- Track active bonuses per civilization (prestige multiplier, favor multiplier, etc.)
- Provide `GetFavorMultiplier(civId)` and `GetPrestigeMultiplier(civId)` methods
- Cache computed multipliers, invalidate on milestone unlock
- Integrate with FavorSystem for favor calculation
- Integrate with ReligionPrestigeManager for prestige calculation

**Integration Points**:
- `FavorSystem.AwardFavor()` - Multiply by civilization bonus
- `ReligionPrestigeManager.AwardPrestige()` - Multiply by civilization bonus

#### Task 1.5: Add BlessingKind.Civilization
**Files**:
- `Models/Enum/BlessingKind.cs` - Add `Civilization` enum value
- `Systems/BlessingEffectSystem.cs` - Handle civilization-wide blessing application

**Changes**:
```csharp
public enum BlessingKind
{
    Player = 0,
    Religion = 1,
    Civilization = 2  // NEW
}
```

- Add `GetCivilizationStatModifiers(string civId)` method
- Modify `GetCombinedStatModifiers()` to include civilization bonuses
- Add `_civilizationModifierCache` for caching

#### Task 1.6: Define Network Packets
**Files to Create**:
- `Network/MilestoneProgressRequestPacket.cs`
- `Network/MilestoneProgressResponsePacket.cs`
- `Network/MilestoneUnlockedPacket.cs`

**Packet Definitions**:
```csharp
[ProtoContract]
public class MilestoneProgressRequestPacket
{
    [ProtoMember(1)] public string CivId { get; set; }
}

[ProtoContract]
public class MilestoneProgressResponsePacket
{
    [ProtoMember(1)] public string CivId { get; set; }
    [ProtoMember(2)] public int Rank { get; set; }
    [ProtoMember(3)] public List<string> CompletedMilestones { get; set; }
    [ProtoMember(4)] public Dictionary<string, MilestoneProgressDto> Progress { get; set; }
}

[ProtoContract]
public class MilestoneUnlockedPacket
{
    [ProtoMember(1)] public string CivId { get; set; }
    [ProtoMember(2)] public string MilestoneId { get; set; }
    [ProtoMember(3)] public string MilestoneName { get; set; }
    [ProtoMember(4)] public int NewRank { get; set; }
    [ProtoMember(5)] public int PrestigePayout { get; set; }
}
```

#### Task 1.7: Load Milestone Definitions
**Files to Create**:
- `assets/divineascension/config/milestones.json` - Milestone definitions
- `assets/divineascension/config/blessings/civilization.json` - Civ blessings

**Content**:
- All milestone definitions with triggers, rewards, benefits
- Tunable prestige payouts
- Civilization cooperation blessing definition

---

### Phase 2: Hook Into Existing Systems (2-3 days)

**Goal**: Detect when milestones should trigger

#### Task 2.1: Holy Site Count and Tier Upgrade Detection
**Files**:
- `Systems/RitualProgressManager.cs` - Already has tier upgrade logic
- `Systems/HolySiteManager.cs` - Track holy site creation

**Changes**:
- After tier upgrade completes, resolve religion → civilization, call `_milestoneManager.CheckMilestones(civId)`
- After holy site consecration, resolve religion → civilization, call `_milestoneManager.CheckMilestones(civId)`
- Trigger checks for `tier_triumph`, `holy_expansion`, and `ritual_mastery`

**Resolution Pattern**:
```csharp
var civId = _civilizationManager.GetCivilizationByReligion(religionUID)?.CivId;
if (civId != null)
{
    _milestoneManager.CheckMilestones(civId);
}
```

#### Task 2.2: War Kill Tracking
**Files**:
- `Systems/PvPManager.cs` - Handles PvP kill rewards

**Changes**:
- After PvP kill during active war, call `_milestoneManager.RecordWarKill(civId)`
- War status already checked via `_diplomacyManager.AreAtWar()`
- Increment `CivilizationData.WarKillCount` and check `war_heroes` threshold (50 kills)

**Implementation**:
```csharp
// In PvPManager.OnPlayerKill()
if (areAtWar && killerCivId != null)
{
    _milestoneManager.RecordWarKill(killerCivId);
}
```

#### Task 2.3: Member Count Tracking
**Files**:
- `Systems/CivilizationManager.cs` - Track member count
- `Systems/ReligionManager.cs` - Subscribe to membership changes

**Changes**:
- Subscribe to `ReligionManager.OnPlayerJoinsReligion` and `OnPlayerLeavesReligion`
- When player joins/leaves a religion that's in a civilization, call `UpdateMemberCounts()` then `CheckMilestones(civId)`
- Trigger `population_milestone` when `MemberCount >= 25`

#### Task 2.4: Domain Diversity Detection
**Files**:
- `Systems/CivilizationMilestoneManager.cs` - Check domains on religion added

**Changes**:
- Subscribe to `CivilizationManager.OnReligionAdded`
- Query `GetCivDeityTypes(civId)` to count unique domains
- If domain count reaches 4, trigger `united_front` milestone

#### Task 2.5: Ritual Completion Counting
**Files**:
- `Systems/RitualProgressManager.cs` - Track ritual completions

**Changes**:
- After ritual completion, resolve to civilization and increment ritual count
- Need to track completed ritual count per civilization (add to CivilizationData or compute dynamically)
- Call milestone check when count reaches 5
- Trigger `ritual_mastery` milestone

**Aggregation Logic**:
```csharp
public int GetCompletedRitualCount(string civId)
{
    var civ = _civilizationManager.GetCivilization(civId);
    if (civ == null) return 0;

    int count = 0;
    foreach (var religionId in civ.MemberReligionIds)
    {
        var sites = _holySiteManager.GetReligionHolySites(religionId);
        foreach (var site in sites)
        {
            // Count tier upgrades as completed rituals (tier - 1 = completed rituals)
            count += Math.Max(0, site.RitualTier - 1);
        }
    }
    return count;
}
```

---

### Phase 3: Reward Application (2-3 days)

**Goal**: Apply milestone benefits when unlocked

#### Task 3.1: Rank & Prestige Payout
**Files**:
- `Systems/CivilizationMilestoneManager.cs` - Apply rewards

**Implementation**:
- On milestone unlock:
  - Increment `CivilizationData.Rank`
  - Add prestige payout to founding religion via `_prestigeManager.AwardPrestige(founderReligionUID, amount)`
  - Log activity to founding religion's activity log
  - Fire `OnMilestoneUnlocked` and `OnRankIncreased` events
  - Broadcast `MilestoneUnlockedPacket` to all civilization members

#### Task 3.2: Stat Bonus Application via CivilizationBonusSystem
**Files**:
- `Systems/CivilizationBonusSystem.cs` - Compute and cache bonuses
- `Systems/FavorSystem.cs` - Apply favor multiplier
- `Systems/ReligionPrestigeManager.cs` - Apply prestige multiplier

**Changes**:
- `CivilizationBonusSystem.GetFavorMultiplier(civId)` returns cumulative favor bonus (e.g., 1.12 for +10% + +2%)
- `CivilizationBonusSystem.GetPrestigeMultiplier(civId)` returns cumulative prestige bonus
- Integrate into `FavorSystem.AwardFavor()`:
```csharp
var civMultiplier = _civBonusSystem.GetFavorMultiplier(civId);
finalFavor *= civMultiplier;
```

#### Task 3.3: Holy Site Slot Unlocking
**Files**:
- `Systems/HolySiteManager.cs` - Modify `GetMaxSitesForReligion()`

**Changes**:
- Query civilization's `holy_expansion` milestone status
- If completed, add +1 to max sites calculation
- Formula: `Math.Min((int)religion.PrestigeRank + 1 + civHolySiteBonus, MAX_SITES_CAP)`

**Implementation**:
```csharp
public int GetMaxSitesForReligion(string religionUID)
{
    var religion = _religionManager.GetReligion(religionUID);
    if (religion == null) return 0;

    int baseSites = Math.Min((int)religion.PrestigeRank + 1, MAX_SITES_PER_TIER);

    // Check for civilization holy site bonus
    var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
    if (civ != null && _milestoneManager.IsMilestoneCompleted(civ.CivId, "holy_expansion"))
    {
        baseSites += 1;
    }

    return Math.Min(baseSites, MAX_SITES_PER_TIER + 1); // Cap at 6 with milestone
}
```

#### Task 3.4: Civilization Blessing Unlocking
**Files**:
- `Systems/BlessingRegistry.cs` - Load civilization blessings
- `Systems/BlessingEffectSystem.cs` - Apply to all civ members
- `Systems/CivilizationMilestoneManager.cs` - Unlock blessings

**Changes**:
- When `ritual_mastery` unlocks, add `civilization_cooperation` to `CivilizationData.UnlockedBlessings`
- `BlessingEffectSystem.GetCivilizationStatModifiers(civId)` returns modifiers from unlocked civ blessings
- `RefreshCivilizationBlessings(civId)` invalidates cache and refreshes all members

---

### Phase 4: UI Integration (2-3 days)

**Goal**: Show milestone progress and rewards to players

#### Task 4.1: Milestone Progress Tracker UI
**Files to Create**:
- `GUI/UI/Utilities/CivilizationProgressHelper.cs` - Format milestone info for display
- `GUI/State/MilestoneProgressState.cs` - Manage milestone display state
- `GUI/UI/ViewModels/MilestoneProgressViewModel.cs` - ViewModel for milestone display

**Features**:
- Show completed vs. pending milestones
- Display progress toward next milestone (e.g., "2/5 rituals completed")
- Show active bonuses from completed milestones
- Rank display (e.g., "Rank 3 / 5")

#### Task 4.2: Civilization Tab Enhancement
**Files**:
- `GUI/UI/Renderers/CivilizationTabRenderer.cs` - Render milestone section

**Changes**:
- Add "Milestones" section to civilization info display
- Show:
  - Current rank with visual indicator
  - Completed milestones (with icons/checkmarks)
  - Next milestone and progress toward it
  - Active bonuses (formatted as stat modifiers: "+5% Prestige, +10% Favor")

#### Task 4.3: Milestone Notification
**Files**:
- `Systems/Networking/Client/MilestoneNetworkHandler.cs` - Handle milestone packets
- `GUI/State/GuiDialogState.cs` - Subscribe to events

**Changes**:
- Register handler for `MilestoneUnlockedPacket`
- Show toast notification on milestone unlock
- Example: "🏆 Your civilization reached Rank 2! Holy site slots increased."

#### Task 4.4: Activity Log Entry
**Files**:
- `Systems/ActivityLogManager.cs` - Already tracks activity

**Changes**:
- Log milestone completions to **founding religion's** activity log
- Example: "Civilization reached Ritual Mastery! Unlocked Bond of Unity blessing."
- Include prestige payout amount in log entry

---

### Phase 5: Testing & Polish (1-2 days)

**Goal**: Ensure milestones work and feel rewarding

#### Task 5.1: Unit Tests
**Files to Create**:
- `Tests/Systems/CivilizationMilestoneManagerTests.cs`
- `Tests/Systems/CivilizationBonusSystemTests.cs`

**Test Cases**:
- Milestone detection triggers correctly for each trigger type
- Rank increments appropriately
- Prestige payouts awarded to founding religion
- Stat bonuses computed and cached correctly
- Bonuses persist across save/load
- Multiple milestones stack properly (additive)
- Cleanup occurs on civilization disband

#### Task 5.2: Integration Tests
**Files to Create**:
- `Tests/Systems/CivilizationMilestoneIntegrationTests.cs`

**Test Cases**:
- Add 2nd religion → `first_alliance` triggers
- Add religions until 4 domains → `united_front` triggers
- Build up 25 members → `population_milestone` triggers
- Complete 5 rituals → `ritual_mastery` triggers and blessing unlocks
- 50 PvP kills during war → `war_heroes` triggers
- Blessings apply to all members in civilization
- Holy site slot cap increases correctly

#### Task 5.3: Manual Testing
**Test Plan**:
- Create civilization and watch milestones trigger
- Verify prestige payouts arrive at founding religion
- Check stat bonuses appear in player stats
- Verify holy site slot increases
- Test blessing unlock and application
- Verify persistence (save/load)
- Test UI displays all information correctly
- Test edge cases (religion leaves mid-progress, etc.)

#### Task 5.4: Balance Tweaking
**Review**:
- Are prestige payouts reasonable?
- Do bonuses feel meaningful?
- Are thresholds (25 members, 5 rituals, 50 kills) achievable?
- Does progression feel rewarding?

---

## Integration Points

### With Existing Systems

1. **CivilizationManager**
   - Add `OnReligionAdded`, `OnReligionRemoved` events (NEW)
   - Subscribe to these events for milestone detection
   - Subscribe to `OnCivilizationDisbanded` for cleanup

2. **ReligionManager**
   - Subscribe to `OnPlayerJoinsReligion`, `OnPlayerLeavesReligion` for member counting
   - Use existing player-to-religion index for lookups

3. **RitualProgressManager**
   - Call milestone check after ritual completion
   - Aggregate ritual counts across civilization religions

4. **PvPManager**
   - Call `RecordWarKill(civId)` after PvP kill during active war
   - Requires war status check from DiplomacyManager

5. **BlessingEffectSystem**
   - Add `BlessingKind.Civilization` support
   - Apply civilization bonuses to stat modifiers
   - Cache civilization modifier calculations

6. **FavorSystem**
   - Apply civilization favor multiplier in `AwardFavor()`
   - Query `CivilizationBonusSystem.GetFavorMultiplier()`

7. **ReligionPrestigeManager**
   - Apply civilization prestige multiplier in `AwardPrestige()`
   - Query `CivilizationBonusSystem.GetPrestigeMultiplier()`

8. **HolySiteManager**
   - Modify `GetMaxSitesForReligion()` to include civ milestone bonus
   - Query milestone completion for `holy_expansion`

9. **ActivityLogManager**
   - Log milestone completions to founding religion's activity log

### New Systems

1. **CivilizationMilestoneManager**
   - Central hub for milestone logic
   - Detects, tracks, and rewards milestones
   - Fires events for UI updates

2. **CivilizationBonusSystem**
   - Computes and caches civilization-wide multipliers
   - Provides favor/prestige multiplier queries

3. **MilestoneDefinitionLoader**
   - Load JSON definitions on startup
   - Cache definitions for performance

---

## Data Persistence

### Save Format

Milestones persist in `CivilizationData`:
- `Rank` (int) - Number of major milestones completed
- `CompletedMilestones` (HashSet<string>) - IDs of all unlocked milestones
- `WarKillCount` (int) - Cumulative PvP kills during wars
- `UnlockedBlessings` (HashSet<string>) - Civilization-wide blessing IDs

Existing ProtoBuf serialization handles persistence automatically via `CivilizationWorldData`.

### Migration

- New civilizations start at Rank 0
- Existing civilizations get Rank 0 on first load (no retroactive milestones)
- `WarKillCount` defaults to 0 for existing civilizations
- Optional: Admin command to manually set milestone state for testing

---

## Edge Cases

### Religion Leaves Civilization Mid-Progress
- Milestone progress is civilization-level, not religion-level
- If a religion leaves, progress toward uncompleted milestones may decrease
- Completed milestones remain completed (no regression)
- Example: If civ has 4 domains and one religion leaves, `united_front` stays completed

### Civilization Founder Transfer
- Not currently supported (founders cannot transfer)
- If implemented later, milestone data stays with civilization, not founder

### Religion Deletion Cascade
- When religion is deleted, it's automatically removed from civilization
- May affect member count and domain count
- Completed milestones are not revoked

### Multiple Milestones Trigger Simultaneously
- Process in definition order
- Each milestone awards separately
- All notifications sent to players

---

## Admin Commands

### Testing & Debugging

```
/civ milestone status [civname]          # Show milestone progress
/civ milestone complete <civname> <id>   # Force-complete milestone (admin only)
/civ milestone reset <civname>           # Reset all milestones (admin only)
/civ milestone setrank <civname> <rank>  # Set rank directly (admin only)
```

### Queries

```
/civ milestone info <civname>            # Show detailed milestone info
/civ milestone bonuses <civname>         # Show active bonuses
```

---

## Success Criteria

- ✅ All milestone definitions load from JSON
- ✅ Milestone triggers detect correctly in all scenarios
- ✅ Rank increases are persistent across save/load
- ✅ Prestige payouts awarded to founding religion on unlock
- ✅ Stat bonuses (favor/prestige multipliers) apply immediately
- ✅ Holy site slots increase with `holy_expansion` milestone
- ✅ Civilization blessings unlock and apply to all members
- ✅ UI shows milestone progress clearly
- ✅ Players receive notifications on milestone unlock
- ✅ Cleanup occurs when civilization disbands
- ✅ All tests pass (unit + integration + manual)
- ✅ No performance regression
- ✅ Build succeeds with 0 errors

---

## Timeline Estimate

| Phase | Tasks | Estimated Duration |
|-------|-------|------------------|
| Phase 1 | Data model + infrastructure + events | 3-4 days |
| Phase 2 | System integration hooks | 2-3 days |
| Phase 3 | Reward application | 2-3 days |
| Phase 4 | UI integration | 2-3 days |
| Phase 5 | Testing + polish | 1-2 days |
| **Total** | **5 phases** | **10-15 days** |

---

## Risk Mitigation

### Potential Issues

| Risk | Mitigation |
|------|-----------|
| Milestones don't trigger | Comprehensive trigger detection tests in Phase 2 |
| Overlapping rewards | Clear definition of each milestone's unique benefit |
| Performance overhead | Cache milestone definitions and bonus calculations |
| Persistence bugs | Early persistence testing in Phase 1 |
| UI clutter | Clean milestone section design, collapse if needed |
| Unbalanced thresholds | Manual testing and player feedback in Phase 5 |
| Race conditions on events | Use same-thread event firing, no async |
| Cache invalidation bugs | Explicit invalidation on all state changes |

---

## Future Extensions

### Possible Future Additions (Post-MVP)

- **Seasonal milestones** - Unique milestones each season
- **Challenge milestones** - Optional hard challenges for bonus rewards
- **Civilization achievements** - Cosmetic badges/titles for milestones
- **Shared treasure** - Players can pool resources via milestone treasury
- **Custom milestones** - Admins define civilization-specific goals
- **Retroactive milestone detection** - Scan existing civs for completed milestones

---

## Open Questions (Resolved)

- ~~Should existing civilizations retroactively earn milestones?~~ **No** - Start fresh at Rank 0
- ~~Should milestone rewards scale with civilization size?~~ **No** - Fixed rewards for simplicity
- ~~Should failed war attempts count toward war milestone?~~ **N/A** - Changed to PvP kill tracking
- ~~Should we have seasonal milestone resets?~~ **Post-MVP** - Consider for future

---

## Changelog

### 2026-01-27 (Plan Review Updates)
- Fixed ProtoMember numbers (11-14 instead of 10-11)
- Changed `united_front` from 5 domains to 4 domains (max possible with 4-religion cap)
- Replaced `war_heroes` war victory tracking with PvP kill tracking (50 kills threshold)
- Added `OnReligionAdded` and `OnReligionRemoved` events to CivilizationManager
- Added CivilizationBonusSystem for favor/prestige multipliers
- Added BlessingKind.Civilization enum value
- Specified initialization order (step 5.5)
- Added network packet definitions
- Added edge cases section
- Added cleanup on civilization disband
- Fixed Task 2.1 to use civId instead of religionUID
- Clarified activity log goes to founding religion
- Defined `civilization_cooperation` blessing
- Extended timeline estimate to 10-15 days

### 2026-01-23 (Initial Planning)
- Created high-level implementation plan
- Defined milestone list and progression model
- Broke down into 5 implementation phases
- Documented data model and integration points
