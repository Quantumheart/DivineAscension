# Civilization Diplomacy System - Implementation Plan

## Overview

Implement a comprehensive diplomacy system for civilizations with 4 relationship types (Neutral, NonAggressionPact, Alliance, War), proposal/acceptance mechanics, PvP integration with violation tracking, and prestige rewards.

**Based on**: `/home/quantumheart/RiderProjects/DivineAscension/docs/topics/planning/feature/civilization-diplomacy.md`

**Estimated Effort**: 5 phases, ~22-26 hours (updated)

**Key Change**: **Stat modifiers removed** - no blessing system integration. Focus on diplomacy mechanics, PvP system, and prestige rewards only.

---

## Phase 1: Core Data Models & Manager (4-5 hours) ✅

### 1.1 Create Enums and Data Models

- [x] **Create**: `/DivineAscension/Models/Enum/DiplomaticStatus.cs`
  ```csharp
  public enum DiplomaticStatus
  {
      Neutral = 0,
      NonAggressionPact = 1,
      Alliance = 2,
      War = 3
  }
  ```

- [x] **Create**: `/DivineAscension/Data/DiplomaticRelationship.cs`
  - [x] Pattern: Follow `CivilizationData.cs` (ProtoBuf serialization)
  - [x] Properties: `RelationshipId`, `CivId1`, `CivId2`, `Status`, `EstablishedDate`, `ExpiresDate?`, `InitiatorCivId`, `ViolationCount`, `BreakScheduledDate?`
  - [x] Helper properties: `IsExpired`, `IsActive`

- [x] **Create**: `/DivineAscension/Data/DiplomaticProposal.cs`
  - [x] Pattern: Follow `CivilizationInvite` (7-day expiration)
  - [x] Properties: `ProposalId`, `ProposerCivId`, `TargetCivId`, `ProposedStatus`, `SentDate`, `ExpiresDate`, `ProposerFounderUID`, `Duration?`
  - [x] Helper property: `IsValid` (checks `DateTime.UtcNow < ExpiresDate`)

- [x] **Create**: `/DivineAscension/Data/DiplomacyWorldData.cs`
  - [x] Pattern: Follow `CivilizationWorldData.cs`
  - [x] Collections: `Dictionary<string, DiplomaticRelationship> Relationships`, `List<DiplomaticProposal> PendingProposals`, `Dictionary<string, List<string>> CivRelationshipMap`
  - [x] Helper methods: `GetRelationship()`, `GetProposalsForCiv()`, `AddRelationship()`, `RemoveRelationship()`, `CleanupExpiredProposals()`

- [x] **Create**: `/DivineAscension/Constants/DiplomacyConstants.cs`
  - [x] All duration constants (proposal=7 days, NAP=3 days, break warning=24 hours)
  - [x] Multipliers (War favor=1.5x, prestige=1.5x)
  - [x] Rank requirements (NAP=1/Established, Alliance=2/Renowned)
  - [x] Violation limit (3 strikes)
  - [x] Prestige bonus (Alliance=100)
  - [x] **Removed**: Stat modifiers (no blessing integration)

### 1.2 Create DiplomacyManager

- [x] **Create**: `/DivineAscension/Systems/Interfaces/IDiplomacyManager.cs`
  - [x] Key methods: `ProposeRelationship()`, `AcceptProposal()`, `DeclineProposal()`, `ScheduleBreak()`, `CancelScheduledBreak()`, `DeclareWar()`, `DeclarePeace()`, `GetDiplomaticStatus()`, `RecordPvPViolation()`, `GetFavorMultiplier()`
  - [x] Events: `OnRelationshipEstablished`, `OnRelationshipEnded`, `OnWarDeclared`

- [x] **Create**: `/DivineAscension/Systems/DiplomacyManager.cs` (~550 lines)
  - [x] Pattern: Follow `CivilizationManager.cs` structure
  - [x] Constructor: `(ICoreServerAPI sapi, CivilizationManager civilizationManager, IReligionPrestigeManager prestigeManager, IReligionManager religionManager)`
  - [x] Data key: `"divineascension_diplomacy"`
  - [x] Initialize: Register save/load events, subscribe to `OnCivilizationDisbanded`
  - [x] Validation: Founder checks, rank requirements, proposal limits
  - [x] Auto-cleanup: Expired proposals (7 days), expired NAP (3 days), scheduled breaks (24 hours), violation counters (3 strikes)

