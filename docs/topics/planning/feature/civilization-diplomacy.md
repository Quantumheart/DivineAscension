# Civilization Diplomacy System - Implementation Plan

## Overview

Add diplomatic relationships between civilizations to create strategic alliances and rivalries. The system builds on existing patterns (deity relationships, civilization invites, blessing effects) and integrates with PvP, favor, prestige, and blessing systems.

## Scope

- **Diplomatic relationships at civilization level only** (not individual religions)
- Founder-only diplomatic actions (following civilization governance model)
- Multiple relationship types with meaningful gameplay effects
- Proposal/acceptance system with 7-day expiration (real-time, matching existing invite patterns)
- Integration with PvP rewards, blessing effects, and prestige system
- **Note:** All durations use real-time (DateTime.UtcNow), not in-game time

## Diplomatic Relationship Types

| Status | PvP Allowed | Favor Multiplier | Rank Required | Duration (Real-Time) | Key Effects |
|--------|-------------|------------------|---------------|----------------------|-------------|
| **Neutral** | Yes | 1.0x | None | Permanent | Default state, no special effects |
| **NonAggressionPact** | Violation-based* | N/A | Established | 3 days | PvP violation system, +5% walk speed near allies |
| **Alliance** | Violation-based* | N/A | Renowned | Permanent | Strong bonuses, +10% walk speed, +5% maxhealth, +5% healing |
| **War** | Yes | 1.5x | None | Until peace | +5% damage vs enemy, +10% prestige from PvP, server-wide announcement |

**Violation-based PvP:* Attacks succeed but trigger violation counter. After 3 violations, treaty auto-breaks. No penalties applied.

## Core Data Models

### 1. DiplomaticStatus Enum
**New File:** `/DivineAscension/Models/Enum/DiplomaticStatus.cs`

```csharp
public enum DiplomaticStatus
{
    Neutral = 0,
    NonAggressionPact = 1,
    Alliance = 2,
    War = 3
}
```

### 2. DiplomaticRelationship
**New File:** `/DivineAscension/Data/DiplomaticRelationship.cs`

- `RelationshipId` (string, GUID)
- `CivId1` and `CivId2` (string, bilateral relationship)
- `Status` (DiplomaticStatus enum)
- `EstablishedDate` and `ExpiresDate` (DateTime, nullable for permanent)
- `InitiatorCivId` (string, who proposed)
- `ViolationCount` (int, tracks PvP violations for NAP/Alliance)
- `BreakScheduledDate` (DateTime?, 24-hour warning timestamp)

### 3. DiplomaticProposal
**New File:** `/DivineAscension/Data/DiplomaticProposal.cs`

- `ProposalId` (string, GUID)
- `ProposerCivId` and `TargetCivId` (string)
- `ProposedStatus` (DiplomaticStatus)
- `SentDate` and `ExpiresDate` (7 days, following invite pattern)
- `ProposerFounderUID` (string)
- `Duration` (int?, optional for timed treaties like NAP)
- `IsValid` property (checks expiration)

### 4. DiplomacyWorldData
**New File:** `/DivineAscension/Data/DiplomacyWorldData.cs`

- `Dictionary<string, DiplomaticRelationship> Relationships` (indexed by RelationshipId)
- `List<DiplomaticProposal> PendingProposals`
- `Dictionary<string, List<string>> CivRelationshipMap` (CivId -> List of relationship IDs)
- Helper methods: `GetRelationship()`, `GetProposalsForCiv()`, `AddRelationship()`, etc.
- Use `[ProtoContract]` and `[ProtoMember(n)]` attributes

## Core System: DiplomacyManager

### Interface
**New File:** `/DivineAscension/Systems/Interfaces/IDiplomacyManager.cs`

