# Civilization Milestone Progression Implementation Plan

**Status**: Planning
**Created**: 2026-01-23
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
| `united_front` | United Front | Get all 5 domains represented | +1 | +10% favor generation for civ | Diversity bonus |
| `population_milestone` | Population Surge | Reach 25+ members across civ | +1 | +2% all prestige/favor rewards | Scale incentive |

### Minor Milestones (One-Time Payouts)

| Milestone ID | Name | Trigger | Prestige Payout | Duration | Notes |
|---|---|---|---|---|---|
| `tier_triumph` | Tier Triumph | Upgrade any holy site to Tier 2 | 300 prestige | One-time | First tier upgrade |
| `diplomatic_victory` | Diplomatic Victory | Form NAP/Alliance with another civ | 200 prestige | One-time | PvP integration |
| `war_heroes` | War Heroes | Win 3 wars against other civs | +5% conquest rewards | 7 days | Temporary buff |
| `cultural_monument` | Cultural Monument | Unlock all 5 major milestones | Permanent +2% prestige multiplier | Permanent | Ultimate achievement |

---

## Data Model

### CivilizationData Updates

```csharp
[ProtoContract]
public class CivilizationData
{
    // Existing fields...
    [ProtoMember(10)]
    public int Rank { get; set; } = 0;

    [ProtoMember(11)]
    public HashSet<string> CompletedMilestones { get; set; } = new();
}
```

### Milestone Definitions (JSON)

```json
{
  "milestones": [
    {
      "id": "first_alliance",
      "name": "First Alliance",
      "type": "major",
      "trigger": "religion_count_reaches_2",
      "rankReward": 1,
      "permanentBenefit": {
        "type": "prestige_multiplier",
        "amount": 0.05
      },
      "prestigePayout": 250
    },
    {
      "id": "ritual_mastery",
      "name": "Ritual Mastery",
      "type": "major",
      "trigger": "rituals_completed_reaches_5",
      "rankReward": 1,
      "permanentBenefit": {
        "type": "unlock_blessing",
        "blessingId": "civilization_cooperation"
      }
    }
  ]
}
```

---

## Implementation Phases

### Phase 1: Backend Data & Manager (2-3 days)

**Goal**: Create data model and milestone detection system

#### Task 1.1: Update CivilizationData
**Files**:
- `Data/CivilizationData.cs` - Add `Rank` and `CompletedMilestones` fields

**Changes**:
- Add ProtoBuf fields for rank and milestone tracking
- Persistence automatically handled by existing save/load

#### Task 1.2: Create CivilizationMilestoneManager
**Files to Create**:
- `Systems/CivilizationMilestoneManager.cs` (new manager)
- `Services/MilestoneDefinitionLoader.cs` (load JSON definitions)
- `Models/MilestoneDefinition.cs` (data model for milestones)

