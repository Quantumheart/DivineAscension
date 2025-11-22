# Guild Management System Conversion Plan

## Overview
Convert PantheonWars from a complex religion/deity/blessing/PvP system into a streamlined guild/group management system.

## ⚠️ IMPORTANT UPDATE - ImGui UI Already Exists

**CRITICAL FINDING:** PantheonWars has **ALREADY IMPLEMENTED** a complete ImGui-based religion management UI that fully replaces the old GuiDialog system. The conversion is simpler than originally planned:

### Current State
- ✅ **BlessingDialog (ImGui)** - Fully functional, opens with P hotkey (will be renamed to GuildManagementDialog)
- ✅ **ReligionBrowserOverlay** - Browse/join religions with deity filtering (will be simplified)
- ✅ **CreateReligionOverlay** - Create religion form with deity dropdown (will be simplified)
- ✅ **ReligionManagementOverlay** - Manage members, kick/ban/invite/edit description/disband (will be simplified)
- ✅ **LeaveReligionConfirmOverlay** - Confirmation dialog for leaving religion
- ✅ **Network Integration** - All packets connected and working
- ✅ **Reusable Components** - ButtonRenderer, TextInput, Dropdown, TabControl, etc.

### Legacy Code (UNUSED)
- ❌ **ReligionManagementDialog** (old GuiDialog) - NOT instantiated, dead code
- ❌ **CreateReligionDialog** - Replaced by CreateReligionOverlay
- ❌ **InvitePlayerDialog** - Replaced by ReligionManagementOverlay
- ❌ **EditDescriptionDialog** - Replaced by ReligionManagementOverlay
- ❌ **BanPlayerDialog** - Replaced by ReligionManagementOverlay
- ❌ **DeitySelectionDialog** - Legacy Phase 1-2 system, unused

### Simplified Conversion Strategy
1. **Delete legacy GuiDialog classes** (simple file deletion)
2. **Rename BlessingDialog to GuildManagementDialog** (reflects actual purpose)
3. **Simplify existing ImGui overlays** (remove deity/rank/favor UI elements)
4. **Update data models** (remove deity/progression fields)
5. **Clean up system logic** (remove blessing/PvP/favor code)

No need to build new UI or register hotkeys - the modern ImGui system is already complete and operational.

## Current State Analysis

### Existing Features
- **Religion System**: Create, join, leave, manage religions with founder privileges
- **8 Deities**: Khoras, Lysa, Morthen, Aethra, Umbros, Tharos, Gaia, Vex
- **Blessing System**: 80 blessings (10 per deity) with stat modifiers
- **Dual Ranking System**:
  - Player Favor Ranks (Initiate → Avatar)
  - Religion Prestige Ranks (Fledgling → Mythic)
- **PvP System**: Favor/Prestige earning through combat
- **Ability System**: Legacy active abilities (Phase 1-2)
- **Complex GUI**: Multiple dialogs, HUD elements
- **Commands**: religion, blessing, deity, favor, ability

### Conversion Goals
1. **Simplify to guild management only** - No combat, no progression, no stats
2. **Keep only religion commands** - Remove blessing, deity, favor, ability commands
3. **Single keybind** - Press key to open ReligionManagement dialog
4. **Remove progression mechanics** - No ranks, favor, prestige, blessings
5. **Remove deity system entirely** - Guilds are just guilds, no themes or deities

---

## Scope Definition

### ✅ KEEP - Core Guild Management
- **ReligionManager** - Core guild management logic
- **PlayerReligionDataManager** - Player guild membership tracking
- **ReligionCommands** - All `/religion` commands
- **BlessingDialog (rename to GuildManagementDialog)** - Main ImGui-based guild management UI
- **ReligionBrowserOverlay** - Browse/join guilds (simplify to remove deity filtering)
- **CreateReligionOverlay** - Guild creation interface (simplify to remove deity selection)
- **ReligionManagementOverlay** - Manage members, kick/ban/invite/edit/disband
- **LeaveReligionConfirmOverlay** - Leave confirmation dialog
- **Guild Features**:
  - Create/join/leave guilds
  - Public/private visibility
  - Founder privileges (kick, ban, disband, edit description)
  - Member list
  - Ban system with expiry
  - Invitations