**Key Logic**:
- [x] **ProposeRelationship()**: Validate founder, check rank requirements (NAP=Established, Alliance=Renowned), check existing proposals, create 7-day proposal
- [x] **AcceptProposal()**: Verify founder, check ranks still valid, create relationship (NAP expires in 3 days, Alliance permanent)
- [x] **DeclareWar()**: Unilateral - both civs automatically enter War, cancel pending NAP/Alliance proposals, fire `OnWarDeclared`
- [x] **ScheduleBreak()**: Set `BreakScheduledDate = DateTime.UtcNow.AddHours(24)`, notify both civs
- [x] **RecordPvPViolation()**: Increment counter, auto-break at 3 violations (no warning)
- [x] **HandleCivilizationDisbanded()**: Dissolve all relationships, cancel proposals, notify affected civs

### 1.3 Add Event to CivilizationManager

- [x] **Modify**: `/DivineAscension/Systems/CivilizationManager.cs`
  - [x] Add after line 26: `public event Action<string>? OnCivilizationDisbanded;`
  - [x] Fire in `DisbandCivilization()` (after line 510): `OnCivilizationDisbanded?.Invoke(civId);`
  - [x] Clear in `Dispose()` (line 53): `OnCivilizationDisbanded = null;`

### 1.4 Register DiplomacyManager

- [x] **Modify**: `/DivineAscension/Systems/DivineAscensionSystemInitializer.cs`
  - [x] Add after line 59 (after PvPManager):
    ```csharp
    var diplomacyManager = new DiplomacyManager(api, civilizationManager, religionPrestigeManager, religionManager);
    diplomacyManager.Initialize();
    ```
  - [x] Add to `InitializationResult` class and return statement

**Critical Order**: DiplomacyManager MUST initialize AFTER CivilizationManager (line 43) and ReligionPrestigeManager (line 50)

---

## Phase 2: System Integration (4-5 hours) ✅

### 2.1 PvP Integration - Violation System & War Multiplier

- [x] **Modify**: `/DivineAscension/Systems/PvPManager.cs`

- [x] **Constructor**: Add `CivilizationManager` and `IDiplomacyManager` parameters (inject after line 30)

- [x] **ProcessPvPKill()** (modify after line 116):
  - [x] Get attacker and victim civilizations via `_civilizationManager.GetCivilizationByPlayer()`
  - [x] Query diplomatic status: `_diplomacyManager.GetDiplomaticStatus(attackerCiv, victimCiv)`
  - [x] **If NAP or Alliance**:
    - [x] Call `_diplomacyManager.RecordPvPViolation(attackerCiv, victimCiv)`
    - [x] Send warning: "Warning: Attacking allied civilization! (Violation X/3)"
    - [x] If 3rd violation, treaty auto-breaks (DiplomacyManager handles)
    - [x] **Do NOT award favor/prestige** (return early)
  - [x] **If War**:
    - [x] Apply 1.5x multiplier to favor and prestige rewards
  - [x] **Otherwise (Neutral)**: Normal rewards (1.0x multiplier)

- [x] **Update Initializer**: Modify PvPManager construction to inject new dependencies (line 57)

### 2.2 Prestige Integration - Alliance Bonus & War Announcements

- [x] **Modify**: `/DivineAscension/Systems/ReligionPrestigeManager.cs`

- [x] **Add Field**: `private IDiplomacyManager? _diplomacyManager;` (after line 26)

- [x] **Add Setter**: `SetDiplomacyManager(IDiplomacyManager diplomacyManager, CivilizationManager civilizationManager)` (after line 41)
  - [x] Store reference and subscribe to events: `OnRelationshipEstablished`, `OnWarDeclared`

- [x] **Add Handler**: `HandleRelationshipEstablished(string civId1, string civId2, DiplomaticStatus status)` (after line 320)
  - [x] **If Alliance**: Award 100 prestige to ALL religions in BOTH civilizations
  - [x] Use existing `AddPrestige()` method for each religion

- [x] **Add Handler**: `HandleWarDeclared(string declarerCivId, string targetCivId)` (after line 320)
  - [x] Get civilization names
  - [x] Broadcast to all online players: "[Diplomacy] {CivName1} has declared WAR on {CivName2}!"
  - [x] Use `SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification)`

- [x] **Add Constructor Parameter**: Inject `CivilizationManager` to get religion lists

