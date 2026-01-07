# Implementation Plan: Simplify PlayerReligionData & Improve Data Consistency

## Overview

This plan addresses the dual-state problem between `ReligionManager` and `PlayerReligionDataManager` by:

1. **Simplifying PlayerReligionData** - Remove redundant cached state (ReligionUID, ActiveDeity, FavorRank,
   LastReligionSwitch)
2. **Making ReligionManager single source of truth** - Add O(1) lookup index for membership
3. **Adding validation** - Enforce invariants at write-time and load-time
4. **Enforcing referential integrity** - Ensure clean deletion cascades

## Implementation Phases

### Phase 1: Add Player-to-Religion Index (FOUNDATION)

**Goal**: Enable O(1) membership lookups and prepare for data simplification

**Files to Modify**:

- `/DivineAscension/Systems/ReligionManager.cs`

**Changes**:

1. Add `Dictionary<string, string> _playerToReligionIndex` field
2. Update `AddMember()` to maintain index
3. Update `RemoveMember()` to maintain index
4. Add `RebuildPlayerIndex()` method called after `LoadAllReligions()`
5. Add `GetPlayerReligionUID(playerUID)` public method for O(1) lookup
6. Optimize `GetPlayerReligion()` to use index

**Why First**: Non-breaking, performance improvement, required for Phase 2

---

### Phase 2: Simplify PlayerReligionData (CORE REFACTOR)

**Goal**: Remove redundant state and clarify purpose with rename

**Files to Create**:

- `/DivineAscension/Data/PlayerProgressionData.cs` (replaces PlayerReligionData.cs)
- `/DivineAscension/Data/PlayerDataMigration.cs` (migration logic)

**Files to Modify**:

- `/DivineAscension/Systems/PlayerReligionDataManager.cs` → Rename to `PlayerProgressionDataManager.cs`
- `/DivineAscension/Systems/Interfaces/IPlayerReligionDataManager.cs` → Rename to `IPlayerProgressionDataManager.cs`
- All network handlers, UI code, tests referencing old types

**Data Model Changes** (PlayerProgressionData):

**REMOVE**:

- ❌ `ReligionUID` - Use `ReligionManager.GetPlayerReligionUID()` instead
- ❌ `ActiveDeity` - Use `ReligionManager.GetPlayerReligion().Deity` instead
- ❌ `FavorRank` - Now computed property from `TotalFavorEarned`
- ❌ `LastReligionSwitch` - 7-day cooldown doesn't exist
- ❌ `KillCount` - Move to separate stats if needed later

**CHANGE**:

- `Dictionary<string, bool> UnlockedBlessings` → `HashSet<string>` (simpler)
- Bump `DataVersion` to 3

**KEEP**:

- ✅ `PlayerUID`
- ✅ `Favor` (current points)
- ✅ `TotalFavorEarned` (lifetime)
- ✅ `AccumulatedFractionalFavor` (passive generation)
- ✅ `UnlockedBlessings` (now HashSet)

**Migration Logic**:

- Detect `DataVersion == 2` on load
- Extract `ReligionUID` from old data and validate with ReligionManager
- Convert blessing dictionary to HashSet
- Log migration and save immediately

**Manager Updates**:

- `JoinReligion()` - Remove setting ReligionUID, only call `ReligionManager.AddMember()`
- `LeaveReligion()` - Query religion from ReligionManager, then call `RemoveMember()`
- Add `GetPlayerDeity()` and `HasReligion()` helper methods that query ReligionManager
- Update `LoadPlayerData()` to handle migration from v2 to v3

**Why Second**: Requires Phase 1 index, breaks compatibility but with automatic migration

---

### Phase 3: Enforce Invariants (VALIDATION)

**Goal**: Prevent inconsistencies at write-time with automatic repair

**Files to Create**:

- `/DivineAscension/Systems/MembershipValidator.cs`

**Files to Modify**:

- `/DivineAscension/Systems/PlayerProgressionDataManager.cs` (add validation to join/leave)

**Validation Logic**:

- After `JoinReligion()`: Verify `GetPlayerReligionUID()` returns correct value, auto-repair if not
- After `LeaveReligion()`: Verify `GetPlayerReligionUID()` returns null, auto-repair if not
- Log all validation failures and repairs
- Use existing `RepairMembershipConsistency()` for fixes

