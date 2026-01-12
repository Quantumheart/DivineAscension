# Deity Naming Feature - Task Breakdown

## Overview

This document contains the detailed task breakdown for implementing the `DeityType` → `DeityDomain` rename and required deity naming feature.

---

## Phase 1: Rename DeityType to DeityDomain

### Task 1.1: Rename Enum File and Update Type/Values

**Files:** 1
- `DivineAscension/Models/Enum/DeityType.cs` → `DeityDomain.cs`

**Changes:**
- Rename file from `DeityType.cs` to `DeityDomain.cs`
- Rename enum from `DeityType` to `DeityDomain`
- Rename values:
  - `Khoras` → `Craft`
  - `Lysa` → `Wild`
  - `Aethra` → `Harvest`
  - `Gaia` → `Stone`
- Keep integer values unchanged (0, 1, 2, 4, 7)
- Update XML documentation

---

### Task 1.2: Update Core Data Models

**Files:** ~5
- `DivineAscension/Data/ReligionData.cs`
- `DivineAscension/Models/Deity.cs`
- `DivineAscension/Models/Blessing.cs`
- `DivineAscension/Network/PlayerDataPacket.cs`
- `DivineAscension/Systems/PlayerProgressionData.cs`

**Changes:**
- Replace `DeityType` with `DeityDomain`
- Update property names where appropriate
- Ensure ProtoMember field numbers remain unchanged

---

### Task 1.3: Update System Interfaces

**Files:** ~8
- `DivineAscension/Systems/Interfaces/IDeityRegistry.cs`
- `DivineAscension/Systems/Interfaces/IReligionManager.cs`
- `DivineAscension/Systems/Interfaces/IPlayerProgressionDataManager.cs`
- `DivineAscension/Systems/Interfaces/IBlessingRegistry.cs`
- `DivineAscension/Systems/Interfaces/IFavorSystem.cs`
- `DivineAscension/Systems/Interfaces/IBlessingEffectSystem.cs`
- `DivineAscension/Systems/Favor/IFavorTracker.cs`
- `DivineAscension/Systems/Interfaces/IReligionPrestigeManager.cs`

**Changes:**
- Replace `DeityType` with `DeityDomain` in all method signatures
- Update parameter names and return types

---

### Task 1.4: Update System Implementations

