# GitHub Issue Breakdown: Configuration & Cooldown System

## Overview

Breaking down the configuration and cooldown system implementation into 8 deliverable GitHub issues. Issues are ordered by dependency and can be worked on sequentially or in parallel where dependencies allow.

**Total Estimated Effort:** ~1,100 new lines, ~580 modified lines across 4 new files and 12 modified files.

---

## Issue #1: Add ConfigLib Integration and GameBalanceConfig Class

**Priority:** High
**Depends on:** None
**Blocks:** Issues #2, #3
**Estimated Size:** Small-Medium (~200 lines)

### Description
Integrate ConfigLib library and create the `GameBalanceConfig` POCO class to store Tier 1 server-tunable balance settings. This provides the foundation for all configuration management.

### Scope
1. Add ConfigLib as optional dependency to `modinfo.json`
2. Add ConfigLib reference to project (DLL or NuGet)
3. Create `GameBalanceConfig.cs` - Simple POCO class with public properties
4. Modify `DivineAscensionModSystem.Start()` to register config with ConfigLib
5. Handle graceful degradation when ConfigLib is not installed

### Acceptance Criteria
- [ ] ConfigLib added to `modinfo.json` as optional dependency
- [ ] `GameBalanceConfig.cs` created with all Tier 1 settings:
  - Favor system properties (rates, penalties, multipliers)
  - Progression thresholds (favor ranks, prestige ranks)
  - PvP properties (rewards, penalties, war multipliers)
- [ ] All properties have default values matching current hardcoded constants
- [ ] Property validation in constructor (ascending thresholds)
- [ ] ConfigLib registration in `DivineAscensionModSystem.Start()` with:
  - Check for ConfigLib presence: `api.ModLoader.IsModEnabled("configlib")`
  - Call `RegisterCustomManagedConfig()` with callbacks
  - Graceful fallback to hardcoded defaults if ConfigLib absent
- [ ] Server logs notification when ConfigLib is/isn't installed
- [ ] Config object accessible to other systems

### Testing
- **Manual Test 1**: Start server WITH ConfigLib, verify YAML file auto-generated at `ModConfig/divineascension.yaml`
- **Manual Test 2**: Start server WITHOUT ConfigLib, verify hardcoded defaults used, logs notification
- **Manual Test 3**: Open ConfigLib GUI (ESC → Mod Settings → Divine Ascension), verify all settings visible

### Files Changed
- `DivineAscension/modinfo.json` (+3 lines)
- `DivineAscension/Configuration/GameBalanceConfig.cs` (new, ~150 lines)
- `DivineAscension/DivineAscensionModSystem.cs` (+20 lines)

### Labels
`enhancement`, `configuration`, `high-priority`, `dependencies`

### Notes
- ConfigLib repo: https://github.com/maltiez2/vsmod_configlib
- ConfigLib mod page: https://mods.vintagestory.at/configlib
- License: CC0-1.0 (public domain)

---

## Issue #2: Integrate Config into Favor System

