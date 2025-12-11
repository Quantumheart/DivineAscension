# UI Network Client Audit & Refactoring Plan

## Executive Summary

The PantheonWars UI has **8 major code quality issues** in how it interacts with `PantheonWarsNetworkClient`. The good
news: You've already built 90% of the solution with your Event-Driven Architecture (EDA) pattern in the Religion tab.
This plan outlines how to complete the migration and eliminate these issues.

---

## Audit Findings

### Problem 1: DRY Violation - Repeated Boilerplate

**Severity**: High
**Occurrences**: 20+ instances across 4 files

**Pattern**:

```csharp
var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
system?.NetworkClient?.RequestSomething(...);
```

**Locations**:

- `ReligionStateManager.cs:151-152, 178-179, 186-187, 197-198` (4 times)
- `BlessingStateManager.cs:120-123` (1 time, with verbose null checking)
- `GuiDialogManager.cs:156-157, 179-180, 190-191` (3 times)
- `GuiDialogHandlers.cs` (9 times across various event handlers)

**Impact**: Maintenance burden, potential for inconsistency, difficult to test

---

### Problem 2: Tight Coupling

**Severity**: High
**Issue**: State managers directly depend on `PantheonWarsSystem` concrete class

**Evidence**:

```csharp
// ReligionStateManager.cs:33, 52
private readonly PantheonWarsSystem _system;
_system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();

// BlessingStateManager.cs:19, 26
private readonly PantheonWarsSystem? _system;
_system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
```

**Impact**:

- Cannot test UI logic without entire network stack
- Violates Dependency Inversion Principle
- Makes refactoring network layer risky

---

### Problem 3: Inconsistent Error Handling

**Severity**: Medium
**Issue**: Different error handling strategies across UI components

**Patterns Found**:

1. **GuiDialog.cs**: Chat messages + logging
2. **ReligionStateManager.cs**: State-based (`State.ErrorState.BrowseError`)
3. **BlessingStateManager.cs**: Logging only (lines 127-128)
4. **CivilizationRenderers**: No explicit error handling (relies on events)

**Impact**: Inconsistent user experience, difficult to debug

---

### Problem 4: Inconsistent Null Checking

**Severity**: Medium
**Issue**: Mixed null safety patterns

**Examples**:

```csharp
// BlessingStateManager.cs:120-129 - Verbose but safe
if (_system?.NetworkClient != null) {
    _system.NetworkClient.RequestBlessingUnlock(...);
} else {
    _coreClientApi.Logger.Warning("...");
}

// ReligionStateManager.cs:152 - Terse, assumes non-null
system?.NetworkClient?.RequestReligionList(deityFilter);

// GuiDialogManager.cs:157 - Same terse pattern
system?.NetworkClient?.RequestCivilizationList(deityFilter);
```

**Impact**: Potential null reference exceptions, inconsistent defensive programming

---

### Problem 5: Scattered Network Calls

**Severity**: High
**Issue**: 30+ network calls spread across 11 files

**Distribution**:

- GuiDialog.cs: 6 calls
- GuiDialogHandlers.cs: 9 calls
- ReligionStateManager.cs: 5 calls
- BlessingStateManager.cs: 1 call
- GuiDialogManager.cs: 3 calls
- 7 renderer files: 1-4 calls each

**Impact**:

- Hard to find all network usage
- Difficult to add cross-cutting concerns (logging, metrics, retries)
- Cannot enforce consistent patterns

---

### Problem 6: Inconsistent Loading State Management

**Severity**: Low
**Issue**: Some calls set loading flags, others don't

**Examples**:

```csharp
// GuiDialogManager.cs:153-157 - Sets loading state
CivState.IsBrowseLoading = true;
CivState.BrowseError = null;
var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
system?.NetworkClient?.RequestCivilizationList(deityFilter);

// ReligionStateManager.cs:151-152 - Same pattern (good!)
State.BrowseState.IsBrowseLoading = true;
State.ErrorState.BrowseError = null;
system?.NetworkClient?.RequestReligionList(deityFilter);

// Some renderers - No loading state management
manager.RequestCivilizationAction("accept", "", invite.InviteId);
```