Key methods:
- `void Initialize()`
- `DiplomaticProposal? ProposeRelationship(string proposerCivId, string targetCivId, DiplomaticStatus proposedStatus, string proposerUID, int? duration = null)`
- `bool AcceptProposal(string proposalId, string accepterUID)`
- `bool DeclineProposal(string proposalId, string declinerUID)`
- `bool ScheduleBreak(string civId1, string civId2, string requesterUID)` (24-hour warning)
- `bool CancelScheduledBreak(string civId1, string civId2, string requesterUID)`
- `bool DeclareWar(string declarerCivId, string targetCivId, string requesterUID)` (unilateral, auto-enters both)
- `bool DeclarePeace(string civId1, string civId2, string requesterUID)` (ends War)
- `DiplomaticStatus GetDiplomaticStatus(string civId1, string civId2)`
- `void RecordPvPViolation(string attackerCivId, string victimCivId)` (tracks NAP/Alliance violations)
- `float GetFavorMultiplier(string civId1, string civId2)`

Events:
- `event Action<string, string, DiplomaticStatus> OnRelationshipEstablished`
- `event Action<string, string, DiplomaticStatus> OnRelationshipEnded`
- `event Action<string, string> OnWarDeclared` (for server-wide announcements)

### Implementation
**New File:** `/DivineAscension/Systems/DiplomacyManager.cs`

Follow CivilizationManager pattern:
- Constructor: `DiplomacyManager(ICoreServerAPI sapi, ICivilizationManager civilizationManager, IReligionPrestigeManager prestigeManager)`
- Data key: `"divineascension_diplomacy"`
- Initialize: Register save/load events, subscribe to `OnCivilizationDisbanded`
- Validation: Check founder permissions, prestige rank requirements, proposal limits
- Auto-cleanup: Expired proposals (7 days), expired treaties (NAP 3 days), scheduled breaks, violation counters
- All durations use real-time (DateTime.UtcNow), not in-game time

**Prestige Rank Requirements:**
- NonAggressionPact: Established rank minimum (both civilizations)
- Alliance: Renowned rank minimum (both civilizations)
- War: No requirement (can always respond to threats)

**War Declaration Mechanics:**
- **Unilateral with auto-response:** Either founder can declare War immediately
- Both civilizations automatically enter War status (no proposal required)
- Target civilization gets notification but cannot decline
- Server-wide announcement: "[CivName1] has declared war on [CivName2]!"
- War ends when either founder declares peace (reverts to Neutral)
- No rank requirement, no cost - ensures players can always respond to threats

**Treaty Breaking:**
- Any treaty (NAP/Alliance) can be broken by either founder at any time
- No prestige penalties or cooldowns (allows strategic flexibility)
- 24-hour warning: `ScheduleBreak()` sets `BreakScheduledDate`, break executes after 24 hours
- During warning period, other party is notified and can prepare
- Either party can cancel scheduled break before it executes
- War can be declared immediately without warning (overrides any existing relationship)

**PvP Violation System (NAP/Alliance):**
- When member attacks allied civilization member, `RecordPvPViolation()` is called
- Increments `ViolationCount` on the relationship
- After 3rd violation, treaty auto-breaks immediately (no 24-hour warning)
- No penalties applied - just relationship dissolution
- Violation counter resets when relationship ends

**Proposal Validation Rules:**
- Only one pending proposal allowed per (proposer, target) pair
- Server validates expiration when accepting (checks `IsValid` property)
- Proposals auto-cancel if relationship changes to incompatible status:
  - War declaration cancels pending NAP/Alliance proposals
  - NAP/Alliance acceptance cancels pending War proposals
- Bidirectional proposals NOT allowed (if A proposes to B, B cannot propose to A until resolved)

**Civilization Disbandment Cleanup:**
When `OnCivilizationDisbanded` fires:
1. Dissolve all relationships where `CivId1` or `CivId2` matches disbanded civilization
2. Cancel all pending proposals (sent to or from disbanded civilization)
3. Cancel scheduled treaty breaks involving disbanded civilization
4. Notify affected civilizations: "Diplomatic relationship with [CivName] ended (civilization disbanded)"
5. Fire `OnRelationshipEnded` events for all dissolved relationships

## Integration with Existing Systems

### 1. CivilizationManager Integration
**Modify:** `/DivineAscension/Systems/CivilizationManager.cs`

