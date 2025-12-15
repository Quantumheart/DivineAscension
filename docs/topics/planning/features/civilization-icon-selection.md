# Feature Plan: Civilization Icon Selection

## Overview

Enable users to select a custom icon when creating or updating their civilization. Icons will be displayed throughout
the UI alongside civilization names.

## Current State Analysis

- **No icon support exists**: `Civilization` class only stores `CivId`, `Name`, `FounderUID`, etc.
- **No update mechanism**: Only creation exists (`CreateCivilization`), no edit/update flow
- **Icon infrastructure exists**: `DeityIconLoader` pattern can be adapted for civilization icons
- **Asset location**: Icons would go in `PantheonWars/assets/pantheonwars/textures/icons/civilizations/`

## Implementation Plan

### Phase 1: Data Layer (Backend)

**Files to modify:**

- `PantheonWars/Data/CivilizationData.cs`
    - Add `[ProtoMember(9)] public string Icon { get; set; } = "default";`
    - Add `UpdateIcon(string icon)` method

- `PantheonWars/Systems/CivilizationManager.cs`
    - Update `CreateCivilization()` to accept optional `icon` parameter
    - Add new `UpdateCivilizationIcon(string civId, string requestorUID, string icon)` method
    - Validate icon exists in assets
    - Validate requestor is civilization founder

### Phase 2: Network Layer

**Files to modify:**

- `PantheonWars/Network/Civilization/CivilizationActionRequestPacket.cs` (if not exists, create)
    - Add support for "UpdateIcon" action type
    - Include icon string in packet

- `PantheonWars/Network/Civilization/CivilizationInfoResponsePacket.cs`
    - Add `Icon` field to response data

- `PantheonWars/Systems/Networking/Server/CivilizationNetworkHandler.cs`
    - Add handler for UpdateIcon requests
    - Return updated civilization data

### Phase 3: Asset Management

**Files to create:**

- `PantheonWars/GUI/UI/Utilities/CivilizationIconLoader.cs`
    - Mirror `DeityIconLoader` pattern
    - Load from `pantheonwars:textures/icons/civilizations/{iconname}.png`
    - Cache loaded textures
    - Provide `GetIconTextureId(string iconName)` method

**Assets to add:**

- `PantheonWars/assets/pantheonwars/textures/icons/civilizations/`
    - Create default set of 8-12 civilization icons (32x32 PNG)
    - Examples: shield, banner, castle, crown, sword, torch, eagle, lion, etc.
    - `default.png` as fallback

### Phase 4: UI Components

**Files to create:**

- `PantheonWars/GUI/UI/Components/IconPicker.cs`
    - Reusable component for icon selection
    - Display grid of available icons (4x3 layout)
    - Highlight selected icon
    - Return selected icon name
    - Parameters: `availableIcons`, `selectedIcon`, `x`, `y`, `width`

### Phase 5: Create Flow

**Files to modify:**

- `PantheonWars/GUI/State/Civilization/CreateState.cs`
    - Add `public string SelectedIcon { get; set; } = "default";`

- `PantheonWars/GUI/Models/Civilization/Create/CivilizationCreateViewModel.cs`
    - Add `string SelectedIcon` property

- `PantheonWars/GUI/Events/Civilization/CreateEvent.cs`
    - Add `IconSelected(string icon)` event

- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationCreateRenderer.cs`
    - Add icon picker section between name input and create button
    - Render available icons using `IconPicker` component
    - Show preview of selected icon

- `PantheonWars/GUI/Managers/CivilizationStateManager.cs`
    - Handle `IconSelected` event
    - Include icon in create request packet

### Phase 6: Update/Edit Flow

**Files to create:**

- `PantheonWars/GUI/State/Civilization/EditState.cs`
    - Store edit form state (name, icon)

- `PantheonWars/GUI/Models/Civilization/Edit/CivilizationEditViewModel.cs`
    - View model for edit dialog

- `PantheonWars/GUI/Events/Civilization/EditEvent.cs`
    - Events: `IconSelected`, `SubmitClicked`, `CancelClicked`

- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationEditRenderer.cs`
    - Similar to create renderer
    - Pre-populate with current icon

**Files to modify:**

- `PantheonWars/GUI/Managers/CivilizationStateManager.cs`
    - Add edit state management
    - Add `OpenEditDialog()`, `CloseEditDialog()` methods
    - Send update requests to server

### Phase 7: Display Icons Throughout UI

**Files to modify:**

- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationInfoRenderer.cs`
    - Display icon next to civilization name in header (line 46)
    - Use `CivilizationIconLoader.GetIconTextureId(vm.Icon)`

- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationBrowseRenderer.cs`
    - Show icon in civilization list cards

- `PantheonWars/GUI/UI/Renderers/Components/ReligionListRenderer.cs`
    - If showing civilization info, include icon

### Phase 8: Testing

**Files to create:**

- `PantheonWars.Tests/Systems/CivilizationManagerIconTests.cs`
    - Test icon validation
    - Test update authorization
    - Test default icon fallback

- `PantheonWars.Tests/GUI/Components/IconPickerTests.cs`
    - Test icon selection logic
    - Test rendering without crashes

## Success Criteria

1. Users can select from 8+ icons when creating civilization
2. Founders can update their civilization icon via edit dialog
3. Icons display consistently across all civilization UI views
4. Invalid icons fall back to "default.png" gracefully
5. Only civilization founders can update icons
6. Icons persist across server restarts

## Technical Notes

- Icon files should be 32x32 PNG with transparency
- Use same asset loading pattern as deity icons (proven reliable)
- Icon names should be lowercase alphanumeric + underscores
- Default icon required as fallback for missing/corrupted assets

## Implementation Order

1. **Phase 1** (Data Layer) - Foundation for storing icon data
2. **Phase 3** (Asset Management) - Need icon loader before UI work
3. **Phase 2** (Network Layer) - Communication layer for icon data
4. **Phase 4** (UI Components) - Reusable icon picker component
5. **Phase 5** (Create Flow) - Allow selection during creation
6. **Phase 7** (Display Icons) - Show icons in existing views
7. **Phase 6** (Update/Edit Flow) - Allow changing icons post-creation
8. **Phase 8** (Testing) - Comprehensive test coverage

## Future Enhancements

- Custom icon upload (requires file transfer system)
- Icon categories/themes (military, religious, nature, etc.)
- Animated icons or effects
- Icon unlock system tied to civilization achievements
- AI-generated custom icons based on civilization name