**Impact**: Inconsistent UI feedback, user confusion

---

### Problem 7: Architectural Inconsistency

**Severity**: Medium
**Issue**: Civilization state managed differently than Religion/Blessing

**Current State**:

- ✅ **ReligionStateManager** exists (EDA pattern, 711 lines)
- ✅ **BlessingStateManager** exists (EDA pattern, 231 lines)
- ❌ **CivilizationStateManager** missing - state in `GuiDialogManager` + scattered renderers

**Evidence**:

```csharp
// GuiDialogManager.cs:36-45 - Civilization state mixed with manager
public CivilizationState CivState { get; } = new();
public string? CurrentCivilizationId { get; set; }
public string? CurrentCivilizationName { get; set; }
// ... more civilization fields

// GuiDialogManager.cs:151-192 - Manager has network methods (wrong layer!)
public void RequestCivilizationList(string deityFilter = "") { ... }
public void RequestCivilizationInfo(string civIdOrEmpty = "") { ... }
public void RequestCivilizationAction(...) { ... }
```

**Impact**: Breaks Single Responsibility Principle, harder to maintain

---

### Problem 8: No Request Deduplication

**Severity**: Low
**Issue**: Cascading requests can cause duplicate network calls

**Example**:

```csharp
// GuiDialogHandlers.cs - After blessing unlock, triggers 3 requests:
_pantheonWarsSystem?.NetworkClient?.RequestBlessingData();      // 1
_pantheonWarsSystem?.NetworkClient?.RequestPlayerReligionInfo(); // 2
_pantheonWarsSystem?.NetworkClient?.RequestCivilizationInfo(""); // 3
```

**Impact**: Increased server load, potential race conditions

---

## Recommended Solution: Service Layer Pattern

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         UI Layer                            │
│  GuiDialog → GuiDialogManager → StateManagers              │
│                                                             │
│  State Managers (EDA pattern):                             │
│  - ReligionStateManager                                    │
│  - BlessingStateManager                                    │
│  - CivilizationStateManager (NEW)                          │
└─────────────────────┬───────────────────────────────────────┘
                      │ depends on abstraction
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                Service Layer (NEW)                          │
│                                                             │
│  IPantheonWarsUIService (interface)                         │
│  PantheonWarsUIService (implementation)                     │
│  - Centralizes all network access                          │
│  - Handles null checking & validation                      │
│  - Standardizes error handling                             │
│  - Enables testing via mocking                             │
└─────────────────────┬───────────────────────────────────────┘
                      │ uses
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    Network Layer                            │
│                                                             │
│  PantheonWarsNetworkClient                                  │
│  - Network I/O only                                        │
│  - Fires events on responses                               │
└─────────────────────────────────────────────────────────────┘
```

### Why This Works

1. **Single Responsibility**: Each layer has one job
    - State Managers: UI state + event processing
    - Service Layer: Network coordination + validation
    - Network Client: Network I/O

2. **DRY**: Eliminates `GetModSystem<PantheonWarsSystem>()` boilerplate

3. **Testability**: Mock `IPantheonWarsUIService` for UI tests

4. **Consistency**: Follows existing EDA pattern you've already built

5. **Loose Coupling**: State managers depend on abstraction, not concrete network client

---

## Implementation Plan

### Phase 1: Create Service Layer

#### 1.1 Create Interface

**File**: `PantheonWars/Services/Interfaces/IPantheonWarsUIService.cs` (NEW)

```csharp
namespace PantheonWars.Services.Interfaces;

public interface IPantheonWarsUIService
{
    // Blessing Operations
    void RequestBlessingData();
    void RequestBlessingUnlock(string blessingId);