### ❌ REMOVE - All Progression & Combat Systems
- **Deity System**:
  - DeityRegistry
  - DeityCommands
  - Deity model
  - DeityType enum (or simplify to None only)
  - All deity selection UI
  - Deity filtering in religion lists
- **Blessing System**:
  - BlessingRegistry
  - BlessingEffectSystem
  - BlessingCommands
  - BlessingDefinitions
  - All blessing dialogs
  - Blessing data models
- **PvP System**:
  - PvPManager
  - Combat integration
  - Favor earning from kills
- **Favor System**:
  - FavorSystem
  - FavorCommands
  - Favor currency
- **Ranking Systems**:
  - FavorRank enum
  - PrestigeRank enum
  - ReligionPrestigeManager
  - Rank progression logic
- **Ability System** (Legacy):
  - AbilitySystem
  - AbilityRegistry
  - AbilityCommands
  - AbilityCooldownManager
  - All ability files
- **Buff System**:
  - BuffManager
  - EntityBehaviorBuffTracker
  - ActiveEffect
- **UI Elements**:
  - FavorHudElement
  - BlessingTreeLayout (blessing display components)
  - DeitySelectionDialog
- **Commands**:
  - DeityCommands
  - BlessingCommands
  - FavorCommands
  - AbilityCommands
- **Network Packets** (blessing/favor related):
  - BlessingUnlockRequestPacket
  - BlessingUnlockResponsePacket
  - BlessingDataRequestPacket
  - BlessingDataResponsePacket

---

## Implementation Plan

### Phase 1: Remove Blessing & Combat Systems
**Goal**: Strip out all blessing, PvP, and progression mechanics

#### 1.1 Remove Blessing System Files
- [ ] Delete `/Systems/BlessingRegistry.cs`
- [ ] Delete `/Systems/BlessingEffectSystem.cs`
- [ ] Delete `/Systems/BlessingDefinitions.cs`
- [ ] Delete `/Systems/BlessingDialogManager.cs`
- [ ] Delete `/Commands/BlessingCommands.cs`
- [ ] **KEEP** `/GUI/BlessingDialog.cs` ✅ (will be renamed to GuildManagementDialog.cs in Phase 4)
- [ ] Delete `/GUI/BlessingTreeLayout.cs`
- [ ] **KEEP** `/GUI/BlessingDialogManager.cs` ✅ (update to remove blessing-specific code)
- [ ] **KEEP** `/GUI/BlessingDialogEventHandlers.cs` ✅ (update to remove blessing-specific code)
- [ ] Delete `/Models/Blessing.cs`
- [ ] Delete `/Models/BlessingNodeState.cs`
- [ ] Delete `/Models/BlessingTooltipData.cs`
- [ ] Delete `/Constants/BlessingIds.cs`
- [ ] Delete `/Constants/BlessingCommandConstants.cs`
- [ ] Delete `/Network/BlessingUnlockRequestPacket.cs`
- [ ] Delete `/Network/BlessingUnlockResponsePacket.cs`
- [ ] Delete `/Network/BlessingDataRequestPacket.cs`
- [ ] Delete `/Network/BlessingDataResponsePacket.cs`

#### 1.2 Remove PvP & Favor Systems
- [ ] Delete `/Systems/PvPManager.cs`
- [ ] Delete `/Systems/FavorSystem.cs`
- [ ] Delete `/Systems/ReligionPrestigeManager.cs`
- [ ] Delete `/Commands/FavorCommands.cs`
- [ ] Delete `/Models/PlayerFavorProgress.cs`
- [ ] Delete `/Models/ReligionPrestigeProgress.cs`

#### 1.3 Remove Ability System (Legacy)
- [ ] Delete `/Systems/AbilitySystem.cs`
- [ ] Delete `/Systems/AbilityRegistry.cs`
- [ ] Delete `/Systems/AbilityCooldownManager.cs`
- [ ] Delete `/Commands/AbilityCommands.cs`
- [ ] Delete `/Models/Ability.cs`
- [ ] Delete `/Data/PlayerAbilityData.cs`
- [ ] Delete entire `/Abilities/` folder (legacy Phase 1-2)

#### 1.4 Remove Buff System
- [ ] Delete `/Systems/BuffSystem/BuffManager.cs`
- [ ] Delete `/Systems/BuffSystem/EntityBehaviorBuffTracker.cs`
- [ ] Delete `/Systems/BuffSystem/ActiveEffect.cs`
- [ ] Delete `/Systems/BuffSystem/Interfaces/` folder