- Add public event: `public event Action<string>? OnCivilizationDisbanded`
- Fire event in `DisbandCivilization()` method (line ~512): `OnCivilizationDisbanded?.Invoke(civId);`

### 2. PvP System Integration
**Modify:** `/DivineAscension/Systems/PvPManager.cs`

In `ProcessPvPKill()` method:
1. Inject `IDiplomacyManager` via constructor
2. Get attacker and victim civilizations
3. Query diplomatic status: `_diplomacyManager.GetDiplomaticStatus(attackerCiv, victimCiv)`
4. **Violation-based blocking for NAP/Alliance:**
   - If status is NonAggressionPact or Alliance:
     - PvP attack succeeds (not blocked)
     - Call `_diplomacyManager.RecordPvPViolation(attackerCiv, victimCiv)`
     - Send warning to attacker: "Warning: Attacking allied civilization! (Violation X/3)"
     - If this was 3rd violation, treaty auto-breaks (DiplomacyManager handles this)
     - Do not award favor/prestige for the attack
5. Apply diplomatic multiplier to favor/prestige rewards:
   - Get multiplier: `_diplomacyManager.GetFavorMultiplier(attackerCiv, victimCiv)`
   - Only War status has multiplier (1.5x); others are 1.0x or N/A
   - Combine with deity multiplier: `totalMultiplier = deityMultiplier * diplomaticMultiplier`
   - Apply to rewards

### 3. Blessing System Integration
**Modify:** `/DivineAscension/Systems/BlessingEffectSystem.cs`

Add new method `GetDiplomaticStatModifiers(string playerUID)`:
1. Get player's civilization via `_civilizationManager.GetCivilizationByPlayer(playerUID)`
2. Get all diplomatic relationships for that civilization
3. Apply stat modifiers based on relationship status:
   - **Alliance**: +10% walk speed, +5% maxhealth, +5% healing
   - **War**: +5% damage vs enemy civilization members (context-aware modifier)
   - **NonAggressionPact**: +5% walk speed when near allied members (proximity-based)

Modify `GetCombinedStatModifiers()` to include diplomatic modifiers:
```csharp
CombineModifiers(combined, GetDiplomaticStatModifiers(playerUID));
```

**Note:** Shared blessings are NOT implemented in this phase. Alliance provides direct stat bonuses only.

### 4. Prestige System Integration
**Modify:** `/DivineAscension/Systems/ReligionPrestigeManager.cs`

Subscribe to DiplomacyManager events:
- `OnRelationshipEstablished`: Award prestige bonuses
  - Alliance established: +100 prestige (one-time, awarded to all religions in the civilization)
  - War declared: No prestige bonus (handled by PvP multipliers instead)
- `OnWarDeclared`: Optional server-wide notification integration
- Optional: Track diplomatic history as prestige source for leaderboards

## Network Protocol

### Packet Types
**New Directory:** `/DivineAscension/Network/Diplomacy/`

1. **DiplomacyActionRequestPacket**
   - `Action` (string): "propose", "accept", "decline", "schedulebreak", "cancelbreak", "declarewar", "declarepeace"
   - `CivId`, `TargetCivId`, `ProposalId`, `ProposedStatus`, `Duration`

2. **DiplomacyActionResponsePacket**
   - `Action`, `Success` (bool), `Message` (string), `RelationshipId`, `ViolationCount` (int)

3. **DiplomacyInfoRequestPacket**
   - `CivId` (string)

4. **DiplomacyInfoResponsePacket**
   - `List<RelationshipInfo>` and `List<ProposalInfo>`
   - Nested classes:
     - `RelationshipInfo`: CivId, CivName, Status, EstablishedDate, ExpiresDate, BreakScheduledDate, ViolationCount
     - `ProposalInfo`: ProposalId, CivId, CivName, ProposedStatus, ExpiresDate, IsIncoming

5. **WarDeclarationPacket** (broadcast)
   - `DeclarerCivId`, `DeclarerCivName`, `TargetCivId`, `TargetCivName`
   - Used for server-wide announcements