    // Religion Operations
    void RequestReligionList(string deityFilter = "");
    void RequestPlayerReligionInfo();
    void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "");
    void RequestCreateReligion(string religionName, string deity, bool isPublic);
    void RequestEditDescription(string religionUID, string description);

    // Civilization Operations
    void RequestCivilizationList(string deityFilter = "");
    void RequestCivilizationInfo(string civId);
    void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "");

    // Utility
    bool IsNetworkAvailable();
}
```

**Benefits**:

- Single source of truth for UI network operations
- Mockable for testing
- Clear contract for service layer

#### 1.2 Create Implementation

**File**: `PantheonWars/Services/PantheonWarsUIService.cs` (NEW)

**Key Features**:

- Constructor injection: `PantheonWarsUIService(ICoreClientAPI capi, PantheonWarsNetworkClient networkClient)`
- Centralized null checking in `IsNetworkAvailable()`
- Consistent error logging
- Parameter validation
- Thin wrapper (no business logic)

**Responsibilities**:

- ✅ Network availability checking
- ✅ Parameter validation
- ✅ Error logging
- ✅ Delegate to network client
- ❌ NO state management (belongs in StateManagers)
- ❌ NO event processing (belongs in StateManagers)

---

### Phase 2: Update State Managers

#### 2.1 Update ReligionStateManager

**File**: `PantheonWars/GUI/Managers/ReligionStateManager.cs`

**Changes**:

```csharp
// REMOVE this field
- private readonly PantheonWarsSystem _system;

// ADD this field
+ private readonly IPantheonWarsUIService _uiService;

// UPDATE constructor
- public ReligionStateManager(ICoreClientAPI coreClientApi)
+ public ReligionStateManager(ICoreClientAPI coreClientApi, IPantheonWarsUIService uiService)
{
    _coreClientApi = coreClientApi;
-   _system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
+   _uiService = uiService;
}

// UPDATE all network calls (4 locations: lines 151-152, 178-179, 186-187, 197-198)
- var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
- system?.NetworkClient?.RequestReligionList(deityFilter);
+ _uiService.RequestReligionList(deityFilter);
```

**Lines to modify**: 33, 52, 151-152, 178-179, 186-187, 197-198

#### 2.2 Update BlessingStateManager

**File**: `PantheonWars/GUI/Managers/BlessingStateManager.cs`

**Changes**:

```csharp
// REMOVE this field
- private readonly PantheonWarsSystem? _system;

// ADD this field
+ private readonly IPantheonWarsUIService _uiService;

// UPDATE constructor
- public BlessingStateManager(ICoreClientAPI api)
+ public BlessingStateManager(ICoreClientAPI api, IPantheonWarsUIService uiService)
{
    _coreClientApi = api;
-   _system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
+   _uiService = uiService;
}

// UPDATE HandleUnlockClicked (lines 120-129)
- if (_system?.NetworkClient != null)
- {
-     _coreClientApi.Logger.Debug($"[PantheonWars] Sending unlock request for: {selectedState.Blessing.Name}");
-     _system.NetworkClient.RequestBlessingUnlock(selectedState.Blessing.BlessingId);
- }
- else
- {
-     _coreClientApi.Logger.Warning("[PantheonWars] Cannot unlock blessing: ...");
- }
+ _coreClientApi.Logger.Debug($"[PantheonWars] Sending unlock request for: {selectedState.Blessing.Name}");
+ _uiService.RequestBlessingUnlock(selectedState.Blessing.BlessingId);
```

**Lines to modify**: 19, 26, 120-129

---

### Phase 3: Create CivilizationStateManager

#### 3.1 Create New State Manager

**File**: `PantheonWars/GUI/Managers/CivilizationStateManager.cs` (NEW)

**Responsibilities**:

- Manage `CivilizationState`
- Handle civilization-specific fields (CurrentCivilizationId, CurrentCivilizationName, etc.)
- Process civilization events
- Make network requests via service

**Pattern**: Follow ReligionStateManager structure

- `State { get; }` property for UI state
- Request methods that set loading flags and call service
- Event processing methods
- Update methods for server responses

#### 3.2 Move Civilization Logic from GuiDialogManager

**File**: `PantheonWars/GUI/GuiDialogManager.cs`

**Move these to CivilizationStateManager**:

- Lines 36-45: All civilization state fields
- Lines 151-192: All civilization request methods
- Lines 101-146: `UpdateCivilizationState()` method

**Keep in GuiDialogManager**:

- High-level orchestration (`HasCivilization()`, etc.)
- Reference to `CivilizationStateManager`

---

### Phase 4: Update GuiDialogManager

**File**: `PantheonWars/GUI/GuiDialogManager.cs`

**Changes**:

```csharp
// UPDATE constructor signature
- public GuiDialogManager(ICoreClientAPI capi)
+ public GuiDialogManager(ICoreClientAPI capi, IPantheonWarsUIService uiService)
{
    _capi = capi;
-   ReligionStateManager = new ReligionStateManager(capi);
-   BlessingStateManager = new BlessingStateManager(capi);
+   ReligionStateManager = new ReligionStateManager(capi, uiService);
+   BlessingStateManager = new BlessingStateManager(capi, uiService);
+   CivilizationStateManager = new CivilizationStateManager(capi, uiService);

    // ... DEBUG setup
}