#### 1.5 Remove Legacy GuiDialog Classes
**Note:** BlessingDialog.cs is **ImGui-based** and is the main guild management UI. It will be renamed to GuildManagementDialog.cs in Phase 4, not deleted.

- [ ] Delete `/GUI/FavorHudElement.cs` (already obsolete)
- [ ] Delete `/GUI/DeitySelectionDialog.cs` (legacy, unused)
- [ ] Delete `/GUI/ReligionManagementDialog.cs` (old GuiDialog, replaced by ImGui overlays)
- [ ] Delete `/GUI/CreateReligionDialog.cs` (old GuiDialog, replaced by CreateReligionOverlay)
- [ ] Delete `/GUI/InvitePlayerDialog.cs` (old GuiDialog, replaced by ReligionManagementOverlay)
- [ ] Delete `/GUI/EditDescriptionDialog.cs` (old GuiDialog, replaced by ReligionManagementOverlay)
- [ ] Delete `/GUI/BanPlayerDialog.cs` (old GuiDialog, replaced by ReligionManagementOverlay)
- [ ] **KEEP** `/GUI/BlessingDialog.cs` ✅ (ImGui-based, will be renamed to GuildManagementDialog.cs)
- [ ] **KEEP** `/GUI/OverlayCoordinator.cs` ✅ (actively used by BlessingDialog/GuildManagementDialog)

#### 1.6 Remove Deity System
- [ ] Delete `/Systems/DeityRegistry.cs`
- [ ] Delete `/Models/Deity.cs`
- [ ] Delete `/Commands/DeityCommands.cs`
- [ ] Update `/Models/Enum/DeityType.cs` (simplify or remove entirely)
- [ ] Delete `/GUI/UI/Utilities/DeityHelper.cs` (deity-only utility, not needed)
- [ ] Delete `/GUI/UI/Utilities/DeityIconLoader.cs` (deity-only utility, not needed)

### Phase 2: Simplify Data Models
**Goal**: Remove progression data, keep only guild membership info

#### 2.1 Update ReligionData
```csharp
// Remove:
- Prestige (int)
- TotalPrestige (int)
- PrestigeRank (PrestigeRank enum)
- UnlockedBlessings (Dictionary)
- Deity (DeityType) ❌ REMOVE ENTIRELY

// Keep:
- ReligionUID
- ReligionName
- FounderUID
- MemberUIDs
- IsPublic
- Description
- CreationDate
- Invitations
- BannedPlayers
```

#### 2.2 Update PlayerReligionData
```csharp
// Remove:
- Favor (int)
- TotalFavorEarned (int)
- FavorRank (FavorRank enum)
- UnlockedBlessings (Dictionary)
- PassiveFavorAccrued
- LastPassiveFavorUpdate
- ActiveDeity (DeityType) ❌ REMOVE ENTIRELY

// Keep:
- PlayerUID
- ReligionUID
- LastReligionSwitchDate (for cooldown)
- TotalReligionSwitches
```

#### 2.3 Simplify Network Packets
- [ ] Update `PlayerReligionDataPacket` (remove favor/rank/deity fields)
- [ ] Update `ReligionListResponsePacket` (remove prestige/rank/deity fields)
- [ ] Update `PlayerReligionInfoResponsePacket` (remove favor/prestige/rank/deity fields)
- [ ] Update `CreateReligionRequestPacket` (remove deity parameter)
- [ ] Update `CreateReligionResponsePacket` (remove deity references)
- [ ] Remove blessing-related packets (already deleted)

#### 2.4 Update Persistence
- [ ] Update save/load logic in `ReligionManager`
- [ ] Update save/load logic in `PlayerReligionDataManager`
- [ ] Remove rank/favor/prestige from saved data

### Phase 3: Update Core Systems
**Goal**: Simplify managers to handle only guild membership

#### 3.1 Simplify ReligionManager
- [ ] Remove prestige calculation methods
- [ ] Remove rank progression logic
- [ ] Remove blessing unlock notifications
- [ ] Keep: Create, join, leave, invite, kick, ban, disband

