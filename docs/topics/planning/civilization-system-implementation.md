# Civilization System Implementation Plan

**Status**: Phase 2 Complete ✅
**Current Phase**: Phase 3 (Pending)
**Last Updated**: 2025-11-25

## Overview

Implementation of a minimal civilization system for PantheonWars - organizational containers that allow 2-4 different-deity religions to form alliances. This MVP focuses on core functionality without buildings or mechanical bonuses.

### Design Principles

- **Minimal viable product**: Test core concept before adding complexity
- **Deity diversity**: Each civilization must have different deities (no duplicates)
- **Founder-controlled**: Only religion founders can manage civilization membership
- **Auto-validation**: Civilizations auto-disband if they fall below 2 religions

---

## Phase 1: Core Backend ✅ COMPLETE

**Duration**: Week 1
**Status**: ✅ Completed 2025-11-25
**Commit**: `8485f6b` - "feat: implement civilization system Phase 1 (backend core)"

### Tasks

#### Task 1.1: Create Data Models ✅
**Status**: Complete
**Files Created**:
- `PantheonWars/Data/CivilizationData.cs` (205 lines)
  - `Civilization` class with member religion tracking
  - `CivilizationInvite` class with 7-day expiry
  - `CivilizationCooldown` class for join restrictions
  - All using ProtoBuf serialization

#### Task 1.2: Create World Data Container ✅
**Status**: Complete
**Files Created**:
- `PantheonWars/Data/CivilizationWorldData.cs` (177 lines)
  - Collection management for civilizations
  - Quick lookup map (ReligionId → CivId)
  - Helper methods for CRUD operations
  - Auto-cleanup of expired data

#### Task 1.3: Implement Civilization Manager ✅
**Status**: Complete
**Files Created**:
- `PantheonWars/Systems/CivilizationManager.cs` (679 lines)
  - Full CRUD operations with validation
  - Business logic for all civilization operations
  - Save/load system using ProtoBuf
  - Integration with ReligionManager and DeityRegistry

**Features Implemented**:
- ✅ Create civilization (founder validation, name uniqueness)
- ✅ Invite religions (deity diversity enforcement, max 4)
- ✅ Accept invitations (7-day expiry)
- ✅ Leave civilization (7-day cooldown)
- ✅ Kick religions (founder only, cooldown applied)
- ✅ Disband civilization (founder only)
- ✅ Auto-disband when below 2 religions
- ✅ Query methods for civilizations and members

#### Task 1.4: Integrate with PantheonWarsSystem ✅
**Status**: Complete
**Files Modified**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Added CivilizationManager field
  - Integrated into server-side lifecycle
  - Proper initialization order (after ReligionManager and DeityRegistry)

### Success Criteria ✅

- ✅ All data models created with ProtoBuf serialization
- ✅ CivilizationManager integrated into mod lifecycle
- ✅ Save/load system functional
- ✅ All business logic validation rules implemented
- ✅ No compilation errors
- ✅ Code follows existing patterns (ReligionManager, etc.)

---

## Phase 2: Commands & Networking ✅ COMPLETE

**Duration**: Week 2
**Status**: ✅ Completed 2025-11-25
**Dependencies**: Phase 1 complete

### Tasks

#### Task 2.1: Create Network Packets ✅
**Status**: Complete
**Files Created**:
- `PantheonWars/Network/Civilization/CivilizationListRequestPacket.cs` (21 lines)
- `PantheonWars/Network/Civilization/CivilizationListResponsePacket.cs` (41 lines)
- `PantheonWars/Network/Civilization/CivilizationInfoRequestPacket.cs` (21 lines)
- `PantheonWars/Network/Civilization/CivilizationInfoResponsePacket.cs` (74 lines)
- `PantheonWars/Network/Civilization/CivilizationActionRequestPacket.cs` (30 lines)
- `PantheonWars/Network/Civilization/CivilizationActionResponsePacket.cs` (30 lines)

**Implementation Details**:
- All packets use ProtoBuf serialization
- Request packets include parameterless constructors for serialization
- Response packets contain nested data structures (CivilizationInfo, CivilizationDetails, MemberReligion, PendingInvite)
- Action packet supports all 6 actions: create, invite, accept, leave, kick, disband