// REMOVE: All civilization state fields (lines 36-45)
- public CivilizationState CivState { get; } = new();
- public string? CurrentCivilizationId { get; set; }
// ... etc.

// REMOVE: All Request methods (lines 151-192)
- public void RequestCivilizationList(string deityFilter = "") { ... }
- public void RequestCivilizationInfo(string civIdOrEmpty = "") { ... }
- public void RequestCivilizationAction(...) { ... }

// ADD: CivilizationStateManager property
+ public CivilizationStateManager CivilizationStateManager { get; }

// UPDATE: HasCivilization() to delegate
public bool HasCivilization()
{
-   return !string.IsNullOrEmpty(CurrentCivilizationId);
+   return CivilizationStateManager.HasCivilization();
}
```

**Lines to modify**: 20, 23-24, 36-45, 93-96, 101-146, 151-192

---

### Phase 5: Wire Up Service in GuiDialog

**File**: `PantheonWars/GUI/GuiDialog.cs`

**Changes**:

```csharp
// ADD field
+ private IPantheonWarsUIService? _uiService;

// UPDATE StartClientSide
public override void StartClientSide(ICoreClientAPI api)
{
    _capi = api;

    // Initialize PantheonWars system
    _pantheonWarsSystem = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
+
+   // Create service
+   if (_pantheonWarsSystem?.NetworkClient != null)
+   {
+       _uiService = new PantheonWarsUIService(_capi, _pantheonWarsSystem.NetworkClient);
+   }

-   _manager = new GuiDialogManager(_capi);
+   _manager = new GuiDialogManager(_capi, _uiService!);

    // Subscribe to events (unchanged)
    if (_pantheonWarsSystem?.NetworkClient != null)
    {
        _pantheonWarsSystem.NetworkClient.BlessingUnlocked += OnBlessingUnlockedFromServer;
        // ... other 9 events
    }
}
```

---

### Phase 6: Update Event Handlers

**File**: `PantheonWars/GUI/GuiDialogHandlers.cs`

**Replace all network calls** (9 locations):

```csharp
// Pattern to find:
- _pantheonWarsSystem?.NetworkClient?.RequestSomething(...)