#### 3.2 Simplify PlayerReligionDataManager
- [ ] Remove favor earning methods
- [ ] Remove rank progression
- [ ] Remove blessing tracking
- [ ] Keep: Join/leave religion, switching cooldown

#### 3.3 Update PantheonWarsSystem.cs
- [ ] Remove blessing system initialization
- [ ] Remove PvP manager initialization
- [ ] Remove favor system initialization
- [ ] Remove ability system initialization
- [ ] Remove buff manager initialization
- [ ] Remove deity registry initialization ❌ REMOVE
- [ ] Remove deity/ability/blessing/favor command registration
- [ ] Keep only religion command registration
- [ ] Remove blessing-related network handlers
- [ ] Remove buff entity behavior registration
- [ ] Simplify OnPlayerJoin (no favor/rank/deity data needed)

#### 3.4 Update ReligionCommands
- [ ] Update `/religion create` command:
  - **OLD**: `/religion create <name> <deity> [public/private]`
  - **NEW**: `/religion create <name> [public/private]` ❌ REMOVE deity parameter
- [ ] Update `/religion list` command:
  - **OLD**: `/religion list [deity]`
  - **NEW**: `/religion list` ❌ REMOVE deity filtering
- [ ] Keep these commands unchanged:
  - `/religion join <name>`
  - `/religion leave`
  - `/religion info [name]`
  - `/religion members`
  - `/religion invite <playername>`
  - `/religion kick <playername>`
  - `/religion ban <playername> [reason] [days]`
  - `/religion unban <playername>`
  - `/religion banlist`
  - `/religion disband`
  - `/religion description <text>`

### Phase 4: Update GUI
**Goal**: Rename BlessingDialog and simplify existing ImGui UI to remove deity/progression elements

#### 4.1 Rename BlessingDialog to GuildManagementDialog
- [ ] Rename `/GUI/BlessingDialog.cs` to `/GUI/GuildManagementDialog.cs`
- [ ] Update class name from `BlessingDialog` to `GuildManagementDialog`
- [ ] Update all references to `BlessingDialog` in:
  - `PantheonWarsSystem.cs` (client initialization)
  - `BlessingDialogManager.cs` (rename to GuildDialogManager.cs)
  - `BlessingDialogEventHandlers.cs` (rename to GuildDialogEventHandlers.cs)
  - `OverlayCoordinator.cs`
- [ ] Remove blessing-related event handlers (BlessingUnlocked, BlessingDataReceived)
- [ ] Update file header comments to reflect guild management purpose
- [ ] Keep P hotkey keybind registration (already functional)

#### 4.2 Clean Up Dead Code in PantheonWarsSystem
- [ ] **REMOVE** unused `_religionDialog` field from PantheonWarsSystem (old GuiDialog reference)
- [ ] **REMOVE** any commented or dead hotkey registration code for old GuiDialog classes

**Current State:**
The ImGui-based GuildManagementDialog (P key) provides full guild management through overlays. Users access religion features via:
- **"Change Religion"** button → ReligionBrowserOverlay (browse/join/create)
- **"Manage Religion"** button → ReligionManagementOverlay (kick/ban/invite/edit/disband)
- **"Leave Religion"** button → LeaveReligionConfirmOverlay

No additional keybind registration needed.

#### 4.3 Simplify ImGui Religion Overlays
- [ ] **Update ReligionBrowserOverlay:**
  - Remove deity filter tabs ❌ (simplify to single list)
  - Remove prestige/rank display from religion list items
- [ ] **Update CreateReligionOverlay:**
  - Remove deity dropdown ❌ (just name + public/private)
- [ ] **Update ReligionManagementOverlay:**
  - Remove favor/rank columns from member list
  - Keep: kick, ban, invite, edit description, disband
- [ ] **Update ReligionHeaderRenderer:**
  - Remove deity icon/name display
  - Remove progress bars (favor/prestige)
  - Keep: religion name, member count, role (member/founder)
- [ ] **OPTIONAL:** Add BanMemberOverlay for ban reason/expiry input (currently simplified to direct ban)

#### 4.4 Legacy Supporting Dialogs - DELETE ALL
- [x] `CreateReligionDialog` - **DELETE** (replaced by CreateReligionOverlay)
- [x] `InvitePlayerDialog` - **DELETE** (replaced by ReligionManagementOverlay invite feature)
- [x] `EditDescriptionDialog` - **DELETE** (replaced by ReligionManagementOverlay edit feature)
- [x] `BanPlayerDialog` - **DELETE** (replaced by ReligionManagementOverlay ban feature)