**Why Third**: Requires Phase 2's simplified model, adds safety without breaking functionality

---

### Phase 4: Load-Time Validation & Deletion Constraints (SAFETY)

**Goal**: Catch issues at startup and enforce clean deletion

**Files to Modify**:

- `/DivineAscension/Systems/ReligionManager.cs` (add `ValidateAllMemberships()` and update `DeleteReligion()`)
- `/DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (call validation after init)

**Load-Time Validation**:

- In `DivineAscensionSystemInitializer.cs` after both managers initialize
- Call `ReligionManager.ValidateAllMemberships(playerDataManager)`
- Log summary: total players, consistent, repaired, failed
- Auto-repair using existing `RepairMembershipConsistency()`

**Deletion Constraints**:

- Update `DeleteReligion()` to remove all members first (auto-removal)
- Verify member count is 0 before deletion
- Log member removal during deletion
- Abort deletion if member removal fails

**Why Fourth**: Requires all previous phases, provides final safety layer

---

## Documentation Updates

**Files to Update**:

1. `/docs/PLAYER_GUIDE.md` - Remove all "7-day religion switch cooldown" references
2. `/CLAUDE.md` - Update architecture section with new data model
3. Test files - Update 46 test files referencing old types

---

## Testing Strategy

**Phase 1 Tests**:

- Index updates on AddMember/RemoveMember
- Index rebuild on load
- O(1) lookup performance

**Phase 2 Tests**:

- Migration from v2 to v3
- HashSet blessing storage
- Computed FavorRank property
- All existing tests updated to use new types

**Phase 3 Tests**:

- Validation detects inconsistencies
- Auto-repair works correctly
- Logging captures violations

**Phase 4 Tests**:

- Load-time validation catches issues
- DeleteReligion removes all members
- Deletion aborted if members can't be removed

---

## Migration Path

**For Existing Saves**:

1. On world load, `PlayerProgressionDataManager.LoadPlayerData()` attempts v3 first
2. If v3 fails, falls back to deserializing as v2 format
3. Calls `PlayerDataMigration.MigrateFromV2()` to convert
4. Validates ReligionUID from old data matches ReligionManager (uses ReligionManager as authority)
5. Saves immediately as v3 format
6. Logs all migrations

**Data Safety**:

- Old v2 saves remain readable
- Migration is automatic and logged
- Admin repair command still available: `/religion admin repair`

---

## Performance Impact

**Before**:

- `GetPlayerReligion()`: O(n) - scan all religions
- `PlayerReligionData`: ~200-300 bytes per player
- Dual state with consistency issues

**After**:

- `GetPlayerReligion()`: O(1) - index lookup
- `PlayerProgressionData`: ~150-200 bytes per player (30-40% smaller)
- Single source of truth with validation

---

## Risk Assessment

**Low Risk**: Phase 1 (additive only), Phase 3 (validation wrapper)
**Medium Risk**: Phase 2 (migration, but automatic and tested)
**Low-Medium Risk**: Phase 4 (load-time validation)

**Mitigation**:

- Comprehensive test coverage (100+ tests to update/create)
- All actions logged for debugging
- Incremental deployment (can merge phases separately)
- Admin repair command exists as fallback
- v2 saves still readable during migration

---

## Estimated Effort

- Phase 1: 4-6 hours
- Phase 2: 8-10 hours
- Phase 3: 3-4 hours
- Phase 4: 3-4 hours
- Documentation & tests: 2-3 hours
- **Total**: ~20-27 hours

---

## Critical Files Summary

**Data Models**:

- `/DivineAscension/Data/PlayerProgressionData.cs` (new, replaces PlayerReligionData.cs)
- `/DivineAscension/Data/PlayerDataMigration.cs` (new)

**Managers**:

- `/DivineAscension/Systems/ReligionManager.cs` (index, validation, deletion)
- `/DivineAscension/Systems/PlayerProgressionDataManager.cs` (rename from PlayerReligionDataManager)
- `/DivineAscension/Systems/MembershipValidator.cs` (new)

**Initialization**:

- `/DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (load-time validation hook)

**Documentation**:

- `/docs/PLAYER_GUIDE.md` (remove cooldown references)
- `/CLAUDE.md` (update architecture docs)
