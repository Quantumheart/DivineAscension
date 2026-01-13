# Implementation Plan: Add Description to Civilization (Issue #81)

## Overview

The civilization system currently lacks a description field. This feature will add:
- A `Description` field to the civilization data model
- Create tab UI for setting description during civilization creation
- Info tab UI for editing/viewing description (founder editable, others read-only)
- Network packets and handlers for description updates
- Profanity filter integration
- Command-line support for setting descriptions

## Acceptance Criteria (from Issue #81)

1. **Edit Access:** Description editing available in both info and create tabs
2. **User Input:** Support copy and paste functionality
3. **Content Filtering:** Integrate ProfanityFilterService to block inappropriate words (when enabled)
4. **Permissions:** Only civilization founders may update descriptions
5. **Display:** Descriptions render on both civilization detail and info renderers

---

## Phase 1: Data Model Updates

**File:** `DivineAscension/Data/CivilizationData.cs`

Add a new `Description` property with ProtoBuf serialization:
```csharp
/// <summary>
/// Civilization description or manifesto set by the founder.
/// </summary>
[ProtoMember(10)]
public string Description { get; set; } = string.Empty;
```

**Considerations:**
- Must assign a new unique ProtoMember number (10, since existing members go up to 9)
- Default to empty string for backward compatibility with existing data

---

## Phase 2: Manager Updates

**File:** `DivineAscension/Systems/CivilizationManager.cs`

1. **Modify `CreateCivilization` method** to accept an optional description parameter:
   - Add `string description = ""` parameter
   - Include profanity validation
   - Set description on new civilization

2. **Add new `UpdateCivilizationDescription` method**:
   - Validate founder permission (only founder can edit)
   - Validate description length (max 200 characters, matching religion pattern)
   - Integrate profanity filter check
   - Update and persist description
   - Return success/error result

**Pattern to follow:** Mirror `UpdateCivilizationIcon` method (lines 671-707) which handles:
- Permission check (`if (civ.FounderUID != requestorUID)`)
- Validation
- Update and save

---

## Phase 3: Network Layer Updates

**Extend Existing Packet**

**File:** `DivineAscension/Network/Civilization/CivilizationActionRequestPacket.cs`

Add description field:
```csharp
[ProtoMember(6)]
public string Description { get; set; } = string.Empty;
```

Add new action type `"setdescription"` to support description updates.

**File:** `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs`

Add handler case for `"setdescription"` action:
- Extract description from packet
- Call `CivilizationManager.UpdateCivilizationDescription`
- Return appropriate response

**Files to update for sync:**
- `CivilizationListResponsePacket.cs` - Ensure description is included in list data
- `CivilizationInfoResponsePacket.cs` - Include description in info responses (if separate from list)

---

## Phase 4: Command Support

**File:** `DivineAscension/Commands/CivilizationCommands.cs`

Add new subcommand for description:
```csharp
.BeginSubCommand("description")
    .WithArgs(_sapi.ChatCommands.Parsers.All("text"))
    .WithDescription("Set your civilization's description")
    .HandleWith(OnSetDescription)
.EndSubCommand()
```

Implement `OnSetDescription` handler:
- Get player's civilization
- Verify founder permission
- Validate description (length, profanity)
- Update via manager
- Return success/error message

**Pattern to follow:** `ReligionCommands.OnSetDescription` (lines 839-874)

---

## Phase 5: GUI - Create Tab

**File:** `DivineAscension/GUI/Models/Civilization/Create/CivilizationCreateViewModel.cs`

Add fields:
```csharp
public string Description { get; init; } = string.Empty;
public string? ProfanityMatchedWordInDescription { get; init; }
```

Update validation in `CanCreate` property to check description profanity.

Add helper method:
```csharp
public CivilizationCreateViewModel WithDescription(string desc) =>
    this with { Description = desc };
```

**File:** `DivineAscension/GUI/UI/Renderers/Civilization/CivilizationCreateRenderer.cs`

Add description input section after name input:
- Label: "Description (Optional)"
- Multi-line text input (80-100px height)
- Max length indicator
- Profanity validation feedback

**Pattern to follow:** Religion create renderer description section

---

## Phase 6: GUI - Info Tab

**File:** `DivineAscension/GUI/Models/Civilization/Info/CivilizationInfoViewModel.cs`

Add fields:
```csharp
public string Description { get; init; } = string.Empty;
public string DescriptionText { get; init; } = string.Empty;  // For editing state
```

Add helper methods:
```csharp
public bool HasDescriptionChanges() => Description != DescriptionText;

public CivilizationInfoViewModel WithDescriptionText(string text) =>
    this with { DescriptionText = text };
```

**File:** `DivineAscension/GUI/UI/Renderers/Civilization/CivilizationInfoRenderer.cs`

Add description section (insert after founder info, before member religions):
- **For founder (`IsFounder == true`):**
  - Editable multi-line text input
  - "Save Description" button (enabled when `HasDescriptionChanges()`)
  - Emit `InfoEvent.SaveDescriptionClicked` on save