**Note:** All functionality exists in ImGui overlays. Old GuiDialog classes are dead code (handled in Phase 1.5).

#### 4.5 Update PantheonWarsSystem Client Initialization
- [ ] Update GuildManagementDialog instantiation (renamed from BlessingDialog)
- [ ] Remove FavorHudElement instantiation (already obsolete)
- [ ] Remove DeitySelectionDialog instantiation (unused)
- [ ] Remove `_religionDialog` field (ReligionManagementDialog - unused legacy code)
- [ ] Remove any event handlers for deleted GuiDialog classes
- [ ] **KEEP** all ImGui overlay event handlers ✅

#### 4.6 Remove Deity References from ImGui Components
- [ ] **Delete DeityHelper.cs** (already listed in Phase 1.6) - deity-only utility
- [ ] **Delete DeityIconLoader.cs** (already listed in Phase 1.6) - deity-only utility
- [ ] **Update ColorPalette.cs:**
  - Keep as-is ✅ (colors still used for general UI)
- [ ] **Update ReligionHeaderRenderer.cs:**
  - Remove deity icon rendering (remove DeityIconLoader usage)
  - Remove deity name display (remove DeityHelper usage)
  - Simplify to show only: religion name, member count, player role
- [ ] **Update ReligionListRenderer.cs:**
  - Remove deity column from religion list items
  - Remove DeityIconLoader and DeityHelper usage

### Phase 5: Clean Up Tests
**Goal**: Remove tests for deleted systems, update remaining tests

#### 5.1 Remove Test Files
- [ ] Delete all blessing system tests
- [ ] Delete PvP manager tests
- [ ] Delete favor system tests
- [ ] Delete ability system tests
- [ ] Delete buff system tests
- [ ] Delete prestige manager tests
- [ ] Delete deity registry tests ❌
- [ ] Delete deity command tests ❌

#### 5.2 Update Remaining Tests
- [ ] Update `ReligionManagerTests` (remove rank/prestige/deity tests)
- [ ] Update `PlayerReligionDataManagerTests` (remove favor/deity tests)
- [ ] Update `ReligionCommandsTests` (update create command tests)

### Phase 6: Update Documentation
**Goal**: Rebrand as guild management system

#### 6.1 Update README.md
- [ ] Change title to "Guild Management System for Vintage Story"
- [ ] Remove all references to:
  - Blessings
  - PvP features
  - Favor/Prestige
  - Ranking systems
  - Abilities
  - Deities ❌
- [ ] Focus on guild management features
- [ ] Update keybind documentation
- [ ] Simplify feature list

#### 6.2 Update Mod Metadata
- [ ] Update `assets/modinfo.json`:
  - Change name to "Guild Management System"
  - Update description
  - Change version to 2.0.0 (major rewrite)
- [ ] Update mod ID if needed

#### 6.3 Archive Old Documentation
- [ ] Move old docs to `/docs/archived/`
- [ ] Create new simple documentation for guild features

### Phase 7: Final Testing
**Goal**: Verify all guild features work correctly

#### 7.1 Manual Testing
- [ ] Test keybind opens dialog
- [ ] Test creating guild (public/private) - ❌ NO deity required
- [ ] Test joining guild
- [ ] Test leaving guild
- [ ] Test inviting players
- [ ] Test kicking members
- [ ] Test banning/unbanning players
- [ ] Test disbanding guild
- [ ] Test editing description
- [ ] Test guild list display - ❌ NO deity shown
- [ ] Test member list display
- [ ] Test public vs private guilds
- [ ] Test founder privileges

#### 7.2 Data Persistence
- [ ] Test guild data saves correctly (no deity field)
- [ ] Test player data saves correctly (no deity field)
- [ ] Test ban data persists
- [ ] Test server restart preserves all data

#### 7.3 Multiplayer Testing
- [ ] Test with multiple players
- [ ] Test invitations work
- [ ] Test kick notifications
- [ ] Test ban notifications
- [ ] Test guild disbanding notifications

---

## File Structure After Conversion

