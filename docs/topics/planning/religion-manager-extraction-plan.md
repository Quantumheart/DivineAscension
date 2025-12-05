# Extract Religion Functionality into ReligionManager ✅

## Overview
Extract 26+ religion-related methods from `BlessingDialogManager` (562 lines) into a new `ReligionManager` class using composition pattern. This follows the existing refactored architecture (event-driven, pure renderers, immutable ViewModels).

## User Decisions
- **Blessing methods**: Move to ReligionManager
- **Migration**: Breaking change - update all call sites immediately
- **State ownership**: ReligionManager owns ReligionTabState

## Architecture

### New Structure
```
BlessingDialogManager
├── Religion: ReligionManager (NEW)
│   ├── State: ReligionTabState (owns)
│   ├── CurrentReligionUID, CurrentDeity, etc.
│   ├── PlayerBlessingStates, ReligionBlessingStates
│   ├── RequestReligionList(), RequestPlayerReligionInfo()
│   ├── DrawReligionInvites() (event-driven pattern)
│   └── LoadBlessingStates(), GetBlessingState(), etc.
├── SelectedBlessingId (stays - UI state)
├── CivState (stays - separate concern)
└── Coordination methods delegate to Religion
```

### Access Pattern Changes
```csharp
// BEFORE
manager.RequestReligionList()
manager.ReligionState.CurrentSubTab
manager.GetPlayerFavorProgress()

// AFTER
manager.Religion.RequestReligionList()
manager.Religion.State.CurrentSubTab
manager.Religion.GetPlayerFavorProgress()
```

## Implementation Steps

### Phase 1: Create ReligionManager Infrastructure
1. **Create directory**: `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Managers/`

2. **Create interface**: `Interfaces/IReligionManager.cs`
   - Properties: `State`, `CurrentReligionUID`, `CurrentDeity`, `CurrentReligionName`, `ReligionMemberCount`, `PlayerRoleInReligion` ✅
   - Properties: `CurrentFavorRank`, `CurrentPrestigeRank`, `CurrentFavor`, `CurrentPrestige`, `TotalFavorEarned` ✅
   - Properties: `PlayerBlessingStates`, `ReligionBlessingStates` (dictionaries) ✅
   - Methods: `Initialize()`, `Reset()`, `HasReligion()` ✅
   - Methods: `LoadBlessingStates()`, `GetBlessingState()`, `SetBlessingUnlocked()`, `RefreshAllBlessingStates()` ✅
   - Methods: `GetPlayerFavorProgress()`, `GetReligionPrestigeProgress()`✅
   - Methods: `RequestReligionList()`, `RequestPlayerReligionInfo()`, `RequestReligionAction()`, `RequestEditReligionDescription()` ✅
   - Methods: `UpdateReligionList()`, `UpdatePlayerReligionInfo()` ✅
   - Methods: `DrawReligionInvites()`✅

3. **Create ReligionManager**: `Managers/ReligionManager.cs` (~650 lines) ✅
   - Constructor: `public ReligionManager(ICoreClientAPI capi)`
   - Initialize `_system = capi.ModLoader.GetModSystem<PantheonWarsSystem>()`
   - Initialize `State = new ReligionTabState()`
   - Implement all interface methods (copy from BlessingDialogManager)
   - **Key methods to copy**:
     - State management: `Initialize()`, `Reset()`, `HasReligion()`
     - Blessing state: `LoadBlessingStates()`, `GetBlessingState()`, `SetBlessingUnlocked()`, `RefreshAllBlessingStates()`, `CanUnlockBlessing()` (private)
     - Progression: `GetPlayerFavorProgress()`, `GetReligionPrestigeProgress()`
     - Network: `RequestReligionList()`, `RequestPlayerReligionInfo()`, `RequestReligionAction()`, `RequestEditReligionDescription()`
     - Updates: `UpdateReligionList()`, `UpdatePlayerReligionInfo()`
     - Drawing: `DrawReligionInvites()`, `ConvertToInviteData()`, `ProcessInvitesEvents()`, `HandleAcceptInvite()`, `HandleDeclineInvite()`
   - Add property: `internal IReligionMemberProvider? MembersProvider { get; set; }` (for DEBUG builds)

4. **Build test**: Verify compilation (ReligionManager standalone)

### Phase 2: Integrate into BlessingDialogManager

**File**: `PantheonWars/GUI/BlessingDialogManager.cs`✅

1. **Add property**:
   ```csharp
   public ReligionManager Religion { get; }
   ```

2. **Update constructor**:
   ```csharp
   public BlessingDialogManager(ICoreClientAPI capi)
   {
       _capi = capi;
       Religion = new ReligionManager(capi);

   #if DEBUG
       Religion.MembersProvider = new FakeReligionMemberProvider();
       Religion.MembersProvider.ConfigureDevSeed(500, 20251204);
   #endif
   }
   ```