- [x] **Update Initializer**: Add after line 75:
  ```csharp
  religionPrestigeManager.SetDiplomacyManager(diplomacyManager, civilizationManager);
  ```

**Note**: Stat modifiers (walk speed, health, damage bonuses) have been **removed** from the design. The diplomacy system now focuses on:
- [x] PvP mechanics (violation system, War multiplier)
- [x] Prestige rewards (Alliance bonus, War announcements)
- [x] Diplomatic relationships and proposals
- [x] No blessing system integration required

---

## Phase 3: Network Layer (5-6 hours) ✅

### 3.1 Create Network Packets

- [x] **Create Directory**: `/DivineAscension/Network/Diplomacy/`

- [x] **Create 5 Packet Classes**:
  - [x] `DiplomacyActionRequestPacket.cs` - Actions: "propose", "accept", "decline", "schedulebreak", "cancelbreak", "declarewar", "declarepeace"
  - [x] `DiplomacyActionResponsePacket.cs` - `Success`, `Message`, `Action`, `RelationshipId`, `ProposalId`, `ViolationCount`
  - [x] `DiplomacyInfoRequestPacket.cs` - `CivId`
  - [x] `DiplomacyInfoResponsePacket.cs` - Nested classes: `RelationshipInfo` (with `ViolationCount`, `BreakScheduledDate`), `ProposalInfo`
  - [x] `WarDeclarationPacket.cs` - `DeclarerCivId`, `DeclarerCivName`, `TargetCivId`, `TargetCivName`

**Pattern**: Follow `CivilizationActionRequestPacket.cs` (ProtoBuf serialization, action discriminator)

### 3.2 Create Server Network Handler

- [x] **Create**: `/DivineAscension/Systems/Networking/Server/DiplomacyNetworkHandler.cs` (~380 lines)
  - [x] Pattern: Follow `CivilizationNetworkHandler.cs`
  - [x] Constructor: `(ICoreServerAPI sapi, IDiplomacyManager diplomacyManager, CivilizationManager civilizationManager, IReligionManager religionManager, IPlayerReligionDataManager playerReligionDataManager, IServerNetworkChannel serverChannel)`
  - [x] Implement `IServerNetworkHandler` with `RegisterHandlers()`

- [x] **Handlers**:
  - [x] `OnDiplomacyInfoRequest()`: Get all relationships and proposals for a civilization, return response
  - [x] `OnDiplomacyActionRequest()`: Switch on action type, call DiplomacyManager methods, validate founder, notify affected civs

- [x] **Helper Methods**:
  - [x] `NotifyTargetCivilization()`: Send message to civilization founder
  - [x] `SendActionResponse()`: Send response packet to player
  - [x] `GetCivName()`: Helper for civilization name lookup
  - [x] `HandleProposeAction()`, `HandleAcceptAction()`, `HandleDeclineAction()`, `HandleScheduleBreakAction()`, `HandleCancelBreakAction()`, `HandleDeclareWarAction()`, `HandleDeclarePeaceAction()`

### 3.3 Register Packets and Handler

- [x] **Modify**: `/DivineAscension/DivineAscensionModSystem.cs`
  - [x] Add using statement: `using DivineAscension.Network.Diplomacy;`
  - [x] Register 5 packet types in `Start()`: `DiplomacyActionRequestPacket`, `DiplomacyActionResponsePacket`, `DiplomacyInfoRequestPacket`, `DiplomacyInfoResponsePacket`, `WarDeclarationPacket`
  - [x] Add field: `private DiplomacyNetworkHandler? _diplomacyNetworkHandler;`
  - [x] Store handler from initialization result

- [x] **Modify**: `/DivineAscension/Systems/DivineAscensionSystemInitializer.cs`
  - [x] Create `DiplomacyNetworkHandler` after line 124
  - [x] Add to `InitializationResult` class and return statement

---

## Phase 4: UI Implementation (8-9 hours) - IMPROVED

### 4.1 Create Diplomacy State ✅

- [x] **Create**: `/DivineAscension/GUI/State/Civilization/DiplomacyState.cs`
  - [x] Properties: `ActiveRelationships`, `IncomingProposals`, `OutgoingProposals`, `SelectedCivId`, `SelectedProposalType`, `SelectedDuration`
  - [x] Confirmation flags: `ConfirmBreakRelationshipId`, `ConfirmWarCivId`
  - [x] UI state: `ErrorMessage`, `IsLoading`, `LastRefresh`
  - [x] Implement `Reset()` method