### Keep These Files
```
PantheonWars/
├── Commands/
│   └── ReligionCommands.cs ✅
├── Data/
│   ├── ReligionData.cs ✅ (simplified)
│   └── PlayerReligionData.cs ✅ (simplified)
├── GUI/
│   ├── GuildManagementDialog.cs ✅ (renamed from BlessingDialog.cs - ImGui main dialog)
│   ├── GuildDialogEventHandlers.cs ✅ (renamed from BlessingDialogEventHandlers.cs)
│   ├── GuildDialogManager.cs ✅ (renamed from BlessingDialogManager.cs)
│   ├── OverlayCoordinator.cs ✅
│   └── UI/
│       ├── Components/
│       │   ├── Buttons/
│       │   │   └── ButtonRenderer.cs ✅
│       │   ├── Inputs/
│       │   │   ├── TextInput.cs ✅
│       │   │   ├── Checkbox.cs ✅
│       │   │   └── Dropdown.cs ✅
│       │   ├── Lists/
│       │   │   ├── ScrollableList.cs ✅
│       │   │   └── Scrollbar.cs ✅
│       │   └── TabControl.cs ✅
│       ├── Renderers/
│       │   ├── ReligionBrowserOverlay.cs ✅ (simplified)
│       │   ├── CreateReligionOverlay.cs ✅ (simplified)
│       │   ├── ReligionManagementOverlay.cs ✅ (simplified)
│       │   ├── LeaveReligionConfirmOverlay.cs ✅
│       │   ├── ReligionHeaderRenderer.cs ✅ (simplified)
│       │   └── Components/
│       │       ├── ReligionListRenderer.cs ✅ (simplified)
│       │       ├── MemberListRenderer.cs ✅ (simplified)
│       │       └── BanListRenderer.cs ✅
│       ├── State/
│       │   ├── ReligionBrowserState.cs ✅
│       │   ├── CreateReligionState.cs ✅
│       │   └── ReligionManagementState.cs ✅
│       └── Utilities/
│           ├── ColorPalette.cs ✅
│           └── TextRenderer.cs ✅
├── Models/
│   └── Enum/ ✅ (simplified, remove DeityType or set to None only)
├── Network/
│   ├── ReligionListRequestPacket.cs ✅
│   ├── ReligionListResponsePacket.cs ✅ (simplified)
│   ├── PlayerReligionInfoRequestPacket.cs ✅
│   ├── PlayerReligionInfoResponsePacket.cs ✅ (simplified)
│   ├── ReligionActionRequestPacket.cs ✅
│   ├── ReligionActionResponsePacket.cs ✅
│   ├── CreateReligionRequestPacket.cs ✅
│   ├── CreateReligionResponsePacket.cs ✅
│   ├── EditDescriptionRequestPacket.cs ✅
│   ├── EditDescriptionResponsePacket.cs ✅
│   ├── ReligionStateChangedPacket.cs ✅
│   └── PlayerReligionDataPacket.cs ✅ (simplified)
├── Systems/
│   ├── ReligionManager.cs ✅ (simplified)
│   └── PlayerReligionDataManager.cs ✅ (simplified)
├── Constants/
│   ├── SystemConstants.cs ✅
│   └── VintageStoryStats.cs ✅
└── PantheonWarsSystem.cs ✅ (simplified)
```