3. **Update coordination methods** (delegate to Religion): ✅
   ```csharp
   public void Initialize(string? religionUID, DeityType deity, string? religionName,
       int favorRank = 0, int prestigeRank = 0)
   {
       Religion.Initialize(religionUID, deity, religionName, favorRank, prestigeRank);
       // Keep blessing UI state initialization here
       IsDataLoaded = true;
       SelectedBlessingId = null;
       HoveringBlessingId = null;
       PlayerTreeScrollX = 0f;
       PlayerTreeScrollY = 0f;
       ReligionTreeScrollX = 0f;
       ReligionTreeScrollY = 0f;
   }

   public void Reset()
   {
       Religion.Reset();
       // Keep blessing UI state reset here
       SelectedBlessingId = null;
       HoveringBlessingId = null;
       PlayerTreeScrollX = 0f;
       PlayerTreeScrollY = 0f;
       ReligionTreeScrollX = 0f;
       ReligionTreeScrollY = 0f;
       IsDataLoaded = false;

       // Keep civilization state (separate concern)
       CurrentCivilizationId = null;
       CurrentCivilizationName = null;
       CivilizationFounderReligionUID = null;
       CivilizationMemberReligions.Clear();
       CivState.Reset();
   }

   public bool HasReligion() => Religion.HasReligion();

   public BlessingNodeState? GetSelectedBlessingState()
   {
       if (string.IsNullOrEmpty(SelectedBlessingId)) return null;
       return Religion.GetBlessingState(SelectedBlessingId);
   }
   ```

4. **Update IsCivilizationFounder**: ✅
   ```csharp
   public bool IsCivilizationFounder =>
       !string.IsNullOrEmpty(Religion.CurrentReligionUID) &&
       !string.IsNullOrEmpty(CivilizationFounderReligionUID) &&
       Religion.CurrentReligionUID == CivilizationFounderReligionUID;
   ```

5. **Remove 26 methods** (now in ReligionManager): ✅
   - Network: `RequestReligionList()`, `RequestPlayerReligionInfo()`, `RequestReligionAction()`, `RequestEditReligionDescription()`
   - Updates: `UpdateReligionList()`, `UpdatePlayerReligionInfo()`
   - Blessings: `LoadBlessingStates()`, `GetBlessingState()`, `SetBlessingUnlocked()`, `RefreshAllBlessingStates()`, `GetPlayerFavorProgress()`, `GetReligionPrestigeProgress()`, `CanUnlockBlessing()`
   - Drawing: `DrawReligionInvites()`, `ConvertToInviteData()`, `ProcessInvitesEvents()`, `HandleAcceptInvite()`, `HandleDeclineInvite()`

6. **Remove properties** (now in ReligionManager): ✅
   - `ReligionState`, `CurrentReligionUID`, `CurrentDeity`, `CurrentReligionName`
   - `ReligionMemberCount`, `PlayerRoleInReligion`
   - `CurrentFavorRank`, `CurrentPrestigeRank`, `CurrentFavor`, `CurrentPrestige`, `TotalFavorEarned`
   - `PlayerBlessingStates`, `ReligionBlessingStates`
   - `MembersProvider`

7. **Keep in BlessingDialogManager**: ✅
   - `SelectedBlessingId`, `HoveringBlessingId` (UI state for blessing tree)
   - `PlayerTreeScrollX/Y`, `ReligionTreeScrollX/Y` (blessing tree scroll)
   - `CivState`, `CurrentCivilizationId`, etc. (civilization concern)

8. **Build test**: Expect ~19+ compilation errors from call sites

### Phase 3: Update Call Sites (Breaking Changes)

#### File 1: `PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs` ✅
**Changes: 4 call sites**
```csharp
// Line 21: State access
var state = manager.Religion.State;

// Line 38: HasReligion
if (!manager.Religion.HasReligion())

// Line 73, 137, 140: Method calls
manager.Religion.RequestPlayerReligionInfo();
manager.Religion.RequestReligionList(state.DeityFilter);

// Line 158: Drawing
manager.Religion.DrawReligionInvites(x, contentY, width, contentHeight);
```

#### File 2: `PantheonWars/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs` ✅
**Changes: 3 call sites**
```csharp
// Line 24: State access
var state = manager.Religion.State;

// Line 55, 78: Method calls
manager.Religion.RequestReligionList(state.DeityFilter);
var userHasReligion = manager.Religion.HasReligion();

// Line 107, 127: Actions
manager.Religion.RequestReligionAction("join", state.SelectedReligionUID!);
```