### Network Handler
**New File:** `/DivineAscension/Systems/Networking/Server/DiplomacyNetworkHandler.cs`

- Constructor: Inject `IDiplomacyManager`, `ICivilizationManager`
- Register handlers for request packets
- Validate player is civilization founder
- Call appropriate DiplomacyManager methods
- Send response packets
- Notify affected civilizations of changes
- Broadcast war declarations server-wide

## UI Components

### Add Diplomacy Tab to Civilization GUI
**Location:** Extend existing GUI in `/DivineAscension/GUI/Managers/CivilizationStateManager.cs`

New components needed:
1. **DiplomacyTabViewModel** - Data model for diplomacy UI state
2. **DiplomacyTabRenderer** - Renders diplomacy UI
3. **DiplomacyTabState** - State management

**UI Sections:**
1. **Current Relationships Panel**
   - List all active relationships with status, partner name, established/expiry dates
   - Color-coded by status (green=Alliance, yellow=NAP, red=War)
   - Show violation count for NAP/Alliance (e.g., "Violations: 1/3")
   - **For NAP/Alliance:** "Schedule Break" button (founder only, triggers 24-hour warning)
   - Show "Treaty break scheduled" if break is pending (with countdown and "Cancel Break" option)
   - **For War:** "Declare Peace" button (founder only, returns to Neutral immediately)

2. **Pending Proposals Panel**
   - Incoming: Show proposer, proposed status, required rank (if applicable), Accept/Decline buttons
   - Outgoing: Show target, proposed status, countdown timer (7 days), Cancel button
   - Note: Cannot send proposal if bidirectional proposal exists

3. **Propose Relationship Panel**
   - Dropdown: Select target civilization
   - Dropdown: Select relationship type (NAP, Alliance, or use "Declare War" button)
   - Display: Prestige rank requirement (show current rank vs required)
   - Slider: Duration (for NAP - 3 days fixed, Alliance - permanent)
   - Button: "Send Proposal" (disabled if insufficient rank or existing proposal)
   - Button: "Declare War" (separate, no requirements, immediate effect)

4. **Notifications**
   - Use existing notification system (SendMessage)
   - Notify all civilization members of diplomatic events
   - Server-wide announcement for war declarations: "[CivName1] has declared war on [CivName2]!"
   - PvP violation warnings: "Warning: Attacking allied civilization! (Violation X/3)"

## Implementation Phases

### Phase 1: Core Data & Manager (Priority 1)
1. Create `DiplomaticStatus` enum (remove TradeAgreement)
2. Create data models: `DiplomaticRelationship`, `DiplomaticProposal`, `DiplomacyWorldData`
   - Include `ViolationCount` and `BreakScheduledDate` in DiplomaticRelationship
3. Create `IDiplomacyManager` interface
4. Implement `DiplomacyManager`:
   - Core CRUD methods (propose, accept, decline, schedulebreak, cancelbreak, declarewar, declarepeace)
   - Persistence (load/save with ProtoContract)
   - Validation logic (founder checks, rank requirements, proposal limits)
   - Proposal system with 7-day expiration
   - 24-hour warning system for treaty breaks
   - PvP violation tracking (3 strikes auto-breaks treaty)
   - Unilateral war declaration (both civs enter War status)
   - Civilization disbandment cleanup
5. Add `OnCivilizationDisbanded` event to CivilizationManager
6. Register DiplomacyManager in system initializer

### Phase 2: System Integration (Priority 2)
1. **PvP Integration:**
   - Modify PvPManager constructor to inject IDiplomacyManager
   - Add diplomatic status queries in `ProcessPvPKill()`
   - Implement violation-based system for NAP/Alliance:
     - Attacks succeed but trigger `RecordPvPViolation()`
     - Send warning messages (Violation X/3)
     - Do not award favor/prestige for violation attacks
   - Apply War multiplier (1.5x) to favor/prestige rewards

