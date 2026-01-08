# Player Progression Refactoring - Task Checklist

This checklist breaks down the implementation plan from `player-progression-refactor.md` into specific, actionable
tasks.

## Phase 1: Add Player-to-Religion Index (FOUNDATION) ✅

- [x] Add `_playerToReligionIndex` Dictionary field to ReligionManager
- [x] Update `AddMember()` to maintain player-to-religion index
- [x] Update `RemoveMember()` to maintain player-to-religion index
- [x] Add `RebuildPlayerIndex()` method called after `LoadAllReligions()`
- [x] Add `GetPlayerReligionId()` public method for O(1) lookup
- [x] Optimize `GetPlayerReligion()` to use the new index
- [x] Write tests for index updates on AddMember/RemoveMember
- [x] Write tests for index rebuild on load and O(1) performance

## Phase 2: Simplify PlayerReligionData (CORE REFACTOR) ✅

### Data Model & Migration

- [x] Create `PlayerProgressionData.cs` with simplified model
    - Remove: ReligionUID, ActiveDeity, FavorRank, LastReligionSwitch, KillCount
    - Change: Dictionary to HashSet for UnlockedBlessings
    - Keep: PlayerUID, Favor, TotalFavorEarned, AccumulatedFractionalFavor
    - Bump DataVersion to 3
- [x] Create `PlayerDataMigration.cs` with v2 to v3 migration logic

### Manager Refactoring

- [x] Rename `PlayerReligionDataManager.cs` to `PlayerProgressionDataManager.cs`
- [x] Rename `IPlayerReligionDataManager.cs` to `IPlayerProgressionDataManager.cs`
- [x] Update `JoinReligion()` to remove ReligionUID setting
- [x] Update `LeaveReligion()` to query ReligionManager
- [x] Add `GetPlayerDeity()` and `HasReligion()` helper methods
- [x] Update `LoadPlayerData()` to handle v2 to v3 migration

### Codebase Updates

- [x] Update all network handlers to use new types
- [x] Update all UI code to use new types
- [x] Update all 46 test files to use new types

### Testing

- [x] Write tests for v2 to v3 migration
- [x] Write tests for HashSet blessing storage and computed FavorRank

## Phase 3: Load-Time Validation & Deletion Constraints (SAFETY)

- [ ] Add `ValidateAllMemberships()` to ReligionManager
- [ ] Update `DeleteReligion()` to remove all members first
- [ ] Add validation call in `DivineAscensionSystemInitializer`
- [ ] Add load-time validation logging with summary
- [ ] Write tests for load-time validation and DeleteReligion constraints

## Documentation Updates

- [ ] Remove 7-day cooldown references from `PLAYER_GUIDE.md`
- [ ] Update `CLAUDE.md` architecture section with new data model

## Final Verification

- [ ] Run full test suite and verify all tests pass
- [ ] Run build and verify no compilation errors

---

**Total Tasks:** 34
**Estimated Effort:** 20-27 hours (see main plan document)

## Progress Tracking

Update this file as tasks are completed by changing `[ ]` to `[x]`.