**Priority:** High
**Depends on:** Issue #1
**Blocks:** None (can parallel with #3)
**Estimated Size:** Small (~120 lines modified)

### Description
Replace hardcoded favor system constants with configurable values from `GameBalanceConfig`. This allows server admins to tune passive favor generation, death penalties, and rank/prestige multipliers.

### Scope
1. Modify `DivineAscensionSystemInitializer.cs` to pass `GameBalanceConfig` to `FavorSystem`
2. Modify `FavorSystem.cs` constructor to accept config reference
3. Replace hardcoded constants with config property access:
   - `BASE_FAVOR_PER_HOUR` (0.5f) → `config.PassiveFavorRate`
   - Rank multipliers → `config.InitiateMultiplier`, etc.
   - Prestige multipliers → `config.FledglingMultiplier`, etc.
   - `DEATH_PENALTY_FAVOR` (50) → `config.DeathPenalty`

### Acceptance Criteria
- [ ] `FavorSystem` constructor accepts `GameBalanceConfig` parameter
- [ ] All hardcoded constants replaced with config property access
- [ ] Passive favor generation uses `config.PassiveFavorRate`
- [ ] Rank multipliers use config values (5 ranks: Initiate → Avatar)
- [ ] Prestige multipliers use config values (5 ranks: Fledgling → Mythic)
- [ ] Death penalty uses `config.DeathPenalty`
- [ ] No compilation errors or warnings
- [ ] Existing tests pass (if any)

### Testing
- **Manual Test 1**: Change `passive-favor-rate` in YAML to 2.0, verify favor generation doubles
- **Manual Test 2**: Change `disciple-multiplier` to 2.0, verify Disciple rank players earn 2x passive favor
- **Manual Test 3**: Change `death-penalty` to 100, verify player loses 100 favor on death
- **Manual Test 4**: Verify favor awards still work correctly with default config values

### Files Changed
- `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (+5 lines)
- `DivineAscension/Systems/FavorSystem.cs` (~120 lines modified)

### Labels
`enhancement`, `configuration`, `favor-system`, `high-priority`

---

## Issue #3: Integrate Config into Progression and PvP Systems

**Priority:** High
**Depends on:** Issue #1
**Blocks:** None (can parallel with #2)
**Estimated Size:** Small (~180 lines modified)

### Description
Replace hardcoded progression thresholds and PvP rewards with configurable values from `GameBalanceConfig`. This allows server admins to tune rank requirements and PvP balance.

### Scope
1. Modify `PlayerProgressionData.cs` to use favor rank thresholds from config
2. Modify `ReligionPrestigeManager.cs` to use prestige rank thresholds from config
3. Modify `PvPManager.cs` to use kill rewards, death penalty, and war multipliers from config
4. Update `DivineAscensionSystemInitializer.cs` to pass config to these managers

### Acceptance Criteria
- [ ] `PlayerProgressionData.CalculateRank()` uses config thresholds:
  - `config.DiscipleThreshold` (default 500)
  - `config.ZealotThreshold` (default 2000)
  - `config.ChampionThreshold` (default 5000)
  - `config.AvatarThreshold` (default 10000)
- [ ] `ReligionPrestigeManager.CalculatePrestigeRank()` uses config thresholds:
  - `config.EstablishedThreshold` (default 2500)
  - `config.RenownedThreshold` (default 10000)
  - `config.LegendaryThreshold` (default 25000)
  - `config.MythicThreshold` (default 50000)
- [ ] `PvPManager` uses config values:
  - `config.KillFavorReward` (default 10)
  - `config.KillPrestigeReward` (default 75)
  - `config.DeathPenalty` (default 50)
  - `config.WarFavorMultiplier` (default 1.5)
  - `config.WarPrestigeMultiplier` (default 1.5)
- [ ] All managers receive config reference via constructor
- [ ] No compilation errors or warnings
- [ ] Existing tests pass (if any)

### Testing
- **Manual Test 1**: Change `disciple-threshold` to 1000, kill to earn 1000 favor, verify rank up to Disciple
- **Manual Test 2**: Change `kill-favor-reward` to 50, kill player, verify 50 favor awarded
- **Manual Test 3**: Change `war-favor-multiplier` to 3.0, declare war, kill enemy, verify 3x favor (150 with default 50 reward)
- **Manual Test 4**: Verify progression and PvP work correctly with default config values

### Files Changed
- `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (+10 lines)
- `DivineAscension/Systems/PlayerProgressionData.cs` (~40 lines modified)
- `DivineAscension/Systems/ReligionPrestigeManager.cs` (~60 lines modified)
- `DivineAscension/Systems/PvPManager.cs` (~80 lines modified)

### Labels
`enhancement`, `configuration`, `progression`, `pvp`, `high-priority`

---

## Issue #4: Implement Cooldown System Core

**Priority:** High
**Depends on:** None (independent of config issues)
**Blocks:** Issues #5, #6, #7
**Estimated Size:** Medium (~450 lines)

### Description
Implement the core cooldown and rate limiting system to prevent griefing attacks (spam kicks, bans, invites, religion deletion). This system tracks per-player cooldowns for various operations and enforces waiting periods.

### Scope
1. Create `CooldownManager.cs` - Core tracking and validation logic
2. Create `CooldownType.cs` - Enum for 7 operation types
3. Create `CooldownTimeFormatter.cs` - User-friendly time display utility
4. Modify `ModConfigData.cs` - Add cooldown configuration (durations + enabled toggle)
5. Initialize `CooldownManager` in `DivineAscensionSystemInitializer.cs`

### Acceptance Criteria
- [ ] `CooldownType` enum created with 7 types:
  - `ReligionDeletion` (60s default)
  - `KickMember` (5s default)
  - `BanPlayer` (10s default)
  - `SendInvite` (2s default)
  - `ReligionCreation` (300s default)
  - `DiplomacyProposal` (30s default)
  - `WarDeclaration` (60s default)
- [ ] `CooldownManager` implements:
  - `CanPerformOperation(playerUID, operationType, out remainingCooldown)` - Check if allowed
  - `RecordOperation(playerUID, operationType)` - Record timestamp
  - `HasAdminPrivilege(playerUID)` - Admin bypass check (Privilege.root)
  - `GetCooldownErrorMessage(operationType, remainingTime)` - Formatted error
  - Auto-cleanup every 5 minutes (remove expired cooldowns)
  - Thread-safe (lock for concurrent requests)
- [ ] `CooldownTimeFormatter.FormatRemainingTime(TimeSpan)` formats as:
  - "a moment" (< 1s)
  - "5 seconds" (< 60s)
  - "2 minutes and 30 seconds" (< 60min)
  - "1 hours and 15 minutes" (≥ 60min)
- [ ] `ModConfigData` stores cooldown config:
  - Dictionary of durations per operation type
  - Global enabled/disabled toggle
  - Default values match security requirements
- [ ] `CooldownManager` initialized after `LocalizationService` in initializer
- [ ] Memory-only implementation (no persistence across restarts)

### Testing
- **Unit Tests** (9 test cases in `CooldownManagerTests.cs`):
  1. `CanPerformOperation_NoCooldownRecorded_ReturnsTrue()`
  2. `CanPerformOperation_CooldownActive_ReturnsFalse()`
  3. `CanPerformOperation_CooldownExpired_ReturnsTrue()`
  4. `CanPerformOperation_AdminPlayer_BypassesCooldown()`
  5. `CanPerformOperation_CooldownsDisabled_ReturnsTrue()`
  6. `RecordOperation_UpdatesTimestamp()`
  7. `GetCooldownErrorMessage_FormatsCorrectly()`
  8. `CleanupExpiredEntries_RemovesExpiredCooldowns()`
  9. `GetRemainingCooldown_CalculatesCorrectly()`
- **Manual Test**: Trigger operation twice rapidly, verify cooldown enforced with formatted time message

### Files Changed
- `DivineAscension/Systems/CooldownManager.cs` (new, ~350 lines)
- `DivineAscension/Models/Enum/CooldownType.cs` (new, ~15 lines)
- `DivineAscension/Utilities/CooldownTimeFormatter.cs` (new, ~30 lines)
- `DivineAscension/Data/ModConfigData.cs` (+30 lines)
- `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (+15 lines)
- `DivineAscension.Tests/Systems/CooldownManagerTests.cs` (new, ~450 lines)

### Labels
`enhancement`, `cooldown`, `security`, `high-priority`, `anti-griefing`

---

## Issue #5: Integrate Cooldowns into Religion and Civilization Commands

**Priority:** High
**Depends on:** Issue #4
**Blocks:** None (can parallel with #6)
**Estimated Size:** Medium (~130 lines modified)

### Description
Protect religion and civilization commands with cooldowns to prevent spam and griefing attacks. Integrates cooldown checks into command handlers.

### Scope
1. Modify `ReligionCommands.cs` - Inject `CooldownManager`, protect 5 operations
2. Modify `CivilizationCommands.cs` - Inject `CooldownManager`, protect 1 operation

### Acceptance Criteria
- [ ] `ReligionCommands` constructor accepts `CooldownManager` parameter
- [ ] `OnDisbandReligion` protected with `CooldownType.ReligionDeletion` (60s)
  - Check cooldown BEFORE validation
  - Record cooldown AFTER successful execution
  - Return clear error message with remaining time
- [ ] `OnKickPlayer` protected with `CooldownType.KickMember` (5s)
- [ ] `OnBanPlayer` protected with `CooldownType.BanPlayer` (10s)
- [ ] `OnInvitePlayer` protected with `CooldownType.SendInvite` (2s)
- [ ] `OnCreateReligion` protected with `CooldownType.ReligionCreation` (300s)
- [ ] `CivilizationCommands.OnInviteReligion` protected with `CooldownType.SendInvite` (2s)
- [ ] Admin players (Privilege.root) bypass all cooldowns automatically
- [ ] Error messages use localized strings
- [ ] Pattern: Check → Execute → Record (only on success)

### Testing
- **Integration Tests** (4 test cases in `ReligionCommandsWithCooldownTests.cs`):
  1. `OnDisbandReligion_WithActiveCooldown_ReturnsError()`
  2. `OnKickPlayer_RapidSuccession_EnforcesCooldown()`
  3. `OnBanPlayer_AdminUser_BypassesCooldown()`
  4. `OnInvitePlayer_SpamPrevention_EnforcesCooldown()`
- **Manual Test 1**: Kick 2 members rapidly, verify 2nd kick has 5s cooldown
- **Manual Test 2**: Ban player, try to ban another immediately, verify 10s cooldown
- **Manual Test 3**: As admin, perform rapid operations, verify no cooldowns
- **Manual Test 4**: Disband religion, try to create new one, verify 300s cooldown

### Files Changed
- `DivineAscension/Commands/ReligionCommands.cs` (+100 lines)
- `DivineAscension/Commands/CivilizationCommands.cs` (+30 lines)
- `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (+10 lines)
- `DivineAscension.Tests/Commands/ReligionCommandsWithCooldownTests.cs` (new, ~350 lines)

### Labels
`enhancement`, `cooldown`, `commands`, `security`, `high-priority`

---

## Issue #6: Integrate Cooldowns into Diplomacy Manager

**Priority:** High
**Depends on:** Issue #4
**Blocks:** None (can parallel with #5)
**Estimated Size:** Small (~50 lines modified)

### Description
Protect diplomacy operations with cooldowns to prevent spam proposals and war declaration flip-flopping.

### Scope
1. Modify `DiplomacyManager.cs` - Inject `CooldownManager`, protect 2 operations
2. Update initialization to pass `CooldownManager` to `DiplomacyManager`

### Acceptance Criteria
- [ ] `DiplomacyManager` constructor accepts `CooldownManager` parameter
- [ ] `ProposeRelationship` protected with `CooldownType.DiplomacyProposal` (30s)
  - Check cooldown before creating proposal
  - Record cooldown after successful proposal creation
  - Return error tuple with cooldown message if blocked
- [ ] `DeclareWar` protected with `CooldownType.WarDeclaration` (60s)
  - Check cooldown before war declaration
  - Record cooldown after successful declaration
  - Return error tuple with cooldown message if blocked
- [ ] Admin bypass works automatically (checked in CooldownManager)
- [ ] Error messages clear and user-friendly

### Testing
- **Manual Test 1**: Create diplomatic proposal, try to create another immediately, verify 30s cooldown
- **Manual Test 2**: Declare war, try to declare another war, verify 60s cooldown
- **Manual Test 3**: As admin, perform rapid diplomatic actions, verify no cooldowns
- **Manual Test 4**: Wait for cooldown to expire, verify operation allowed

### Files Changed
- `DivineAscension/Systems/DiplomacyManager.cs` (+50 lines)
- `DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (+5 lines)

### Labels
`enhancement`, `cooldown`, `diplomacy`, `security`, `high-priority`

---

## Issue #7: Integrate Cooldowns into Network Handlers

**Priority:** Medium
**Depends on:** Issue #4, #5, #6
**Blocks:** None
**Estimated Size:** Small (~60 lines modified)

### Description
Add cooldown checks to network handlers to prevent client-side spam through UI operations (ImGui actions bypass command layer).

### Scope
1. Modify `ReligionNetworkHandler.cs` - Add cooldown checks for action requests
2. Modify `CivilizationNetworkHandler.cs` - Add cooldown checks for action requests
3. Return error packets when cooldown active

### Acceptance Criteria
- [ ] `ReligionNetworkHandler` constructor accepts `CooldownManager` parameter
- [ ] Action request handler checks cooldowns for:
  - Kick actions → `CooldownType.KickMember`
  - Ban actions → `CooldownType.BanPlayer`
  - Invite actions → `CooldownType.SendInvite`
  - Disband actions → `CooldownType.ReligionDeletion`
- [ ] `CivilizationNetworkHandler` checks cooldowns for:
  - Invite actions → `CooldownType.SendInvite`
  - War declaration → `CooldownType.WarDeclaration`
- [ ] Cooldown violations return error packet with message
- [ ] Successful operations record cooldown timestamp
- [ ] Admin bypass works for network operations
- [ ] Pattern: Check → Execute → Record → Send response

### Testing
- **Manual Test 1**: Use ImGui to kick member, try to kick another immediately, verify cooldown enforced via UI
- **Manual Test 2**: Use ImGui to send invite, try to send another, verify 2s cooldown in UI
- **Manual Test 3**: Use ImGui to declare war, try to declare another, verify 60s cooldown in UI
- **Manual Test 4**: Verify error messages display correctly in UI/chat

### Files Changed
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs` (+60 lines)
- `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs` (+40 lines) [estimated]

### Labels
`enhancement`, `cooldown`, `networking`, `security`, `medium-priority`

---

## Issue #8: Add Cooldown Admin Commands and Localization

**Priority:** Medium
**Depends on:** Issue #4
**Blocks:** None
**Estimated Size:** Small-Medium (~120 lines)

### Description
Add admin commands for managing cooldowns and localization strings for all cooldown error messages.

### Scope
1. Extend `ConfigCommands.cs` with `/da config cooldown` subcommands
2. Add localization keys to `LocalizationKeys.cs` for cooldown errors
3. Add translations to `assets/divineascension/lang/en.json`

### Acceptance Criteria
- [ ] `/da config cooldown status` command implemented:
  - Shows all 7 operation types with current durations
  - Shows whether cooldowns are enabled/disabled globally
  - Admin-only (Privilege.root)
- [ ] `/da config cooldown set <operation> <seconds>` command implemented:
  - Updates cooldown duration for specific operation
  - Validates operation name and duration (0-3600 seconds)
  - Persists to `ModConfigData`
  - Admin-only
- [ ] `/da config cooldown enable` command implemented:
  - Enables cooldown system globally
  - Logs action
  - Admin-only
- [ ] `/da config cooldown disable` command implemented:
  - Disables cooldown system globally (for events/testing)
  - Logs action
  - Admin-only
- [ ] Localization keys added to `LocalizationKeys.cs`:
  - `COOLDOWN_ERROR_RELIGION_DELETION`
  - `COOLDOWN_ERROR_KICK_MEMBER`
  - `COOLDOWN_ERROR_BAN_PLAYER`
  - `COOLDOWN_ERROR_SEND_INVITE`
  - `COOLDOWN_ERROR_RELIGION_CREATION`
  - `COOLDOWN_ERROR_DIPLOMACY_PROPOSAL`
  - `COOLDOWN_ERROR_WAR_DECLARATION`
- [ ] English translations added to `en.json` with format:
  - "You must wait {0} before [action] again."
- [ ] All commands documented in help text

### Testing
- **Manual Test 1**: Run `/da config cooldown status`, verify output shows all operations with durations
- **Manual Test 2**: Run `/da config cooldown set kickMember 10`, verify duration changed, kick twice to test
- **Manual Test 3**: Run `/da config cooldown disable`, perform rapid operations, verify no cooldowns
- **Manual Test 4**: Run `/da config cooldown enable`, verify cooldowns re-enabled
- **Manual Test 5**: Trigger cooldown, verify localized error message displays correctly

### Files Changed
- `DivineAscension/Commands/ConfigCommands.cs` (+100 lines)
- `DivineAscension/Constants/LocalizationKeys.cs` (+10 lines)
- `assets/divineascension/lang/en.json` (+10 lines)

### Labels
`enhancement`, `cooldown`, `admin-tools`, `localization`, `medium-priority`

---

## Dependency Graph

```
Issue #1 (ConfigLib Core)
├─> Issue #2 (Favor Config) ─┐
└─> Issue #3 (Progression/PvP Config) ─┤
                                        ├─> [Can be tested independently]
Issue #4 (Cooldown Core)                │
├─> Issue #5 (Command Cooldowns) ──┐   │
├─> Issue #6 (Diplomacy Cooldowns) ├───┤
├─> Issue #7 (Network Cooldowns) ───┘   │
└─> Issue #8 (Admin Commands) ──────────┘
```

## Recommended Implementation Order

### Phase 1: Configuration Foundation (Parallel)
1. **Issue #1** - ConfigLib integration (required for #2, #3)
2. **Issue #2** - Favor config (can parallel with #3)
3. **Issue #3** - Progression/PvP config (can parallel with #2)

### Phase 2: Cooldown Foundation
4. **Issue #4** - Cooldown core (required for #5, #6, #7, #8)

### Phase 3: Cooldown Integration (Parallel after #4)
5. **Issue #5** - Command cooldowns (can parallel with #6, #7)
6. **Issue #6** - Diplomacy cooldowns (can parallel with #5, #7)
7. **Issue #7** - Network cooldowns (can parallel with #5, #6)

### Phase 4: Polish
8. **Issue #8** - Admin commands and localization

## Testing Strategy

### Per-Issue Testing
Each issue includes unit tests and/or manual test cases in its acceptance criteria.

### Integration Testing (After All Issues Complete)
1. **Config Integration Test**: Change all config values via GUI, verify all systems use new values
2. **Cooldown Integration Test**: Test all 7 cooldown types via commands and UI
3. **Config + Cooldown Test**: Modify cooldown durations via admin commands, verify enforcement
4. **Performance Test**: Simulate 50+ players, verify no lag from config/cooldown checks
5. **Backward Compatibility Test**: Load existing world, verify default behavior unchanged

## Rollout Plan

### Recommended Release Strategy
- **v1.0 (MVP)**: Issues #1, #2, #3, #4, #5, #6
  - Core config and cooldown functionality
  - Essential protection mechanisms
- **v1.1 (Polish)**: Issues #7, #8
  - Network handler protection
  - Admin tooling

OR release all together as single feature update.

## Estimated Timeline

Assuming single developer working full-time:
- **Issue #1**: 0.5 days
- **Issue #2**: 0.5 days
- **Issue #3**: 0.75 days
- **Issue #4**: 1.5 days (includes tests)
- **Issue #5**: 1 day (includes tests)
- **Issue #6**: 0.5 days
- **Issue #7**: 0.5 days
- **Issue #8**: 0.5 days

**Total: ~5.75 days** (assuming no blockers, includes testing)

With part-time work or multiple developers, adjust accordingly. Issues #2/#3 and #5/#6/#7 can be parallelized.