**Files:** ~20
- `DivineAscension/Systems/DeityRegistry.cs`
- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Systems/BlessingRegistry.cs`
- `DivineAscension/Systems/BlessingDefinitions.cs` (heavy usage)
- `DivineAscension/Systems/FavorSystem.cs`
- `DivineAscension/Systems/PlayerProgressionDataManager.cs`
- `DivineAscension/Systems/BlessingEffectSystem.cs`
- `DivineAscension/Systems/PvPManager.cs`
- `DivineAscension/Systems/ReligionPrestigeManager.cs`
- `DivineAscension/Systems/Favor/MiningFavorTracker.cs`
- `DivineAscension/Systems/Favor/AnvilFavorTracker.cs`
- `DivineAscension/Systems/Favor/HuntingFavorTracker.cs`
- `DivineAscension/Systems/Favor/ForagingFavorTracker.cs`
- `DivineAscension/Systems/Favor/AethraFavorTracker.cs`
- `DivineAscension/Systems/Favor/GaiaFavorTracker.cs`
- `DivineAscension/Systems/Favor/SmeltingFavorTracker.cs`
- `DivineAscension/Systems/BlessingEffects/Handlers/*.cs`

**Changes:**
- Replace all `DeityType` references with `DeityDomain`
- Update enum value references (Khoras→Craft, etc.)

---

### Task 1.5: Update Commands

**Files:** ~5
- `DivineAscension/Commands/ReligionCommands.cs`
- `DivineAscension/Commands/BlessingCommands.cs`
- `DivineAscension/Commands/CivilizationCommands.cs`
- `DivineAscension/Commands/FavorCommands.cs`
- `DivineAscension/Commands/CommandHelpers.cs`

**Changes:**
- Replace `Enum.TryParse<DeityType>` with `Enum.TryParse<DeityDomain>`
- Update command help text
- Update validation messages

---

### Task 1.6: Update Network Handlers

**Files:** ~4
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/BlessingNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/PlayerDataNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs`

**Changes:**
- Replace `DeityType` with `DeityDomain`
- Update packet handling logic

---

### Task 1.7: Update GUI Utilities

**Files:** ~8
- `DivineAscension/GUI/UI/Utilities/DeityHelper.cs`
- `DivineAscension/GUI/UI/Utilities/DeityInfoHelper.cs`
- `DivineAscension/GUI/UI/Utilities/DeityIconLoader.cs`
- `DivineAscension/GUI/UI/Utilities/BlessingIconLoader.cs`
- `DivineAscension/GUI/UI/Utilities/GuiIconLoader.cs`
- `DivineAscension/Extensions/EnumLocalizationExtensions.cs`
- `DivineAscension/Services/LocalizationService.cs`

**Changes:**
- Replace `DeityType` with `DeityDomain`
- Update helper methods and color mappings
- Update icon path mappings

---

### Task 1.8: Update GUI State and Renderers

**Files:** ~15
- `DivineAscension/GUI/State/Religion/CreateState.cs`
- `DivineAscension/GUI/State/Religion/InfoState.cs`
- `DivineAscension/GUI/State/Religion/BrowseState.cs`
- `DivineAscension/GUI/State/Blessing/BlessingTabState.cs`
- `DivineAscension/GUI/Models/Religion/*.cs`
- `DivineAscension/GUI/UI/Renderers/Religion/*.cs`
- `DivineAscension/GUI/UI/Renderers/Blessing/*.cs`
- `DivineAscension/GUI/UI/Renderers/Civilization/*.cs`
- `DivineAscension/GUI/Managers/*.cs`

**Changes:**
- Replace `DeityType` with `DeityDomain`
- Update view models and state management

---

### Task 1.9: Update Localization Files

**Files:** 5
- `DivineAscension/assets/divineascension/lang/en.json`
- `DivineAscension/assets/divineascension/lang/fr.json`
- `DivineAscension/assets/divineascension/lang/de.json`
- `DivineAscension/assets/divineascension/lang/es.json`
- `DivineAscension/assets/divineascension/lang/ru.json`

**Changes:**
- Add localization keys for new domain names (Craft, Wild, Harvest, Stone)
- Update any references to old enum values
- Keep deity name translations (Khoras, Lysa, etc.) for default deity names

---

### Task 1.10: Update Test Files

**Files:** ~54
- `DivineAscension.Tests/Helpers/TestFixtures.cs`
- `DivineAscension.Tests/Commands/Helpers/*.cs`
- `DivineAscension.Tests/Systems/*.cs`
- `DivineAscension.Tests/Data/*.cs`
- `DivineAscension.Tests/GUI/**/*.cs`
- All other test files referencing DeityType

**Changes:**
- Global find-replace `DeityType` → `DeityDomain`
- Update enum value references
- Update test assertions and setup code

---

### Task 1.11: Build and Test Verification

**Actions:**
- Run `dotnet build DivineAscension.sln`
- Run `dotnet test`
- Fix any compilation errors
- Fix any test failures
- Commit Phase 1 changes

---

## Phase 2: Add Required DeityName Field

### Task 2.1: Add DeityName to ReligionData Model

**Files:** 1
- `DivineAscension/Data/ReligionData.cs`

**Changes:**
```csharp
/// <summary>
/// The name of the deity this religion worships (required).
/// </summary>
[ProtoMember(18)]
public string DeityName { get; set; } = string.Empty;
```

Update constructor to require deity name parameter.

---

### Task 2.2: Update IReligionManager and ReligionManager

**Files:** 2
- `DivineAscension/Systems/Interfaces/IReligionManager.cs`
- `DivineAscension/Systems/ReligionManager.cs`

**Changes:**
- Add `string deityName` parameter to `CreateReligion()` method
- Update method signature in interface
- Pass deity name to `ReligionData` constructor
- Add validation for deity name (required, length, characters)

---

### Task 2.3: Update Network Packets

**Files:** ~8

- `DivineAscension/Network/CreateReligionRequestPacket.cs` - Client sends deity name
- `DivineAscension/Network/CreateReligionResponsePacket.cs` - Server confirms creation
- `DivineAscension/Network/ReligionDetailResponsePacket.cs` - Full religion info for detail view
- `DivineAscension/Network/ReligionListResponsePacket.cs` - Religion list for browse view
- `DivineAscension/Network/PlayerReligionDataPacket.cs` - Player's current religion info
- `DivineAscension/Network/ReligionStateChangedPacket.cs` - Broadcast on state changes
- `DivineAscension/Network/SetDeityNameRequestPacket.cs` - **(new)** Client requests deity name change
- `DivineAscension/Network/SetDeityNameResponsePacket.cs` - **(new)** Server confirms/rejects change

**Add DeityName to existing packets:**
```csharp
[ProtoMember(X)]
public string DeityName { get; set; } = string.Empty;
```

**New SetDeityName packets:**

```csharp
// Request packet
[ProtoContract]
public class SetDeityNameRequestPacket
{
    [ProtoMember(1)]
    public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string NewDeityName { get; set; } = string.Empty;
}