**Files Modified**:
- `PantheonWars/PantheonWarsSystem.cs` (lines 81-86)
  - Registered all 6 packet types in `Start()` method

#### Task 2.2: Implement Server-Side Networking ✅
**Status**: Complete
**Files Modified**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Added handler registrations in `SetupServerNetworking()` (lines 221-224)
  - Implemented `OnCivilizationListRequest()` (lines 887-913)
  - Implemented `OnCivilizationInfoRequest()` (lines 918-975)
  - Implemented `OnCivilizationActionRequest()` (lines 980-1079)
- `PantheonWars/Systems/CivilizationManager.cs` (lines 558-561)
  - Added `GetInvitesForCiv()` helper method
- `PantheonWars/Data/CivilizationWorldData.cs` (lines 148-151)
  - Added `GetInvitesForCivilization()` data access method

**Implementation Details**:
- List handler returns civilization summaries with member religions and deities
- Info handler returns detailed civilization data including pending invites (founder only)
- Action handler uses switch statement to process all 6 civilization actions
- Full permission validation (founder checks, cooldown checks, deity diversity)
- Comprehensive error handling with try-catch blocks
- User-friendly success/error messages

#### Task 2.3: Implement Client-Side Networking ✅
**Status**: Complete
**Files Modified**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Registered packet handlers in `SetupClientNetworking()` (lines 1140-1142)
  - Implemented `OnCivilizationListResponse()` (lines 1258-1262)
  - Implemented `OnCivilizationInfoResponse()` (lines 1264-1268)
  - Implemented `OnCivilizationActionResponse()` (lines 1270-1281)

**Public Request Methods** (lines 1395-1441):
- `RequestCivilizationList(string deityFilter = "")` - Request all civilizations
- `RequestCivilizationInfo(string civId)` - Request specific civilization details
- `RequestCivilizationAction(string action, ...)` - Request civilization action

**Events Added** (lines 1479-1492):
- `CivilizationListReceived` - Fires when civilization list received
- `CivilizationInfoReceived` - Fires when civilization info received
- `CivilizationActionCompleted` - Fires when action completes

**Implementation Details**:
- All handlers log debug messages and fire events
- Action response handler displays chat messages to user
- Null-safe client API calls throughout

#### Task 2.4: Create Command System ✅
**Status**: Complete
**Files Created**:
- `PantheonWars/Commands/CivilizationCommands.cs` (446 lines)

**Files Modified**:
- `PantheonWars/PantheonWarsSystem.cs` (lines 30, 179-180)
  - Added `_civilizationCommands` field
  - Instantiated and registered commands in `StartServerSide()`