- [x] **Modify**: `/DivineAscension/GUI/State/CivilizationTabState.cs`
  - [x] Add: `public DiplomacyState DiplomacyState { get; } = new();`
  - [x] Update `CivilizationSubTab` enum: Add `Diplomacy`

### 4.2 Create Diplomacy Renderer ✅

- [x] **Create**: `/DivineAscension/GUI/UI/Renderers/Civilization/DiplomacyTabRenderer.cs` (~430 lines)
  - [x] Pattern: Follow existing renderers in `/GUI/UI/Renderers/Civilization/`

**UI Sections**:

- [x] **1. Current Relationships Panel** (table layout):
  - [x] Columns: Civilization, Status (color-coded badges), Established, Expires, Violations (X/3), Actions
  - [x] Color scheme: Green=Alliance, Yellow=NAP, Red=War, Gray=Neutral
  - [x] Actions: NAP/Alliance → "Schedule Break" button, show countdown if scheduled, "Cancel Break" option
  - [x] Actions: War → "Declare Peace" button

- [x] **2. Pending Proposals Panel**:
  - [x] **Incoming**: Proposer name, proposed status, expires (countdown), rank requirement display, Accept/Decline buttons
  - [x] **Outgoing**: Target name, proposed status, expires (countdown), Cancel button
  - [x] Show note if bidirectional proposal exists

- [x] **3. Propose Relationship Panel**:
  - [x] Dropdown: Select target civilization (exclude current civ, exclude existing relationships)
  - [x] Dropdown: Select relationship type (NAP, Alliance) with rank requirement shown
  - [x] Duration display: NAP = 3 days (fixed), Alliance = Permanent
  - [x] Prestige rank validation: Show current rank vs required (red if insufficient)
  - [x] "Send Proposal" button (disabled if insufficient rank or existing proposal)
  - [x] Separate "Declare War" button (red, always enabled, confirmation dialog)

- [x] **4. Notifications/Warnings**:
  - [x] Display violation warnings
  - [x] Display treaty break countdowns
  - [x] Display rank requirement errors

- [x] **Event Emission**: Return events for button clicks (use record pattern)

### 4.3 Integrate into Civilization GUI ✅

- [x] **Modify**: `/DivineAscension/GUI/Managers/CivilizationStateManager.cs`
  - [x] Add handler: `DrawCivilizationDiplomacy()` - builds ViewModel, calls renderer, processes events
  - [x] Add methods: `RequestDiplomacyInfo()`, `RequestDiplomacyAction()`
  - [x] Subscribe to network events for diplomacy updates
  - [x] Auto-refresh on tab open (see 4.6 below)

- [x] **Modify**: `/DivineAscension/GUI/UI/Renderers/CivilizationTabRenderer.cs`
  - [x] Add "Diplomacy" tab button in tab bar
  - [x] Route to `DiplomacyTabRenderer` when `CurrentSubTab == Diplomacy`

### 4.4 Client Network Integration (~1-1.5 hours) - NEW ✅

- [x] **Modify**: `/DivineAscension/GUI/DivineAscensionNetworkClient.cs`

**Add Packet Handlers** (~150 lines):

- [x] **`OnDiplomacyInfoResponse(DiplomacyInfoResponsePacket packet)`**
  - [x] Deserialize relationships and proposals
  - [x] Update `DiplomacyState.ActiveRelationships`, `IncomingProposals`, `OutgoingProposals`
  - [x] Set `IsLoading = false`
  - [x] Fire event: `DiplomacyInfoReceived?.Invoke(packet.CivId)`

- [x] **`OnDiplomacyActionResponse(DiplomacyActionResponsePacket packet)`**
  - [x] Check `packet.Success`
  - [x] If success: Update local state, clear confirmation flags, show success message
  - [x] If failure: Display `packet.Message` in `ErrorMessage`
  - [x] Fire event: `DiplomacyActionCompleted?.Invoke(packet.Action, packet.Success)`
  - [x] Auto-trigger refresh via `RequestDiplomacyInfo()`

- [x] **`OnWarDeclarationBroadcast(WarDeclarationPacket packet)`**
  - [x] If player is in either civilization: Show prominent notification
  - [x] Update local relationship cache if loaded
  - [x] Fire event: `WarDeclared?.Invoke(packet.DeclarerCivId, packet.TargetCivId)`