#### File 3: `PantheonWars/GUI/UI/Renderers/Religion/ReligionMyReligionRenderer.cs` ✅
**Changes: 7 call sites**
```csharp
// Line 28: State access
var state = manager.Religion.State;

// Line 96: Progression
var prestigeProgress = manager.Religion.GetReligionPrestigeProgress();

// Line 126: Edit description
manager.Religion.RequestEditReligionDescription(religion.ReligionUID, trimmedDesc);

// Line 153: MembersProvider
if (manager.Religion.MembersProvider is { } provider)

// Line 211, 235, 251, 281, 291, 306: Actions
manager.Religion.RequestReligionAction("unban", religion.ReligionUID, playerUID);
manager.Religion.RequestReligionAction("invite", religion.ReligionUID, state.InvitePlayerName.Trim());
manager.Religion.RequestReligionAction("leave", religion.ReligionUID);
manager.Religion.RequestReligionAction("disband", religion.ReligionUID);
manager.Religion.RequestReligionAction("kick", religion.ReligionUID, state.KickConfirmPlayerUID!);
manager.Religion.RequestReligionAction("ban", religion.ReligionUID, state.BanConfirmPlayerUID!);
```

#### File 4: `PantheonWars/GUI/UI/BlessingUIRenderer.cs` ✅
**Changes: 4 call sites**
```csharp
// Line 80-81: State and method
manager.Religion.State.IsBrowseLoading = true;
manager.Religion.RequestReligionList(manager.Religion.State.DeityFilter);

// Line 84-90: HasReligion and state
if (manager.Religion.HasReligion())
{
    manager.Religion.State.IsMyReligionLoading = true;
}
else
{
    manager.Religion.State.IsInvitesLoading = true;
}

// Line 93: Method call
manager.Religion.RequestPlayerReligionInfo();

// Line 174: Blessing state
var hoveringState = manager.Religion.GetBlessingState(hoveringBlessingId);

// Line 177-182: Dictionary access
foreach (var s in manager.Religion.PlayerBlessingStates.Values)
foreach (var s in manager.Religion.ReligionBlessingStates.Values)
```

#### File 5: `PantheonWars/GUI/BlessingDialogEventHandlers.cs` ✅
**Changes: 20+ property accesses**

Key changes:
```csharp
// Initialize calls
_manager!.Religion.Initialize(...);

// Progression properties
_manager.Religion.CurrentFavor = packet.CurrentFavor;
_manager.Religion.CurrentPrestige = packet.CurrentPrestige;
_manager.Religion.TotalFavorEarned = packet.TotalFavorEarned;
_manager.Religion.CurrentFavorRank = (int)favorRankEnum;
_manager.Religion.CurrentPrestigeRank = (int)prestigeRankEnum;

// Blessing methods
_manager.Religion.LoadBlessingStates(playerBlessings, religionBlessings);
_manager.Religion.SetBlessingUnlocked(blessingId, true);
_manager.Religion.RefreshAllBlessingStates();

// State updates
_manager!.Religion.UpdateReligionList(packet.Religions);
_manager!.Religion.UpdatePlayerReligionInfo(packet);

// State access
_manager!.Religion.State.IsBrowseLoading = true;
_manager!.Religion.State.LastActionError = packet.Message;
_manager.Religion.State.ShowDisbandConfirm = false;

// Properties
_manager.Religion.PlayerRoleInReligion = packet.IsFounder ? "Leader" : "Member";
_manager.Religion.ReligionMemberCount = packet.Members.Count;
_manager.Religion.CurrentDeity
```

9. **Build test**: Should compile with 0 errors

### Phase 4: Update Interface

**File**: `PantheonWars/GUI/Interfaces/IBlessingDialogManager.cs`

1. **Remove religion methods** from interface (now in IReligionManager)
2. **Add property**: `ReligionManager Religion { get; }`
3. **Build test**: Final compilation check

## Critical Files

### New Files
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Managers/ReligionManager.cs` (~650 lines)
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Interfaces/IReligionManager.cs` (~60 lines)

### Modified Files
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/BlessingDialogManager.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Interfaces/IBlessingDialogManager.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/UI/Renderers/Religion/ReligionMyReligionRenderer.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/UI/BlessingUIRenderer.cs`
- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/BlessingDialogEventHandlers.cs` (most changes)

## Testing Checklist

1. ✅ **Compilation**: Build succeeds with 0 errors
2. ✅ **Launch**: Game starts, Blessing Dialog opens (Shift+G)
3. ✅ **Religion tab**: All sub-tabs render (Browse, My Religion, Activity, Invites, Create)
4. ✅ **Blessing progression**: Favor/prestige progress displays correctly
5. ✅ **Network**: Join/leave religion requests work
6. ✅ **Event-driven**: Accept/decline invites flow correctly
7. ✅ **State isolation**: Religion state independent of civilization state

## Benefits

- **Separation of concerns**: Religion logic isolated in dedicated manager
- **Smaller files**: BlessingDialogManager reduces from 562 to ~400 lines
- **Testability**: Can mock ReligionManager independently
- **Scalability**: Future refactoring can follow same pattern (CivilizationManager)
- **Consistency**: Follows existing event-driven architecture pattern