- **For non-founders:**
  - Read-only text display
  - Show description if not empty, hide section if empty

**Pattern to follow:** `ReligionInfoDescriptionRenderer.cs` for the editable/read-only pattern

**File:** `DivineAscension/GUI/State/Civilization/CivilizationInfoState.cs` (or equivalent state manager)

Handle `SaveDescriptionClicked` event:
- Get current description from viewmodel
- Validate profanity client-side
- Send network packet to update description
- Update local state on success response

---

## Phase 7: Detail Renderer

**File:** `DivineAscension/GUI/UI/Renderers/Civilization/CivilizationDetailRenderer.cs` (if exists)

Add description display section:
- Show description if not empty
- Read-only display for browse/detail views

---

## Phase 8: Localization

**File:** `DivineAscension/Services/LocalizationKeys.cs`

Add new keys:
```csharp
public const string CivilizationDescriptionLabel = "civilization-description-label";
public const string CivilizationDescriptionPlaceholder = "civilization-description-placeholder";
public const string CivilizationDescriptionSaved = "civilization-description-saved";
public const string CivilizationDescriptionTooLong = "civilization-description-too-long";
public const string CivilizationDescriptionProfanity = "civilization-description-profanity";
```

**File:** `DivineAscension/assets/divineascension/lang/en.json`

Add translations:
```json
"civilization-description-label": "Description",
"civilization-description-placeholder": "Enter a description for your civilization...",
"civilization-description-saved": "Civilization description updated.",
"civilization-description-too-long": "Description must be 200 characters or less.",
"civilization-description-profanity": "Description contains inappropriate content."
```

---

## Phase 9: Testing

**New Test Files:**

1. `DivineAscension.Tests/Systems/CivilizationManagerDescriptionTests.cs`
   - Test `UpdateCivilizationDescription` permission checks
   - Test description length validation
   - Test profanity filter integration
   - Test successful update

2. `DivineAscension.Tests/GUI/Models/CivilizationCreateViewModelDescriptionTests.cs`
   - Test description field initialization
   - Test `CanCreate` with profanity in description

3. `DivineAscension.Tests/GUI/Models/CivilizationInfoViewModelDescriptionTests.cs`
   - Test `HasDescriptionChanges()`
   - Test `WithDescriptionText()`

---

## Implementation Order

| Step | Component | Priority | Dependencies |
|------|-----------|----------|--------------|
| 1 | Data Model (`CivilizationData.cs`) | Critical | None |
| 2 | Manager (`CivilizationManager.cs`) | Critical | Step 1 |
| 3 | Network Packets | Critical | Step 1 |
| 4 | Network Handler | Critical | Steps 2, 3 |
| 5 | Commands | High | Step 2 |
| 6 | Create ViewModel | High | None |
| 7 | Create Renderer | High | Step 6 |
| 8 | Info ViewModel | High | None |
| 9 | Info Renderer | High | Step 8 |
| 10 | Detail Renderer | Medium | Step 1 |
| 11 | Localization | Medium | None (parallel) |
| 12 | Tests | High | All steps |

---

## Acceptance Criteria Mapping

| Requirement | Implementation |
|-------------|----------------|
| Edit access in info tab | Phase 6: Info ViewModel + Renderer with editable field for founder |
| Edit access in create tab | Phase 5: Create ViewModel + Renderer with input field |
| Copy/paste support | Use `TextInput.DrawMultiline` which supports clipboard operations |
| Profanity filter integration | Phases 2, 4, 5, 6: ProfanityFilterService checks at all entry points |
| Founder-only editing | Phase 2: Permission check in manager; Phase 6: `IsFounder` check in UI |
| Display in detail/info | Phase 6, 7: Show in info tab and detail renderer |

---

## Risks and Mitigations

1. **Data Migration**: Existing civilizations will have empty descriptions. Mitigated by defaulting to empty string.

2. **Serialization Compatibility**: New ProtoBuf field must use unique number. Verified ProtoMember(10) is available.

3. **UI Layout**: Adding description section may affect layout. Follow existing religion pattern for consistent design.

4. **Network Protocol**: Adding field to existing packet maintains backward compatibility (new field defaults to empty).

---

## Files to Modify Summary

| Category | Files |
|----------|-------|
| Data Model | `DivineAscension/Data/CivilizationData.cs` |
| Manager | `DivineAscension/Systems/CivilizationManager.cs` |
| Network | `DivineAscension/Network/Civilization/CivilizationActionRequestPacket.cs`, `CivilizationNetworkHandler.cs` |
| Commands | `DivineAscension/Commands/CivilizationCommands.cs` |
| GUI Create | `CivilizationCreateViewModel.cs`, `CivilizationCreateRenderer.cs` |
| GUI Info | `CivilizationInfoViewModel.cs`, `CivilizationInfoRenderer.cs`, `CivilizationInfoState.cs` |
| GUI Detail | `CivilizationDetailRenderer.cs` |
| Localization | `LocalizationKeys.cs`, `en.json` |
| Tests | New test files in `DivineAscension.Tests/` |