**Add Events**:
- [x] `public event Action<string>? DiplomacyInfoReceived;`
- [x] `public event Action<string, bool>? DiplomacyActionCompleted;`
- [x] `public event Action<string, string>? WarDeclared;`
- [x] `public event Action<string, string, DiplomaticStatus>? RelationshipChanged;`

**Error Handling**:
- [x] Add try-catch around all packet handlers
- [x] Log deserialization errors with packet details
- [x] Set `ErrorMessage` on failures
- [x] Implement timeout for network requests (5 seconds)
- [x] Retry logic for failed info requests (max 2 retries)

**Register Handlers**:
- [x] In `RegisterClientHandlers()`: Register all 3 packet types with channel

### 4.5 Notification System (~1 hour) - NEW ✅

- [x] **Create**: `/DivineAscension/GUI/Utilities/DiplomacyNotificationHelper.cs` (~170 lines)

**Notification Types**:
- [x] **Proposals Received**: Toast notification "New diplomacy proposal from {CivName}"
- [x] **Proposals Accepted**: Chat message "{CivName} accepted your {Type} proposal"
- [x] **Proposals Declined**: Chat message "{CivName} declined your {Type} proposal"
- [x] **Proposals Expired**: Notification "Proposal to {CivName} expired"
- [x] **Treaty Established**: Notification "Now {Status} with {CivName}"
- [x] **Treaty Breaking**: Warning "Treaty with {CivName} will break in {Hours} hours"
- [x] **Treaty Broken**: Notification "Treaty with {CivName} has ended"
- [x] **War Declared**: Prominent alert "WAR: {CivName1} vs {CivName2}"
- [x] **Violation Warning**: Urgent alert "PvP violation against ally! ({X}/3)"
- [x] **NAP Expiring Soon**: Notification "NAP with {CivName} expires in 12 hours"

**Implementation**:
- [x] Use `ShowChatMessage()` for all notifications (no EnumChatType parameter)
- [x] Helper methods: `FormatTimeRemaining()`, `IsTimeCritical()`, `GetStatusDisplayName()`, `GetStatusIcon()`

**Integration Points**:
- [x] Integrated with `DivineAscensionNetworkClient` for war declaration notifications
- [x] Used in `DiplomacyTabRenderer` for countdown displays with color coding

### 4.6 UI Refresh Strategy (~30 minutes) - NEW ✅

- [x] **Auto-Refresh on Tab Open**:
  - [x] In `CivilizationStateManager.DrawCivilizationDiplomacy()`: Check if `LastRefresh` is null or > 30 seconds old
  - [x] Call `RequestDiplomacyInfo()` if stale
  - [x] Set `IsLoading = true` while waiting for response

- [x] **Event-Driven Updates**:
  - [x] Network client updates `DiplomacyState` directly in packet handlers
  - [x] `DiplomacyInfoReceived` event fired after state update
  - [x] Auto-refresh triggered after `DiplomacyActionCompleted`

- [x] **Real-Time Countdowns**:
  - [x] In renderer: Uses `DiplomacyNotificationHelper.FormatTimeRemaining()` for proposals and scheduled breaks
  - [x] Display format: "Expires in 6d 14h" or "Expires in 2h 45m" or "Breaks in 23h 12m"
  - [x] Update every frame (ImGui handles this automatically)
  - [x] Change color to red when `IsTimeCritical()` returns true (< 1 hour remaining)

- [ ] **Manual Refresh** (not implemented - auto-refresh is sufficient):
  - [ ] Add "Refresh" button in UI header
  - [ ] Throttle to prevent spam (min 2 seconds between refreshes)

- [ ] **Background Polling** (not implemented - not needed with event-driven updates):
  - [ ] Every 60 seconds: Poll for new incoming proposals if tab is open
  - [ ] Cancel polling when tab is closed or dialog is hidden

---

## Phase 5: Polish & Testing (5-6 hours) - ENHANCED

### 5.1 Add Chat Commands

- [ ] **Create**: `/DivineAscension/Commands/DiplomacyCommands.cs`
  - [ ] `/diplomacy status` - Show diplomatic status with all civilizations
  - [ ] `/diplomacy propose <civId> <type>` - Propose relationship (NAP/Alliance)
  - [ ] `/diplomacy accept <proposalId>` - Accept proposal
  - [ ] `/diplomacy decline <proposalId>` - Decline proposal
  - [ ] `/diplomacy break <civId>` - Schedule treaty break
  - [ ] `/war <civId>` - Declare war (shortcut)
  - [ ] `/peace <civId>` - Declare peace (shortcut)