### Delete These Files
```
❌ Commands/BlessingCommands.cs
❌ Commands/DeityCommands.cs
❌ Commands/FavorCommands.cs
❌ Commands/AbilityCommands.cs
❌ Systems/BlessingRegistry.cs
❌ Systems/BlessingEffectSystem.cs
❌ Systems/BlessingDefinitions.cs
❌ Systems/BlessingDialogManager.cs
❌ Systems/DeityRegistry.cs
❌ Systems/PvPManager.cs
❌ Systems/FavorSystem.cs
❌ Systems/ReligionPrestigeManager.cs
❌ Systems/AbilitySystem.cs
❌ Systems/AbilityRegistry.cs
❌ Systems/AbilityCooldownManager.cs
❌ Systems/BuffSystem/ (entire folder)
❌ GUI/FavorHudElement.cs
❌ GUI/BlessingTreeLayout.cs
❌ GUI/DeitySelectionDialog.cs
❌ GUI/ReligionManagementDialog.cs (old GuiDialog - replaced by ImGui overlays)
❌ GUI/CreateReligionDialog.cs (old GuiDialog - replaced by CreateReligionOverlay)
❌ GUI/InvitePlayerDialog.cs (old GuiDialog - replaced by ReligionManagementOverlay)
❌ GUI/EditDescriptionDialog.cs (old GuiDialog - replaced by ReligionManagementOverlay)
❌ GUI/BanPlayerDialog.cs (old GuiDialog - replaced by ReligionManagementOverlay)
❌ GUI/UI/Utilities/DeityHelper.cs
❌ GUI/UI/Utilities/DeityIconLoader.cs
❌ Models/Blessing.cs
❌ Models/BlessingNodeState.cs
❌ Models/BlessingTooltipData.cs
❌ Models/Deity.cs
❌ Models/Ability.cs
❌ Models/PlayerFavorProgress.cs
❌ Models/ReligionPrestigeProgress.cs
❌ Data/PlayerAbilityData.cs
❌ Network/BlessingUnlockRequestPacket.cs
❌ Network/BlessingUnlockResponsePacket.cs
❌ Network/BlessingDataRequestPacket.cs
❌ Network/BlessingDataResponsePacket.cs
❌ Constants/BlessingIds.cs
❌ Constants/BlessingCommandConstants.cs
❌ Abilities/ (entire folder)
```

### Rename These Files
```
BlessingDialog.cs → GuildManagementDialog.cs
BlessingDialogEventHandlers.cs → GuildDialogEventHandlers.cs
BlessingDialogManager.cs → GuildDialogManager.cs
```

---

## Post-Conversion Summary

### What Users Get
1. **Simple Guild System**: Create and manage guilds with friends
2. **Modern ImGui Interface**:
   - Press **P key** to open GuildManagementDialog (renamed from BlessingDialog)
   - Click **"Change Religion"** button to browse/join/create guilds
   - Click **"Manage Religion"** button to invite/kick/ban/edit/disband (founder only)
   - Smooth overlays with form validation, tooltips, and responsive interactions
3. **Core Features**:
   - Create public or private guilds (just name + visibility, no deity selection)
   - Join/leave guilds
   - Invite players
   - Founder can kick/ban members
   - Founder can disband guild
   - Guild descriptions
   - Ban system with expiry
4. **No Themes/Deities**: Pure guild names, no cosmetic themes or deity associations
5. **No Combat**: Pure social/management system (no PvP, favor, or prestige)

### What's Removed
- All combat mechanics (PvP, favor earning)
- All progression (ranks, favor, prestige)
- All stat bonuses (blessings, abilities, buffs)
- Complex UI (HUD elements, blessing trees)
- Multiple command groups (only `/religion` remains)

### Why This is Better
- **Focused**: Does one thing well (guild management)
- **Simple**: No complex mechanics to learn
- **Social**: Purely about organizing players
- **Lightweight**: Much smaller mod, easier to maintain
- **Flexible**: Can be used alongside combat mods

---

## Estimated Effort
- **Phase 1**: 2-3 hours (file deletion - blessing/PvP/favor/deity/ability/buff systems)
- **Phase 2**: 1-2 hours (model simplification - remove deity/progression fields)
- **Phase 3**: 2-3 hours (system updates - remove blessing/PvP/favor logic)
- **Phase 4**: 3-4 hours (rename BlessingDialog + simplify ImGui overlays - remove deity/rank/favor UI)
- **Phase 5**: 1-2 hours (test cleanup)
- **Phase 6**: 1-2 hours (documentation updates)
- **Phase 7**: 2-3 hours (final testing)
- **Total**: 12-19 hours

**Key Simplifications:**
- No new UI to build (ImGui system already complete and functional)
- No keybind registration needed (P key already works)
- Legacy GuiDialog classes are dead code (simple deletion)
- Main work is:
  1. File deletion (Phase 1)
  2. Renaming BlessingDialog → GuildManagementDialog (Phase 4.1)
  3. Removing deity/progression elements from existing ImGui overlays (Phase 4.3-4.6)
- Network layer already supports simplified guild operations

---

## Next Steps
1. Get user approval for plan
2. Create backup branch
3. Start with Phase 1 (file deletion)
4. Work through phases sequentially
5. Test after each phase
6. Commit with clear messages
7. Create PR when complete