**Responsibilities**:
- Track milestone completion
- Detect milestone triggers
- Apply rewards (rank bump, prestige payout, stat bonuses)
- Query civilization progress
- Fire events on milestone unlock

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

    // Trigger milestone checks (called by other systems)
    void CheckMilestones(string civId);

    // Events
    event Action<string, string>? OnMilestoneUnlocked; // civId, milestoneId
    event Action<string>? OnRankIncreased; // civId
}
```

#### Task 1.3: Integrate with CivilizationManager
**Files**:
- `Systems/CivilizationManager.cs` - Add milestone manager integration

**Changes**:
- Inject `CivilizationMilestoneManager` into CivilizationManager
- Call `CheckMilestones()` after operations that affect civilization state:
  - After religion added (check `first_alliance`)
  - After religion removed (may trigger different logic)
  - After civilization rank change (fire notification)

#### Task 1.4: Load Milestone Definitions
**Files to Create**:
- `assets/divineascension/config/milestones.json` - Milestone definitions

**Content**:
- All milestone definitions with triggers, rewards, benefits
- Tunable prestige payouts

---

### Phase 2: Hook Into Existing Systems (2-3 days)

**Goal**: Detect when milestones should trigger

#### Task 2.1: Holy Site Tier Upgrade Detection
**Files**:
- `Systems/RitualProgressManager.cs` - Already has tier upgrade logic

**Changes**:
- Call `_milestoneManager.CheckMilestones(religionUID)` after tier upgrade
- Trigger checks for `tier_triumph` and `ritual_mastery`

#### Task 2.2: War Completion Detection
**Files**:
- `Systems/DiplomacyManager.cs` - Handles war state changes

**Changes**:
- Track war wins per civilization (not per religion)
- Call milestone check after 3rd win
- Trigger `war_heroes` bonus

#### Task 2.3: Member Count Tracking
**Files**:
- `Systems/CivilizationManager.cs` - Track member count

**Changes**:
- Count members across all religions in civilization
- Call milestone check whenever member count increases
- Trigger `population_milestone` at 25+ members

#### Task 2.4: Domain Diversity Detection
**Files**:
- `Systems/CivilizationManager.cs` - Check domains of member religions

**Changes**:
- After religion added to civilization, check domain representation
- If all 5 domains now present, trigger `united_front` milestone

#### Task 2.5: Ritual Completion Counting
**Files**:
- `Systems/RitualProgressManager.cs` - Track ritual completions

**Changes**:
- Count completed rituals per civilization (not per religion)
- Call milestone check when 5th ritual completed
- Trigger `ritual_mastery` milestone

---

### Phase 3: Reward Application (2-3 days)

**Goal**: Apply milestone benefits when unlocked

#### Task 3.1: Rank & Prestige Payout
**Files**:
- `Systems/CivilizationMilestoneManager.cs` - Apply rewards

**Implementation**:
- On milestone unlock:
  - Increment civilization rank
  - Add prestige payout to founding religion
  - Log activity (if applicable)

#### Task 3.2: Stat Bonus Application
**Files**:
- `Systems/BlessingEffectSystem.cs` - Already applies stat modifiers
- `Systems/CivilizationMilestoneManager.cs` - Register bonuses

**Changes**:
- When milestone grants stat bonus, register with BlessingEffectSystem
- Examples:
  - `+5% prestige generation` → Modify prestige earned
  - `+10% favor generation` → Modify favor earned
  - `+2% all rewards` → Modify both favor and prestige

**Approach**:
- Create `CivilizationBonusModifier` system similar to player stat modifiers
- Store active bonuses per civilization
- Apply during favor/prestige award calculations

#### Task 3.3: Holy Site Slot Unlocking
**Files**:
- `Systems/HolySiteManager.cs` - Respects civilization rank for slot cap
- `Systems/CivilizationPrestigeManager.cs` - Returns prestige rank cap

**Changes**:
- Modify `GetMaxHolySitesForReligion()` to check civilization rank
- If religion is in civilization: return `base_sites_for_rank + civ_rank_bonus`

#### Task 3.4: Blessing Unlocking
**Files**:
- `Systems/BlessingRegistry.cs` - Load civilization blessings
- `Systems/CivilizationMilestoneManager.cs` - Unlock blessings

**Changes**:
- When `ritual_mastery` unlocks, call `BlessingRegistry.UnlockCivilizationBlessing(civId, blessingId)`
- Blessing applies to all members of civilization
- Persist in `CivilizationData.UnlockedBlessings`

---

### Phase 4: UI Integration (2-3 days)

**Goal**: Show milestone progress and rewards to players

#### Task 4.1: Milestone Progress Tracker UI
**Files to Create**:
- `GUI/UI/Utilities/CivilizationProgressHelper.cs` - Format milestone info for display
- `GUI/State/MilestoneProgressState.cs` - Manage milestone display state

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
  - Current rank
  - Completed milestones (with icons/badges)
  - Next milestone and progress toward it
  - Active bonuses (formatted as stat modifiers)

#### Task 4.3: Milestone Notification
**Files**:
- `Systems/CivilizationMilestoneManager.cs` - Fire events
- `GUI/State/GuiDialogState.cs` - Subscribe to events

**Changes**:
- When milestone unlocks, fire event
- Subscribe in UI and show toast notification
- Example: "Your civilization reached Rank 2! Holy site slots increased."

#### Task 4.4: Activity Log Entry
**Files**:
- `Systems/ActivityLogManager.cs` - Already tracks activity

**Changes**:
- Log milestone completions to religion activity log
- Example: "Civilization reached Ritual Mastery! Unlocked cooperation blessing."

---

### Phase 5: Testing & Polish (1-2 days)

**Goal**: Ensure milestones work and feel rewarding

#### Task 5.1: Unit Tests
**Files to Create**:
- `Tests/Systems/CivilizationMilestoneManagerTests.cs`

**Test Cases**:
- Milestone detection triggers correctly
- Rank increments appropriately
- Prestige payouts awarded
- Stat bonuses applied correctly
- Bonuses persist across save/load
- Multiple milestones stack properly

#### Task 5.2: Integration Tests
**Files to Create**:
- `Tests/Systems/CivilizationMilestoneIntegrationTests.cs`

**Test Cases**:
- Create civilization → `first_alliance` triggers
- Add religions across domains → `united_front` triggers
- Build up 25 members → `population_milestone` triggers
- Complete 5 rituals → `ritual_mastery` triggers and blessing unlocks
- Blessings apply to all members

#### Task 5.3: Manual Testing
**Test Plan**:
- Create civilization and watch milestones trigger
- Verify prestige payouts arrive
- Check stat bonuses appear in player stats
- Verify holy site slot increases
- Test blessing unlock and application
- Verify persistence (save/load)
- Test UI displays all information correctly

#### Task 5.4: Balance Tweaking
**Review**:
- Are prestige payouts reasonable?
- Do bonuses feel meaningful?
- Are thresholds (25 members, 5 rituals) achievable?
- Does progression feel rewarding?

---

## Integration Points

### With Existing Systems

1. **CivilizationManager**
   - Hook into `OnReligionAdded`, `OnReligionRemoved` events
   - Call milestone checks after state changes

2. **RitualProgressManager**
   - Call milestone check after ritual completion
   - Track civilization-wide ritual count

3. **DiplomacyManager**
   - Track war outcomes per civilization
   - Call milestone check after 3rd war win

4. **BlessingEffectSystem**
   - Apply civilization bonuses to stat modifiers
   - Unlock civilization blessings

5. **HolySiteManager**
   - Respect civilization rank for holy site slot cap

6. **ActivityLogManager**
   - Log milestone completions

### New Systems

1. **CivilizationMilestoneManager**
   - Central hub for milestone logic
   - Detects, tracks, and rewards milestones

2. **MilestoneDefinitionLoader**
   - Load JSON definitions on startup
   - Cache definitions for performance

---

## Data Persistence

### Save Format

Milestones persist in `CivilizationData`:
- `Rank` (int) - Number of major milestones completed
- `CompletedMilestones` (HashSet<string>) - IDs of all unlocked milestones

Existing ProtoBuf serialization handles persistence automatically.

### Migration

- New civilizations start at Rank 0
- Existing civilizations get Rank 0 on first load (no retroactive milestones)
- Optional: Admin command to manually set milestone state for testing

---

## Admin Commands

### Testing & Debugging

```
/civ milestone status [civname]          # Show milestone progress
/civ milestone complete <civname> <id>   # Force-complete milestone (admin only)
/civ milestone reset <civname>           # Reset all milestones (admin only)
```

### Queries

```
/civ milestone info <civname>             # Show detailed milestone info
```

---

## Success Criteria

- ✅ All milestone definitions load from JSON
- ✅ Milestone triggers detect correctly in all scenarios
- ✅ Rank increases are persistent across save/load
- ✅ Prestige payouts awarded on unlock
- ✅ Stat bonuses apply immediately
- ✅ Holy site slots increase with civilization rank
- ✅ Blessings unlock and apply civilization-wide
- ✅ UI shows milestone progress clearly
- ✅ Players receive notifications on milestone unlock
- ✅ All tests pass (unit + integration + manual)
- ✅ No performance regression
- ✅ Build succeeds with 0 errors

---

## Timeline Estimate

| Phase | Tasks | Estimated Duration |
|-------|-------|------------------|
| Phase 1 | Data model + manager | 2-3 days |
| Phase 2 | System integration hooks | 2-3 days |
| Phase 3 | Reward application | 2-3 days |
| Phase 4 | UI integration | 2-3 days |
| Phase 5 | Testing + polish | 1-2 days |
| **Total** | **5 phases** | **9-14 days** |

---

## Risk Mitigation

### Potential Issues

| Risk | Mitigation |
|------|-----------|
| Milestones don't trigger | Comprehensive trigger detection tests in Phase 2 |
| Overlapping rewards | Clear definition of each milestone's unique benefit |
| Performance overhead | Cache milestone definitions, lazy-load checks |
| Persistence bugs | Early persistence testing in Phase 1 |
| UI clutter | Clean milestone section design, collapse if needed |
| Unbalanced thresholds | Manual testing and player feedback in Phase 5 |

---

## Future Extensions

### Possible Future Additions (Post-MVP)

- **Seasonal milestones** - Unique milestones each season
- **Challenge milestones** - Optional hard challenges for bonus rewards
- **Civilization achievements** - Cosmetic badges/titles for milestones
- **Shared treasure** - Players can pool resources via milestone treasury
- **Custom milestones** - Admins define civilization-specific goals

---

## Open Questions

- Should existing civilizations retroactively earn milestones they've already completed?
- Should milestone rewards scale with civilization size?
- Should failed war attempts count toward war milestone?
- Should we have seasonal milestone resets?

---

## Changelog

### 2026-01-23 (Initial Planning)
- Created high-level implementation plan
- Defined milestone list and progression model
- Broke down into 5 implementation phases
- Documented data model and integration points