**Admin/Debug Commands** (require privilege level):
- [ ] `/diplomacy admin view <civId>` - View all relationships for civilization
- [ ] `/diplomacy admin break <civId1> <civId2>` - Force-break relationship
- [ ] `/diplomacy admin clear <civId>` - Clear all relationships and proposals
- [ ] `/diplomacy admin violations <civId1> <civId2>` - View violation count
- [ ] `/diplomacy admin reset` - Reset all diplomacy data (confirmation required)

**Pattern**: Follow existing command handlers in `/Commands/`

### 5.2 Add Comprehensive Logging

- [ ] **Add to**: All managers and handlers
  - [ ] DiplomacyManager: All CRUD operations, violations, war declarations, treaty breaks
  - [ ] PvPManager: Violation tracking, multiplier application
  - [ ] DiplomacyNetworkHandler: All network requests/responses
  - [ ] DivineAscensionNetworkClient: Packet reception, errors

**Pattern**: Use `DiplomacyConstants.LOG_PREFIX` for consistency

**Log Levels**:
- [ ] `Info`: Relationship established/ended, proposals sent/accepted
- [ ] `Warning`: Violations, treaty breaks scheduled, rank requirements failed
- [ ] `Error`: Network failures, data corruption, invalid state transitions
- [ ] `Debug`: All network packets (verbose mode only)

### 5.3 Write Tests

- [ ] **Create**: `/DivineAscension.Tests/Systems/DiplomacyManagerTests.cs`
  - [ ] Proposal validation (no bidirectional, one per target)
  - [ ] Relationship state transitions
  - [ ] Prestige rank validation
  - [ ] Treaty expiration (NAP 3 days)
  - [ ] Violation counter (3 strikes)
  - [ ] War declaration (unilateral)
  - [ ] Scheduled break system
  - [ ] Civilization disbandment cleanup

- [ ] **Create**: `/DivineAscension.Tests/Data/DiplomaticRelationshipTests.cs`
  - [ ] `IsExpired` validation
  - [ ] `IsActive` validation
  - [ ] Bilateral lookup

- [ ] **Create**: `/DivineAscension.Tests/Systems/PvPManagerDiplomacyTests.cs`
  - [ ] War multiplier (1.5x)
  - [ ] Violation system (attack succeeds, no rewards, counter increments)
  - [ ] 3rd violation auto-breaks treaty

- [ ] **Create**: `/DivineAscension.Tests/GUI/State/DiplomacyStateTests.cs` - NEW
  - [ ] State reset behavior
  - [ ] Confirmation flag management
  - [ ] Refresh timestamp tracking

**Pattern**: Follow existing test files, use xUnit v3, Moq for mocking

### 5.4 Edge Case Handling - NEW

- [ ] **Concurrent Operations**:
  - [ ] Both civs propose to each other simultaneously → Accept first received, cancel second
  - [ ] Both civs declare war simultaneously → Both succeed, no duplicate notifications
  - [ ] Founder transfers during pending proposal → Proposal remains valid (founder check on accept)

- [ ] **Disconnection Scenarios**:
  - [ ] Founder offline when proposal received → Proposal persists, accept on next login
  - [ ] Network timeout during action → Client shows error, server state unchanged (idempotent)
  - [ ] Server restart with active relationships → All data persists via world save

- [ ] **Data Integrity**:
  - [ ] Civilization disbands with scheduled break pending → Cancel break, dissolve relationships
  - [ ] NAP expires while founder offline → Auto-revert to Neutral, notify on next login
  - [ ] Violation counter reaches 3 while offline → Treaty breaks, notification on login

- [ ] **UI Edge Cases**:
  - [ ] Proposal expires while UI open → Gray out buttons, show "Expired" badge
  - [ ] Relationship changes while viewing tab → Auto-refresh detects change, updates UI
  - [ ] Invalid action attempt (insufficient rank) → Disable button preemptively, show tooltip

### 5.5 Manual Testing Scenarios