// Response packet
[ProtoContract]
public class SetDeityNameResponsePacket
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string? ErrorMessage { get; set; }

    [ProtoMember(3)]
    public string? NewDeityName { get; set; }  // Confirmed name on success
}
```

---

### Task 2.4: Update ReligionNetworkHandler

**Files:** 2
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs`

**Changes:**
- Extract `DeityName` from `CreateReligionRequestPacket`
- Validate deity name
- Pass to `ReligionManager.CreateReligion()`
- Include `DeityName` in response packets

**Add SetDeityName handler:**

```csharp
// Server-side handler
private void HandleSetDeityNameRequest(IServerPlayer player, SetDeityNameRequestPacket packet)
{
    // Validate player is founder of the religion
    // Validate deity name
    // Call ReligionManager.SetDeityName()
    // Send response packet
    // Broadcast ReligionStateChangedPacket to all online members
}
```

**Client-side:**

- Register `SetDeityNameResponsePacket` handler
- Raise event for UI to update (e.g., `OnDeityNameChanged`)

---

### Task 2.5: Update /religion create Command

**Files:** 1
- `DivineAscension/Commands/ReligionCommands.cs`

**Changes:**
- Add required `deityname` parameter
- Update syntax: `/religion create "Religion Name" <domain> "Deity Name" [public|private]`
- Add validation for deity name
- Update help text

---

### Task 2.6: Add Deity Name Input to Create Religion UI

**Files:** ~5
- `DivineAscension/GUI/State/Religion/CreateState.cs`
  - Add `DeityName` property
- `DivineAscension/GUI/Models/Religion/Create/ReligionCreateViewModel.cs`
  - Add `DeityName` property
  - Add `IsDeityNameValid` property
- `DivineAscension/GUI/UI/Renderers/Religion/ReligionCreateRenderer.cs`
  - Add text input field for deity name
  - Add validation feedback
- `DivineAscension/GUI/Events/Religion/CreateEvent.cs`
  - Add `DeityNameChanged` event (if needed)
- `DivineAscension/GUI/Managers/ReligionTabManager.cs`
  - Handle deity name in create flow

**UI Requirements:**
- Text input field labeled "Deity Name (required)"
- Helper text: "Name the deity your religion worships"
- Validation: 2-48 characters, letters/spaces/apostrophes/hyphens
- Disable "Create" button if deity name is empty/invalid

---

### Task 2.7: Update Display Logic in DeityHelper

**Files:** 2
- `DivineAscension/GUI/UI/Utilities/DeityHelper.cs`
- `DivineAscension/Extensions/EnumLocalizationExtensions.cs`

**Changes:**
```csharp
/// <summary>
/// Gets the display name for a religion's deity.
/// Returns the custom deity name from the religion.
/// </summary>
public static string GetDeityDisplayName(ReligionData religion)
{
    return religion.DeityName;
}

/// <summary>
/// Gets the domain display name (Craft, Wild, Harvest, Stone).
/// </summary>
public static string GetDomainDisplayName(DeityDomain domain)
{
    return domain.ToLocalizedString();
}
```

---

### Task 2.8: Update Religion Info Displays

**Files:** ~10
- `DivineAscension/GUI/UI/Renderers/Religion/ReligionDetailRenderer.cs`
- `DivineAscension/GUI/UI/Renderers/Religion/ReligionInfoRenderer.cs`
- `DivineAscension/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs`
- `DivineAscension/GUI/UI/Renderers/Civilization/CivilizationDetailRenderer.cs`
- `DivineAscension/GUI/State/Religion/InfoState.cs`
- `DivineAscension/GUI/Managers/ReligionStateManager.cs`
- `DivineAscension/Commands/ReligionCommands.cs` (info command output)

**Changes:**
- Display deity name prominently
- Show domain as secondary information
- Format: "Deity: [DeityName] (Domain: [Domain])"

**Founder Edit Control (ReligionInfoRenderer):**

- Add inline edit button next to deity name (visible only to founder)
- On click: Show text input field with current name pre-filled
- Save button validates and sends `SetDeityNameRequestPacket`
- Cancel button reverts to display mode
- Show loading state while request is in flight
- Display success/error feedback