2. **Blessing Integration:**
   - Add `GetDiplomaticStatModifiers()` to BlessingEffectSystem
   - Modify `GetCombinedStatModifiers()` to include diplomatic modifiers
   - Alliance: +10% walk speed, +5% maxhealth, +5% healing
   - War: +5% damage vs enemy (context-aware)
   - NAP: +5% walk speed near allies (proximity-based)
   - **Note:** Shared blessings NOT implemented

3. **Prestige Integration:**
   - Subscribe to diplomatic events in ReligionPrestigeManager
   - Award prestige bonuses: Alliance establishment = +100 (one-time, all religions in civ)
   - Subscribe to `OnWarDeclared` for optional server-wide notifications
   - Optional: Track diplomatic history as prestige source

### Phase 3: Network Layer (Priority 3)
1. Create packet classes in `/DivineAscension/Network/Diplomacy/`:
   - DiplomacyActionRequestPacket (updated actions)
   - DiplomacyActionResponsePacket (include ViolationCount)
   - DiplomacyInfoRequestPacket
   - DiplomacyInfoResponsePacket (updated RelationshipInfo with violations and break schedule)
   - WarDeclarationPacket (new, for broadcasts)
2. Implement `DiplomacyNetworkHandler`
3. Register network channel (`"divineascension"`) and packet types
4. Test client-server communication
5. Test war declaration broadcasts

### Phase 4: UI Implementation (Priority 4)
1. Create ViewModels, Renderers, and State classes
2. Add diplomacy tab to Civilization GUI
3. Implement proposal management UI (with validation: no bidirectional proposals)
4. Add relationship listing with violation counters
5. Implement treaty break scheduling UI (24-hour warning display)
6. Add "Declare War" button (separate from proposals)
7. Implement notification system for diplomatic events and violations

### Phase 5: Polish & Balance (Priority 5)
1. Add chat commands (`/diplomacy`, `/war`, `/peace`)
2. Fine-tune multipliers and prestige bonuses
3. Add comprehensive logging (violations, war declarations, treaty breaks)
4. Performance testing (violation tracking overhead)
5. Write unit and integration tests:
   - Violation system (3 strikes)
   - Unilateral war declaration
   - Proposal validation rules
   - Civilization disbandment cleanup
   - 24-hour warning system

## Critical Files Summary

**New Files:**
- `/DivineAscension/Models/Enum/DiplomaticStatus.cs` (3 statuses: Neutral, NAP, Alliance, War)
- `/DivineAscension/Data/DiplomaticRelationship.cs` (includes ViolationCount, BreakScheduledDate)
- `/DivineAscension/Data/DiplomaticProposal.cs`
- `/DivineAscension/Data/DiplomacyWorldData.cs`
- `/DivineAscension/Systems/Interfaces/IDiplomacyManager.cs`
- `/DivineAscension/Systems/DiplomacyManager.cs`
- `/DivineAscension/Systems/Networking/Server/DiplomacyNetworkHandler.cs`
- `/DivineAscension/Network/Diplomacy/*.cs` (5 packet classes including WarDeclarationPacket)
- `/DivineAscension/GUI/Models/Diplomacy/DiplomacyTabViewModel.cs`
- `/DivineAscension/GUI/UI/Renderers/Diplomacy/DiplomacyTabRenderer.cs`
- `/DivineAscension/GUI/State/DiplomacyTabState.cs`

**Modified Files:**
- `/DivineAscension/Systems/CivilizationManager.cs` (add OnCivilizationDisbanded event)
- `/DivineAscension/Systems/PvPManager.cs` (violation tracking, War multiplier)
- `/DivineAscension/Systems/BlessingEffectSystem.cs` (diplomatic stat modifiers, NO shared blessings)
- `/DivineAscension/Systems/ReligionPrestigeManager.cs` (Alliance prestige bonus, war announcements)
- `/DivineAscension/Systems/DivineAscensionSystemInitializer.cs` (register DiplomacyManager)

## Design Decisions & Rationale