- [ ] **Alliance formation** → verify prestige bonus (+100) → test violation system (3 attacks break treaty)
- [ ] **War declaration** → verify server-wide announcement → verify 1.5x PvP rewards → peace
- [ ] **Treaty breaking** → verify 24-hour warning → cancel scheduled break
- [ ] **NAP expiration** → verify auto-revert to Neutral after 3 days
- [ ] **Civilization disbandment** → verify relationship cleanup
- [ ] **Proposal edge cases** → bidirectional blocked → War cancels pending proposals
- [ ] **Violation counter** → 3rd violation auto-breaks → counter resets on new relationship
- [ ] **UI refresh** → tab open triggers request → countdown timers update in real-time
- [ ] **Notifications** → verify all 10 notification types fire correctly
- [ ] **Client disconnect** → verify state recovery on reconnect

### 5.6 Performance Testing

**Focus**:
- [ ] Violation tracking overhead (<1ms per PvP kill)
- [ ] Proposal cleanup (<10ms)
- [ ] Relationship queries (<1ms)
- [ ] UI rendering with 20+ relationships (<16ms/frame)
- [ ] Network packet serialization (<5ms)

### 5.7 Future Enhancements (Optional)

**Stat Modifiers** (removed from this implementation):
- [ ] Alliance bonuses (+10% walk speed, +5% maxhealth, +5% healing)
- [ ] War damage bonus (+5% vs enemies)
- [ ] NAP proximity bonus (+5% walk speed near allies)

If desired in the future, these would require:
- [ ] BlessingEffectSystem integration
- [ ] Proximity tracking for NAP (position checking, spatial indexing)
- [ ] Context-aware damage for War (Harmony patches, entity context)

---

## Critical Files Summary

### New Files (28 total - increased from 26)

**Phase 1** (9 files):
- [ ] `/DivineAscension/Models/Enum/DiplomaticStatus.cs`
- [ ] `/DivineAscension/Data/DiplomaticRelationship.cs`
- [ ] `/DivineAscension/Data/DiplomaticProposal.cs`
- [ ] `/DivineAscension/Data/DiplomacyWorldData.cs`
- [ ] `/DivineAscension/Systems/Interfaces/IDiplomacyManager.cs`
- [ ] `/DivineAscension/Systems/DiplomacyManager.cs`
- [ ] `/DivineAscension/Constants/DiplomacyConstants.cs`
- [ ] CivilizationManager.cs (modified - add event)
- [ ] DivineAscensionSystemInitializer.cs (modified - register manager)

**Phase 2** (2 files):
- [ ] PvPManager.cs (modified - violation system, War multiplier)
- [ ] ReligionPrestigeManager.cs (modified - Alliance prestige, War announcements)
- [ ] **Removed**: BlessingEffectSystem.cs (no stat modifiers)

**Phase 3** (7 files):
- [ ] `/DivineAscension/Network/Diplomacy/DiplomacyActionRequestPacket.cs`
- [ ] `/DivineAscension/Network/Diplomacy/DiplomacyActionResponsePacket.cs`
- [ ] `/DivineAscension/Network/Diplomacy/DiplomacyInfoRequestPacket.cs`
- [ ] `/DivineAscension/Network/Diplomacy/DiplomacyInfoResponsePacket.cs`
- [ ] `/DivineAscension/Network/Diplomacy/WarDeclarationPacket.cs`
- [ ] `/DivineAscension/Network/Diplomacy/DiplomacyNetworkHandler.cs`
- [ ] DivineAscensionModSystem.cs (modified - register packets)

**Phase 4** (6 files - increased from 4):
- [ ] `/DivineAscension/GUI/State/Civilization/DiplomacyState.cs`
- [ ] `/DivineAscension/GUI/UI/Renderers/Civilization/DiplomacyTabRenderer.cs`
- [ ] `/DivineAscension/GUI/Utilities/DiplomacyNotificationHelper.cs` - NEW
- [ ] CivilizationTabState.cs (modified - add diplomacy state)
- [ ] CivilizationStateManager.cs (modified - integrate tab)
- [ ] DivineAscensionNetworkClient.cs (modified - packet handlers, events) - NEW

**Phase 5** (5 files - increased from 4):
- [ ] `/DivineAscension/Commands/DiplomacyCommands.cs`
- [ ] `/DivineAscension.Tests/Systems/DiplomacyManagerTests.cs`
- [ ] `/DivineAscension.Tests/Data/DiplomaticRelationshipTests.cs`
- [ ] `/DivineAscension.Tests/Systems/PvPManagerDiplomacyTests.cs`
- [ ] `/DivineAscension.Tests/GUI/State/DiplomacyStateTests.cs` - NEW

**Total**: 28 new files, 8 modified files (increased from 26 new/7 modified)