```
┌─────────────────────────────────────────────────┐
│ Religion Info                                   │
├─────────────────────────────────────────────────┤
│ Deity: Khoras the Eternal Smith  [✏️ Edit]     │  ← Founder sees edit button
│ Domain: Craft                                   │
│ ...                                             │
└─────────────────────────────────────────────────┘

Edit mode:
┌─────────────────────────────────────────────────┐
│ Deity Name                                      │
│ ┌─────────────────────────────────────────────┐ │
│ │ Khoras the Eternal Smith                    │ │
│ └─────────────────────────────────────────────┘ │
│ [Save] [Cancel]                                 │
└─────────────────────────────────────────────────┘
```

**State Management:**

- Add `IsEditingDeityName` flag to `InfoState`
- Add `EditDeityNameValue` string to `InfoState`
- Handle edit mode toggle, validation, and submission

---

### Task 2.9: Add Localization Keys for Deity Name UI

**Files:** 5
- `DivineAscension/assets/divineascension/lang/en.json`
- `DivineAscension/assets/divineascension/lang/fr.json`
- `DivineAscension/assets/divineascension/lang/de.json`
- `DivineAscension/assets/divineascension/lang/es.json`
- `DivineAscension/assets/divineascension/lang/ru.json`

**New Keys:**
```json
{
  "divineascension:ui-religion-deity-name-label": "Deity Name",
  "divineascension:ui-religion-deity-name-required": "Deity Name (required)",
  "divineascension:ui-religion-deity-name-hint": "Name the deity your religion worships",
  "divineascension:ui-religion-deity-name-invalid": "Deity name must be 2-48 characters",
  "divineascension:ui-religion-deity-name-edit": "Edit",
  "divineascension:ui-religion-deity-name-save": "Save",
  "divineascension:ui-religion-deity-name-cancel": "Cancel",
  "divineascension:ui-religion-deity-name-saving": "Saving...",
  "divineascension:ui-religion-deity-name-updated": "Deity name updated successfully",
  "divineascension:ui-religion-deity-name-error": "Failed to update deity name: {0}",
  "divineascension:domain-craft": "Craft",
  "divineascension:domain-wild": "Wild",
  "divineascension:domain-harvest": "Harvest",
  "divineascension:domain-stone": "Stone"
}
```

---

### Task 2.10: Update Tests for Required Deity Name

**Files:** ~18
- `DivineAscension.Tests/Data/ReligionDataTests.cs`
- `DivineAscension.Tests/Systems/ReligionManagerTests.cs`
- `DivineAscension.Tests/Commands/Religion/ReligionCommandCreateTests.cs`
- `DivineAscension.Tests/Commands/Religion/ReligionCommandInfoTests.cs`
- `DivineAscension.Tests/Commands/Religion/ReligionCommandListTests.cs`
- `DivineAscension.Tests/Commands/Religion/ReligionCommandAdminTests.cs`
- `DivineAscension.Tests/Commands/Helpers/ReligionCommandsTestHelpers.cs`
- `DivineAscension.Tests/Commands/Helpers/CivilizationCommandsTestHelpers.cs`
- `DivineAscension.Tests/Network/CreateReligionRequestPacketTests.cs`
- `DivineAscension.Tests/Network/CreateReligionResponsePacketTests.cs`
- `DivineAscension.Tests/Systems/CivilizationManagerTests.cs`
- `DivineAscension.Tests/GUI/State/Religion/CreateStateTests.cs`
- `DivineAscension.Tests/Helpers/TestFixtures.cs`
- Additional test files that use `CreateReligion()` helper methods

**New Tests:**
- Religion creation requires deity name
- Deity name validation (length, characters)
- Empty deity name rejected
- Deity name displayed correctly in UI
- Network packet includes deity name
- CreateReligion helper methods updated with deity name parameter

---

### Task 2.11: Implement Migration for Existing Religions

**Files:** ~4

- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Systems/Interfaces/IReligionManager.cs`
- `DivineAscension/GUI/UI/Utilities/DeityHelper.cs`

**Changes:**

Add migration logic to auto-populate `DeityName` for existing religions:

```csharp
/// <summary>
/// Migrates existing religions that have empty DeityName fields.
/// Called on world load after religions are loaded from save data.
/// </summary>
public void MigrateEmptyDeityNames()
{
    foreach (var religion in GetAllReligions())
    {
        if (string.IsNullOrEmpty(religion.DeityName))
        {
            religion.DeityName = GetLegacyDeityName(religion.Domain);
            MarkDirty(); // Flag for save
        }
    }
}