1. **Civilizations only (not religions)**: User confirmed this scope. Simpler and more strategic.
2. **Founder-only permissions**: Matches civilization governance model, prevents chaos.
3. **Prestige rank requirements only (no costs)**: User preference. Gates diplomacy by achievement without resource cost. Simpler, more accessible.
4. **Both civilizations must meet rank requirements**: Prevents exploiting by allying with high-rank civ if you're low-rank. Both parties must have earned the privilege.
5. **Removed TradeAgreement**: No implemented benefits - cut to reduce scope and complexity. Can be added in future phase when resource economy is developed.
6. **Violation-based PvP system for NAP/Alliance**: Attacks succeed but trigger violation counter. After 3 violations, treaty auto-breaks. Allows mistakes/testing without immediate consequences, but prevents sustained abuse.
7. **No favor multipliers for NAP/Alliance**: Since PvP uses violation system (not hard block), multipliers would create confusing incentive structure. War has 1.5x multiplier only.
8. **24-hour treaty break warning**: Prevents surprise backstabs. Gives other party time to prepare or negotiate. Can be canceled if parties reconcile.
9. **No penalties for breaking treaties or violations**: User preference for strategic flexibility. Players can adapt to changing situations without permanent consequences.
10. **Timed vs permanent treaties**: NAP (3 days) is short-term tactical agreement. Alliance is permanent (serious commitment). Short NAP duration keeps diplomacy dynamic.
11. **Unilateral war declaration with auto-response**: Either founder can declare War immediately, both civilizations enter War status. No proposal required - ensures reactive capability. Server-wide announcement creates drama.
12. **War is free to declare**: No cost, no rank requirement - ensures players can always respond to threats.
13. **No shared blessings**: Deferred due to complexity (deity compatibility, prerequisites, performance). Alliance provides direct stat bonuses instead.
14. **No bidirectional proposals**: Prevents proposal spam and confusion. If A proposes to B, B must respond before proposing back.
15. **Real-time durations**: All timers use real-time (DateTime.UtcNow) for consistency and to prevent time-manipulation exploits.

## Testing Strategy

**Unit Tests:**
- DiplomacyManager proposal validation (no bidirectional, one per target)
- Relationship state transitions (NAP → War, Alliance → Neutral)
- Prestige rank requirement validation
- Treaty expiration logic (NAP 3 days)
- PvP violation counter (3 strikes auto-breaks)
- War declaration (unilateral, both civs enter War)
- Scheduled break cancellation

**Integration Tests:**
- PvP rewards with War multiplier (1.5x)
- Violation system: attack succeeds, no rewards, counter increments
- Blessing effects applied correctly (Alliance stat bonuses)
- Network packet serialization (all 5 packet types)
- Civilization disbandment cleanup (relationships, proposals, scheduled breaks)

**Manual Test Scenarios:**
1. Alliance formation → verify stat bonuses (+10% walk speed, etc.) → test violation system (3 attacks)
2. War declaration → verify server-wide announcement → verify increased PvP rewards → peace treaty
3. Treaty breaking → verify 24-hour warning → verify no penalties → cancel scheduled break
4. NAP expiration → verify auto-revert to Neutral after 3 days
5. Civilization disbandment → verify relationship cleanup and notifications
6. Proposal edge cases → bidirectional blocked → incompatible proposals canceled by War
7. Violation counter → 3rd violation auto-breaks → counter resets on new relationship

## Balance Configuration

Constants in `DiplomacyConstants.cs`:
- **Favor multipliers:** War=1.5x, others=1.0x (NAP/Alliance removed)
- **Prestige multipliers:** War=1.5x (affects PvP prestige gains)
- **Durations (real-time):** Proposal=7 days, NAP=3 days (Alliance=permanent)
- **Treaty break warning period:** 24 hours (real-time)
- **PvP violation limit:** 3 strikes (then auto-breaks treaty)
- **Rank requirements (both civs must meet):** NAP=Established, Alliance=Renowned, War=None
- **Prestige bonuses:** Alliance establishment=+100 (one-time, all religions in civilization)
- **Stat modifiers:**
  - Alliance: +10% walk speed, +5% maxhealth, +5% healing
  - War: +5% damage vs enemy civilization members
  - NAP: +5% walk speed when near allied members (proximity-based)