---

## Critical Initialization Order

**MUST preserve this exact order** (from DivineAscensionSystemInitializer.cs):
1. [ ] DeityRegistry (line 37)
2. [ ] ReligionManager (line 40)
3. [ ] **CivilizationManager** (line 43) - fires `OnCivilizationDisbanded`
4. [ ] PlayerReligionDataManager (line 46)
5. [ ] **ReligionPrestigeManager** (line 50) - MUST be before FavorSystem
6. [ ] FavorSystem (line 53)
7. [ ] **NEW: DiplomacyManager** (after line 59) - subscribes to CivilizationManager events
8. [ ] **PvPManager** (line 57) - updated to depend on DiplomacyManager
9. [ ] BlessingRegistry (line 61)
10. [ ] BlessingEffectSystem (line 64) - **no changes needed** (stat modifiers removed)
11. [ ] **religionPrestigeManager.SetDiplomacyManager()** (after line 69) - AFTER DiplomacyManager init

---

## Key Design Decisions Validated

- [x] **Civilizations only** (not religions) - Simpler, more strategic
- [x] **Founder-only permissions** - Matches civilization governance
- [x] **Prestige rank requirements** - Gates diplomacy by achievement
- [x] **Violation-based PvP** (3 strikes) - Allows mistakes without immediate consequences
- [x] **24-hour treaty break warning** - Prevents surprise backstabs
- [x] **No penalties for breaking treaties** - Strategic flexibility
- [x] **Unilateral war declaration** - Ensures reactive capability
- [x] **Real-time durations** - All timers use `DateTime.UtcNow`
- [x] **No stat modifiers** - **Simplified implementation, removes blessing system integration**
- [x] **No shared blessings** - Deferred due to complexity
- [x] **Event-driven UI updates** - NEW: Efficient state synchronization
- [x] **Comprehensive notifications** - NEW: Player awareness of all diplomacy events

---

## Implementation Notes

- [ ] All durations use **real-time** (DateTime.UtcNow), not in-game time
- [ ] **Violation system**: Attacks succeed but trigger counter, 3 strikes auto-breaks treaty
- [ ] **War declaration**: Unilateral with auto-response (both civs enter War status)
- [ ] **Treaty breaks**: 24-hour warning, can be canceled, no penalties
- [ ] **Proposal validation**: Only one pending proposal per (proposer, target) pair, no bidirectional
- [ ] **Cleanup**: Civilization disbandment dissolves all relationships and cancels proposals
- [ ] **Network pattern**: Action discriminator in single request packet (switch statement)
- [ ] **Data persistence**: ProtoBuf serialization with `"divineascension_diplomacy"` key
- [ ] **UI refresh**: Event-driven updates + auto-refresh on tab open + real-time countdowns
- [ ] **Error handling**: Network timeouts, retry logic, graceful degradation

---

## Risks & Mitigation

1. **Stat modifiers removed** → Significantly reduces complexity, no blessing system integration needed
2. **Network packet serialization** → Test thoroughly, follow established ProtoBuf patterns
3. **UI state synchronization** → Event-driven updates mitigate stale data, auto-refresh on tab open
4. **Violation system abuse** → Intentional design (allows reactive breaking), log for admin review
5. **Client-server timing** → Use server timestamps for all expiration calculations
6. **Notification spam** → Throttle notifications, allow user preferences for filtering

---

## Summary of Improvements

### Phase 4 Enhancements:
1. **4.4 Client Network Integration** - Complete specification of packet handlers, events, and error handling
2. **4.5 Notification System** - 10 notification types with implementation details
3. **4.6 UI Refresh Strategy** - Event-driven + auto-refresh + real-time countdown mechanism

### Phase 5 Enhancements:
1. **5.1** - Added admin/debug commands for testing and moderation
2. **5.4** - New edge case handling section covering concurrency, disconnections, data integrity
3. **5.7** - Expanded logging strategy with specific log levels

### Document-Wide Changes:
- All tasks converted to checkbox format (- [ ]) for progress tracking
- Nested checkboxes for subtasks
- Maintained code blocks and technical details
- Added 2 new files (DiplomacyNotificationHelper, DiplomacyStateTests)
- Increased Phase 4 estimate from 6-7 hours to 8-9 hours
- Increased total estimate from 18-22 hours to 22-26 hours

---

**Ready for implementation with comprehensive client-side networking, notifications, and UI refresh strategy.**