private static string GetLegacyDeityName(DeityDomain domain) => domain switch
{
    DeityDomain.Craft => "Khoras",
    DeityDomain.Wild => "Lysa",
    DeityDomain.Harvest => "Aethra",
    DeityDomain.Stone => "Gaia",
    _ => "Unknown Deity"
};
```

**Integration:**

- Call `MigrateEmptyDeityNames()` in `DivineAscensionSystemInitializer` after `ReligionManager` loads data
- Run before any network handlers are initialized

---

### Task 2.12: Add /religion setdeityname Commands

**Files:** ~3

- `DivineAscension/Commands/ReligionCommands.cs`
- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Systems/Interfaces/IReligionManager.cs`

**Command Syntax:**

```bash
# Founder command (changes own religion's deity name)
/religion setdeityname "New Deity Name"

# Admin command (changes any religion's deity name)
/religion admin setdeityname "Religion Name" "New Deity Name"
```

**Requirements:**

- Founder command: Founder-only permission for own religion
- Admin command: Requires server admin privilege
- Same validation as create (2-48 chars, letters/spaces/apostrophes/hyphens)
- Broadcasts update to all online members
- Updates network state

**Changes:**

```csharp
// In IReligionManager
bool SetDeityName(string religionUID, string deityName, out string error);

// In ReligionCommands
RegisterCommand("setdeityname", HandleSetDeityName);

// In admin subcommand group
RegisterAdminCommand("setdeityname", HandleAdminSetDeityName);
```

---

### Task 2.13: Add Founder Notification for Migrated Religions

**Files:** ~3

- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Systems/Networking/Server/PlayerDataNetworkHandler.cs`
- `DivineAscension/assets/divineascension/lang/en.json` (and other lang files)

**Changes:**

Track which religions were migrated and notify founders on first login:

```csharp
// Track migrated religion UIDs (cleared after founder is notified)
private HashSet<string> _migratedReligionUIDs = new();

// On player join, check if they're a founder of a migrated religion
public void NotifyFounderOfMigration(string playerUID)
{
    var religion = GetPlayerReligion(playerUID);
    if (religion != null &&
        religion.IsFounder(playerUID) &&
        _migratedReligionUIDs.Contains(religion.ReligionUID))
    {
        // Send notification message
        SendMessage(playerUID, Lang.Get("divineascension:migration-deity-name-notice",
            religion.DeityName));
        _migratedReligionUIDs.Remove(religion.ReligionUID);
    }
}
```

**Localization Key:**

```json
{
  "divineascension:migration-deity-name-notice": "Your religion's deity has been set to '{0}'. You can customize this with /religion setdeityname \"New Name\""
}
```

---

### Task 2.14: Update Tests for Migration

**Files:** ~6

- `DivineAscension.Tests/Systems/ReligionManagerTests.cs`
- `DivineAscension.Tests/Commands/Religion/ReligionCommandSetDeityNameTests.cs` (new)
- `DivineAscension.Tests/Commands/Religion/ReligionCommandAdminTests.cs`
- `DivineAscension.Tests/Helpers/TestFixtures.cs`

**New Tests:**

- Migration populates empty DeityName with legacy name
- Migration preserves existing DeityName values
- SetDeityName validates input correctly
- SetDeityName requires founder permission
- SetDeityName rejects non-founder members
- Admin SetDeityName works on any religion
- Admin SetDeityName requires admin privilege
- Founder notification sent on first login after migration
- Founder notification only sent once

---

### Task 2.15: Build and Test Verification

**Actions:**
- Run `dotnet build DivineAscension.sln`
- Run `dotnet test`
- Fix any compilation errors
- Fix any test failures
- Manual testing of:
  - New religion creation with deity name
  - Migration of existing save data
  - `/religion setdeityname` command
  - `/religion admin setdeityname` command
  - UI edit flow in ReligionInfoRenderer (founder only)
  - Founder notification on login
- Commit Phase 2 changes

---

## Summary

| Phase     | Tasks        | Estimated Files |
|-----------|--------------|-----------------|
| Phase 1   | 11 tasks     | ~117 files      |
| Phase 2   | 15 tasks     | ~50 files       |
| **Total** | **26 tasks** | **~165 files**  |

## Commit Strategy

1. **Phase 1 Commit:** `refactor: rename DeityType to DeityDomain with domain-based values`
2. **Phase 2 Commit:** `feat: add required deity name field to religions`
3. **Phase 2 Commit:** `feat: add migration for existing religions and setdeityname command`

Or combine into a single feature branch with multiple commits for reviewability.