// Replace with:
+ _uiService?.RequestSomething(...)
```

**Approximate locations** (from exploration):

- Lines 29, 111, 114, 135, 219, 229, 437 (and similar patterns)

**Note**: Event handlers will continue to subscribe to `PantheonWarsNetworkClient` events - this is correct! The network
client fires events, the service provides request methods.

---

### Phase 7: Update Renderer Files

**Files**:

- `CivilizationBrowseRenderer.cs`
- `CivilizationCreateRenderer.cs`
- `CivilizationInvitesRenderer.cs`
- `CivilizationManageRenderer.cs`
- `CivilizationTabRenderer.cs`

**Changes**:
Replace calls like:

```csharp
- manager.RequestCivilizationList(...)
+ manager.CivilizationStateManager.RequestCivilizationList(...)
```

This reflects the new architecture where civilization requests go through the state manager.

---

## Migration Strategy

### Recommended: Incremental Migration

**Week 1: Foundation**

1. ✅ Create `IPantheonWarsUIService` interface
2. ✅ Create `PantheonWarsUIService` implementation
3. ✅ Add unit tests for service (optional but recommended)

**Week 2: State Managers**

4. ✅ Update `BlessingStateManager` constructor + network calls
5. ✅ Update `ReligionStateManager` constructor + network calls
6. ✅ Test each state manager independently

**Week 3: Civilization Extraction**

7. ✅ Create `CivilizationStateManager`
8. ✅ Move civilization logic from `GuiDialogManager`
9. ✅ Update `GuiDialogManager` constructor

**Week 4: Integration**

10. ✅ Wire up service in `GuiDialog`
11. ✅ Update `GuiDialogHandlers.cs`
12. ✅ Update renderer files
13. ✅ Integration testing

### Alternative: Big Bang (Not Recommended)

- All changes at once
- High risk of breaking functionality
- Difficult to isolate issues
- Hard to code review

---

## Expected Benefits

### Before Refactoring

- ❌ 20+ boilerplate `GetModSystem` calls
- ❌ 30+ scattered network calls across 11 files
- ❌ Inconsistent null checking
- ❌ 3 different error handling patterns
- ❌ State managers tightly coupled to `PantheonWarsSystem`
- ❌ Cannot test UI logic without network layer
- ❌ Civilization state scattered across manager + renderers

### After Refactoring

- ✅ 1 service class centralizes network access
- ✅ 0 boilerplate in state managers (constructor injection)
- ✅ Consistent null checking in service layer
- ✅ Standardized error handling pattern
- ✅ State managers depend on abstraction (`IPantheonWarsUIService`)
- ✅ Easy to mock service for UI tests
- ✅ Civilization state properly encapsulated in `CivilizationStateManager`
- ✅ Follows established EDA pattern consistently

### Metrics

| Metric                      | Before         | After           | Improvement       |
|-----------------------------|----------------|-----------------|-------------------|
| `GetModSystem` calls        | 20+            | 1               | 95% reduction     |
| Direct network calls in UI  | 30+            | 0               | 100% elimination  |
| Files with network coupling | 11             | 1 (service)     | 91% reduction     |
| Testable state managers     | 0              | 3               | ∞                 |
| Lines of boilerplate        | ~60            | ~10             | 83% reduction     |
| Architectural consistency   | 67% (2/3 tabs) | 100% (3/3 tabs) | Perfect alignment |

---

## Critical Files

### Files to Create (3)

1. `PantheonWars/Services/Interfaces/IPantheonWarsUIService.cs` (NEW - ~40 lines)
2. `PantheonWars/Services/PantheonWarsUIService.cs` (NEW - ~150 lines)
3. `PantheonWars/GUI/Managers/CivilizationStateManager.cs` (NEW - ~300 lines estimated)

### Files to Modify (7)

1. `PantheonWars/GUI/GuiDialog.cs` - Wire up service (~10 lines changed)
2. `PantheonWars/GUI/GuiDialogManager.cs` - Update constructor, remove civilization logic (~60 lines changed)
3. `PantheonWars/GUI/Managers/ReligionStateManager.cs` - Update constructor + 4 network calls (~10 lines changed)
4. `PantheonWars/GUI/Managers/BlessingStateManager.cs` - Update constructor + 1 network call (~5 lines changed)
5. `PantheonWars/GUI/GuiDialogHandlers.cs` - Replace 9 network calls (~9 lines changed)
6. `PantheonWars/GUI/UI/Renderers/Civilization/*` - Update manager references (~15 lines across 5 files)
7. `PantheonWars/GUI/UI/MainDialogRenderer.cs` - Update manager references (~4 lines changed)

---

## Code Samples

### Before: ReligionStateManager Network Call

```csharp
public void RequestReligionList(string? deityFilter = "")
{
    State.BrowseState.IsBrowseLoading = true;
    State.ErrorState.BrowseError = null;
    var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();  // Boilerplate
    if (deityFilter != null)
        system?.NetworkClient?.RequestReligionList(deityFilter);  // Null checking
}
```

**Issues**: Boilerplate, null checking, tight coupling, untestable

### After: ReligionStateManager Network Call

```csharp
public void RequestReligionList(string? deityFilter = "")
{
    State.BrowseState.IsBrowseLoading = true;
    State.ErrorState.BrowseError = null;
    if (deityFilter != null)
        _uiService.RequestReligionList(deityFilter);  // Clean!
}
```

**Benefits**: No boilerplate, service handles null checking, testable, loosely coupled

---

## Decision Log

### Decision 1: Service Layer vs Facade

**Chosen**: Service Layer (thin wrapper)
**Rationale**: Network client already well-designed, just need cleaner access

### Decision 2: Constructor Injection vs Property Injection

**Chosen**: Constructor Injection
**Rationale**: Explicit dependencies, compile-time safety, standard C# practice

### Decision 3: Events vs Async/Await

**Chosen**: Keep existing Events
**Rationale**: Works well for multiplayer, network layer already uses events

### Decision 4: Error Handling Strategy

**Chosen**: Hybrid (service validates, state managers track UI errors)
**Rationale**: Follows existing patterns, flexibility per feature

### Decision 5: Create CivilizationStateManager

**Chosen**: Yes, include in refactoring
**Rationale**: Achieves architectural consistency across all 3 tabs

---

## Risk Assessment

### Low Risk

- ✅ Creating new service layer (additive change)
- ✅ Adding constructor parameters (controlled injection)
- ✅ Moving civilization logic (internal refactoring)

### Medium Risk

- ⚠️ Updating all network call sites (many locations, but mechanical)
- ⚠️ Testing all network flows (need comprehensive testing)

### High Risk

- ❌ None identified (changes are well-scoped and incremental)

---

## Testing Strategy

### Unit Tests (Recommended)

1. **Test `PantheonWarsUIService`**:
    - Network availability checking
    - Parameter validation
    - Error logging

2. **Test State Managers with Mock Service**:
    - Verify correct service calls
    - Verify state updates
    - Verify loading flag management

### Integration Tests (Essential)

1. Test each tab end-to-end:
    - Blessing unlock flow
    - Religion join/leave/create flow
    - Civilization create/invite/kick flow

2. Verify event handling still works:
    - Network events fire correctly
    - UI updates on events
    - Error states displayed properly

---

## Backward Compatibility

### Breaking Changes

- Constructor signatures for:
    - `GuiDialogManager`
    - `ReligionStateManager`
    - `BlessingStateManager`

### Non-Breaking Changes

- All other changes are internal refactoring
- UI behavior unchanged
- Network protocol unchanged
- Event subscriptions unchanged

### Migration Notes

- All breaking changes controlled in `GuiDialog.StartClientSide()`
- Single cutover point
- No need for dual patterns during migration

---

## Conclusion

Your codebase has **excellent bones** - the EDA pattern in Religion/Blessing tabs is well-designed. The issues
identified are primarily:

1. **Incomplete migration**: Civilization tab not yet extracted to state manager
2. **Missing abstraction**: No service layer between UI and network
3. **Boilerplate repetition**: Direct `GetModSystem` calls throughout

The recommended solution **completes your existing architectural vision** rather than adding new complexity. By creating
a thin service layer and extracting `CivilizationStateManager`, you'll achieve:

- ✅ 100% DRY compliance
- ✅ Clean architecture across all 3 tabs
- ✅ Testable UI logic
- ✅ Consistent patterns
- ✅ Easy to maintain and extend

**Estimated Effort**: 2-4 weeks for full implementation + testing
**Lines Changed**: ~500 lines across 10 files
**New Code**: ~490 lines (3 new files)
**Risk Level**: Low (incremental, well-scoped changes)