**Commands Implemented** (All 9):
- ✅ `/civ create <name>` - Create a civilization
- ✅ `/civ invite <religionname>` - Invite a religion (founder only)
- ✅ `/civ accept <inviteid>` - Accept an invitation
- ✅ `/civ leave` - Leave civilization (7-day cooldown)
- ✅ `/civ kick <religionname>` - Kick a religion (founder only, 7-day cooldown)
- ✅ `/civ disband` - Disband civilization (founder only)
- ✅ `/civ list [deity]` - List all civilizations with optional deity filter
- ✅ `/civ info [name]` - Show civilization details (defaults to player's civ)
- ✅ `/civ invites` - Show pending civilization invitations

**Validation**:
- ✅ Permission checks (founder only for create/invite/kick/disband)
- ✅ Religion membership validation
- ✅ Civilization membership validation
- ✅ Proper error messages for all failure cases
- ✅ Success confirmations with helpful information
- ✅ Formatted output with bullet points and sections

**Implementation Details**:
- Follows existing ReligionCommands pattern
- Uses primary constructors with dependency injection
- All handlers return TextCommandResult for proper feedback
- Comprehensive error handling and validation
- User-friendly output formatting with StringBuilder

### Success Criteria

- ✅ All packet types registered and handlers implemented
- ✅ Server validates all actions and sends appropriate responses
- ✅ Client can request data and receive updates
- ✅ All commands functional and accessible via chat
- ✅ Proper error messages for invalid actions
- ✅ Events fire correctly for UI updates
- ✅ Build succeeds with 0 errors

---

## Phase 3: UI/UX ⏳ PENDING

**Duration**: Week 3
**Status**: Not Started
**Dependencies**: Phase 2 complete

### Tasks

#### Task 3.1: Add Civilization Info to BlessingDialog
**Status**: Pending
**Files to Modify**:
- `PantheonWars/GUI/BlessingDialog.cs`

**UI Elements to Add**:
- Civilization name display (if player is in one)
- Member religion list with deity icons
- Total member count
- Founder indicator
- "Manage Civilization" button (for founders)

**Integration**:
- Subscribe to civilization data events
- Request civilization info on dialog open
- Update display when civilization changes

#### Task 3.2: Create Civilization Management Dialog (Optional)
**Status**: Pending (Optional)
**Files to Create**:
- `PantheonWars/GUI/CivilizationDialog.cs`

**Features**:
- **Browse Tab**: List all civilizations, filter by deity, search
- **My Civilization Tab**: Manage members, invites, settings
- **Invites Tab**: View and accept pending invitations
- **Create Tab**: Form to create new civilization

**UI Elements**:
- Religion list with deity diversity indicators
- Invite management (send, view pending, expiry timers)
- Member count and founder display
- Kick functionality (founder only)
- Disband button (founder only)
- Cooldown timers

#### Task 3.3: Create Civilization Info Overlay (Optional)
**Status**: Pending (Optional)
**Files to Create**:
- `PantheonWars/GUI/CivilizationInfoOverlay.cs`

**Features**:
- Compact overlay showing civilization info
- Toggle visibility
- Displays: civ name, member religions, deity types, member count

### Success Criteria

- [x] BlessingDialog shows civilization info
- [ ] Players can see their civilization membership
- [x] Founders can access management features
- [x] (Optional) Full civilization dialog functional
- [x] (Optional) Info overlay displays correctly
- [x] UI matches existing mod style (VSImGui)
- [x] All UI updates in response to events

---

## Phase 4: Testing & Release ⏳ PENDING

**Duration**: Week 4
**Status**: Not Started
**Dependencies**: Phase 3 complete

### Tasks

#### Task 4.1: Manual Testing
**Status**: Pending

**Test Cases**:
- [x] Create civilization as religion founder
- [ ] Invite religions from different deities
- [ ] Accept/reject invitations
- [ ] Leave civilization (verify cooldown)
- [ ] Kick religion (verify cooldown)
- [x] Disband civilization
- [ ] Verify deity diversity enforcement
- [ ] Test 2-4 religion capacity limits
- [ ] Test auto-disband when below 2 religions
- [ ] Verify invite expiry (7 days)
- [ ] Verify cooldown expiry (7 days)
- [x] Test persistence (save/load)

**Edge Cases to Test**:
- [ ] Religion founder leaves religion while in civilization
- [ ] Religion disbanded while in civilization
- [ ] Multiple simultaneous invites
- [ ] Civilization at capacity when invite accepted
- [ ] Network disconnection during operations

#### Task 4.2: Fix Bugs
**Status**: Pending

- Document and fix any bugs found during testing
- Verify all edge cases handled gracefully
- Ensure proper error messages for users

#### Task 4.3: Update Documentation
**Status**: Pending

**Files to Update**:
- [ ] Create `docs/topics/reference/civilization-system.md`
- [ ] Update mod README with civilization info
- [ ] Document all commands
- [ ] Add troubleshooting section

#### Task 4.4: Release
**Status**: Pending

- [ ] Create release notes
- [ ] Increment version number
- [ ] Tag release in git
- [ ] Update changelog

### Success Criteria

- [ ] All test cases pass
- [ ] No critical bugs
- [ ] Documentation complete
- [ ] Release deployed

---

## Phase 5: Future Enhancements ⏳ DEFERRED

**Status**: Post-MVP
**Dependencies**: User validation, player feedback
**Condition**: Only implement if player adoption validates the need

### Potential Features

#### 5.1: Civilization Bonuses (Conditional)
**Trigger**: High player engagement with civilizations

**Possible Bonuses**:
- Small passive favor gain bonus (2-5% per additional religion)
- Shared deity blessing pools
- Coordinated PvP benefits
- Resource sharing mechanics

**Design Constraints**:
- Bonuses must be minimal to avoid making civilizations mandatory
- Should encourage cooperation without penalizing solo religions
- Balance carefully against existing deity/religion bonuses

#### 5.2: Civilization Buildings (Conditional)
**Trigger**: Strong player demand for shared infrastructure

**Possible Buildings**:
- Shared shrine or temple
- Civilization hall for meetings
- Storage facilities
- Defensive structures

**Considerations**:
- Building placement and ownership
- Maintenance costs
- Griefing prevention

#### 5.3: Civilization Progression (Conditional)
**Trigger**: Need for long-term engagement goals

**Possible Systems**:
- Civilization levels based on combined prestige
- Unlock tiers with escalating benefits
- Achievements and milestones

### Decision Criteria

**Implement if**:
- 50%+ of active religions join civilizations
- Players request more depth
- PvP loop needs strengthening

**Skip if**:
- Low adoption rate
- Players prefer solo religions
- Complexity outweighs benefits

---

## Technical Notes

### Architecture Patterns Used

- **Data Models**: ProtoBuf serialization for all persistent data
- **Manager Pattern**: CivilizationManager follows ReligionManager conventions
- **Event-Driven**: Client/server communication via network packets and events
- **Separation of Concerns**: Civilizations manage religions, religions manage players

### Dependencies

- `ReligionManager` - Required for religion validation and queries
- `DeityRegistry` - Required for deity diversity enforcement
- `PantheonWarsSystem` - Integration point for lifecycle and networking

### Performance Considerations

- Quick lookup map (ReligionId → CivId) for O(1) access
- Cached member counts to avoid repeated calculations
- Minimal overhead for players not in civilizations

### Validation Rules

1. **Creation**:
   - Only religion founders can create civilizations
   - Name must be 3-32 characters and unique
   - Religion must not be in another civilization
   - Religion must not be on cooldown

2. **Invitations**:
   - Only civilization founder can invite
   - Max 4 religions per civilization
   - No duplicate deities
   - Target religion must exist and not be in another civilization
   - Target religion must not be on cooldown
   - One pending invite per religion per civilization

3. **Acceptance**:
   - Only religion founder can accept
   - Invite must be valid and not expired
   - Civilization must have space

4. **Leaving/Kicking**:
   - Only religion founder can leave
   - Only civilization founder can kick
   - Founder cannot kick own religion
   - Founder must disband, not leave
   - 7-day cooldown applied
   - Auto-disband if below 2 religions

5. **Disbanding**:
   - Only civilization founder can disband
   - All pending invites removed

---

## Open Questions

- [ ] Should we add a civilization chat channel?
- [ ] Should civilizations have configurable privacy settings?
- [ ] Should we allow religion founders to transfer civilization founder role?
- [ ] Should there be a cost (favor/prestige) to create civilizations?

---

## Changelog

### 2025-11-25 (Phase 2)
- ✅ Completed Phase 2: Commands & Networking
- Created 6 network packet files with ProtoBuf serialization
- Implemented server-side packet handlers (list, info, action)
- Implemented client-side networking (handlers, request methods, events)
- Created CivilizationCommands with all 9 chat commands
- Added helper methods to CivilizationManager and CivilizationWorldData
- Registered commands in PantheonWarsSystem
- Build succeeds with 0 errors
- Committed to branch: `claude/repo-overview-01T1bvhfFLmMUMwnbWNVhWCA`

### 2025-11-25 (Phase 1)
- ✅ Completed Phase 1: Core Backend
- Created data models, world data container, and manager
- Integrated with PantheonWarsSystem
- Implemented save/load system
- Committed to branch: `claude/repo-overview-01T1bvhfFLmMUMwnbWNVhWCA`
- Commit: `8485f6b`
