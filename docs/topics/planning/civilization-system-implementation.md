# Civilization System Implementation Plan

**Status**: Phase 1 Complete ✅
**Current Phase**: Phase 2 (Pending)
**Last Updated**: 2025-11-25

## Overview

Implementation of a minimal civilization system for PantheonWars - organizational containers that allow 2-4 different-deity religions to form alliances. This MVP focuses on core functionality without buildings or mechanical bonuses.

### Design Principles

- **Minimal viable product**: Test core concept before adding complexity
- **Deity diversity**: Each civilization must have different deities (no duplicates)
- **Founder-controlled**: Only religion founders can manage civilization membership
- **Cooldown periods**: 7-day cooldowns prevent rapid civ-hopping
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

## Phase 2: Commands & Networking ⏳ PENDING

**Duration**: Week 2
**Status**: Not Started
**Dependencies**: Phase 1 complete

### Tasks

#### Task 2.1: Create Network Packets
**Status**: Pending
**Files to Create**:
- `PantheonWars/Network/CivilizationPackets.cs`
  - `CivilizationListRequestPacket`
  - `CivilizationListResponsePacket`
  - `CivilizationActionRequestPacket` (create, invite, accept, leave, kick, disband)
  - `CivilizationActionResponsePacket`
  - `CivilizationInfoRequestPacket`
  - `CivilizationInfoResponsePacket`

**Files to Modify**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Register new packet types in `Start()` method

#### Task 2.2: Implement Server-Side Networking
**Status**: Pending
**Files to Modify**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Add packet handlers in `SetupServerNetworking()`
  - `OnCivilizationListRequest()`
  - `OnCivilizationActionRequest()`
  - `OnCivilizationInfoRequest()`

**Logic to Implement**:
- Handle civilization creation requests
- Handle invite/accept/leave/kick/disband actions
- Validate permissions (founder checks)
- Send appropriate responses with success/error messages
- Notify affected players

#### Task 2.3: Implement Client-Side Networking
**Status**: Pending
**Files to Modify**:
- `PantheonWars/PantheonWarsSystem.cs`
  - Add packet handlers in `SetupClientNetworking()`
  - Add public request methods for UI to call
  - Add events for UI to subscribe to

**Methods to Add**:
- `RequestCivilizationList()`
- `RequestCivilizationInfo(string civId)`
- `RequestCivilizationAction(string action, ...)`

**Events to Add**:
- `CivilizationListReceived`
- `CivilizationInfoReceived`
- `CivilizationActionCompleted`

#### Task 2.4: Create Command System
**Status**: Pending
**Files to Create**:
- `PantheonWars/Commands/CivilizationCommands.cs`

**Commands to Implement**:
- `/civ create <name>` - Create a civilization
- `/civ invite <religionName>` - Invite a religion
- `/civ accept <inviteId>` - Accept an invitation
- `/civ leave` - Leave civilization
- `/civ kick <religionName>` - Kick a religion
- `/civ disband` - Disband civilization
- `/civ list` - List all civilizations
- `/civ info [civName]` - Show civilization details
- `/civ invites` - Show pending invites

**Validation**:
- Permission checks (founder only for certain commands)
- Proper error messages
- Success confirmations

### Success Criteria

- [ ] All packet types registered and handlers implemented
- [ ] Server validates all actions and sends appropriate responses
- [ ] Client can request data and receive updates
- [ ] All commands functional and accessible via chat
- [ ] Proper error messages for invalid actions
- [ ] Events fire correctly for UI updates

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

- [ ] BlessingDialog shows civilization info
- [ ] Players can see their civilization membership
- [ ] Founders can access management features
- [ ] (Optional) Full civilization dialog functional
- [ ] (Optional) Info overlay displays correctly
- [ ] UI matches existing mod style (VSImGui)
- [ ] All UI updates in response to events

---

## Phase 4: Testing & Release ⏳ PENDING

**Duration**: Week 4
**Status**: Not Started
**Dependencies**: Phase 3 complete

### Tasks

#### Task 4.1: Manual Testing
**Status**: Pending

**Test Cases**:
- [ ] Create civilization as religion founder
- [ ] Invite religions from different deities
- [ ] Accept/reject invitations
- [ ] Leave civilization (verify cooldown)
- [ ] Kick religion (verify cooldown)
- [ ] Disband civilization
- [ ] Verify deity diversity enforcement
- [ ] Test 2-4 religion capacity limits
- [ ] Test auto-disband when below 2 religions
- [ ] Verify invite expiry (7 days)
- [ ] Verify cooldown expiry (7 days)
- [ ] Test persistence (save/load)

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
- Auto-cleanup of expired invites/cooldowns on save/load
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
- [ ] Should cooldowns be configurable per server?

---

## Changelog

### 2025-11-25
- ✅ Completed Phase 1: Core Backend
- Created data models, world data container, and manager
- Integrated with PantheonWarsSystem
- Implemented save/load system
- Committed to branch: `claude/repo-overview-01T1bvhfFLmMUMwnbWNVhWCA`
- Commit: `8485f6b`
