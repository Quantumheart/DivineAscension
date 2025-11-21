# Guild Management System Conversion Plan

## Overview
Convert PantheonWars from a complex religion/deity/blessing/PvP system into a streamlined guild/group management system.

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
- **ReligionManagementDialog** - Main guild management UI
- **CreateReligionDialog** - Guild creation interface
- **InvitePlayerDialog** - Invite players to guild
- **EditDescriptionDialog** - Edit guild description
- **BanPlayerDialog** - Ban management
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
  - BlessingDialog
  - BlessingTreeLayout
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

#### 1.1 Remove Blessing System Files ✅ COMPLETE
- [x] Delete `/Systems/BlessingRegistry.cs`
- [x] Delete `/Systems/BlessingEffectSystem.cs`
- [x] Delete `/Systems/BlessingDefinitions.cs`
- [x] Delete `/Systems/BlessingDialogManager.cs`
- [x] Delete `/Commands/BlessingCommands.cs`
- [x] Delete `/GUI/BlessingDialog.cs`
- [x] Delete `/GUI/BlessingTreeLayout.cs`
- [x] Delete `/GUI/BlessingDialogManager.cs`
- [x] Delete `/GUI/BlessingDialogEventHandlers.cs`
- [x] Delete `/Models/Blessing.cs`
- [x] Delete `/Models/BlessingNodeState.cs`
- [x] Delete `/Models/BlessingTooltipData.cs`
- [x] Delete `/Constants/BlessingIds.cs`
- [x] Delete `/Constants/BlessingCommandConstants.cs`
- [x] Delete `/Network/BlessingUnlockRequestPacket.cs`
- [x] Delete `/Network/BlessingUnlockResponsePacket.cs`
- [x] Delete `/Network/BlessingDataRequestPacket.cs`
- [x] Delete `/Network/BlessingDataResponsePacket.cs`
- [x] Delete entire `/GUI/UI/` directory (blessing renderers, components, state)
- [x] Delete blessing enum files (BlessingKind, BlessingCategory)
- [x] Delete blessing interfaces (IBlessingRegistry, IBlessingEffectSystem)

#### 1.2 Remove PvP & Favor Systems ✅ COMPLETE
- [x] Delete `/Systems/PvPManager.cs`
- [x] Delete `/Systems/FavorSystem.cs`
- [x] Delete `/Systems/ReligionPrestigeManager.cs`
- [x] Delete `/Commands/FavorCommands.cs`
- [x] Delete `/Models/PlayerFavorProgress.cs`
- [x] Delete `/Models/ReligionPrestigeProgress.cs`

#### 1.3 Remove Ability System (Legacy) ✅ COMPLETE
- [x] Delete `/Systems/AbilitySystem.cs`
- [x] Delete `/Systems/AbilityRegistry.cs`
- [x] Delete `/Systems/AbilityCooldownManager.cs`
- [x] Delete `/Commands/AbilityCommands.cs`
- [x] Delete `/Commands/DeityCommands.cs`
- [x] Delete `/Models/Ability.cs`
- [x] Delete `/Data/PlayerAbilityData.cs`
- [x] Delete entire `/Abilities/` folder (Khoras, Lysa implementations)

#### 1.4 Remove Buff System ✅ COMPLETE
- [x] Delete `/Systems/BuffSystem/BuffManager.cs`
- [x] Delete `/Systems/BuffSystem/EntityBehaviorBuffTracker.cs`
- [x] Delete `/Systems/BuffSystem/ActiveEffect.cs`
- [x] Delete `/Systems/BuffSystem/Interfaces/IBuffManager.cs`

#### 1.5 Remove UI Elements ✅ COMPLETE
- [x] Delete `/GUI/FavorHudElement.cs`
- [x] Delete `/GUI/DeitySelectionDialog.cs`
- [x] Delete `/GUI/OverlayCoordinator.cs`

#### 1.6 Remove Deity System ✅ COMPLETE
- [x] Delete `/Systems/DeityRegistry.cs`
- [x] Delete `/Models/Deity.cs`
- [x] Delete `/Commands/DeityCommands.cs` (already in 1.3)
- [x] Delete `/Models/Enum/DeityType.cs`
- [x] Delete `/Models/Enum/DeityAlignment.cs`
- [x] Delete `/Models/Enum/DeityRelationshipType.cs`

**Phase 1 Summary:** ✅ Removed 83 files (14,832 lines of code)

### Phase 2: Simplify Data Models ✅ COMPLETE
**Goal**: Remove progression data, keep only guild membership info

#### 2.1 Update ReligionData ✅ COMPLETE
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

#### 2.2 Update PlayerReligionData ✅ COMPLETE
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

#### 2.3 Simplify Network Packets (TODO - Phase 3)
- [ ] Update `PlayerReligionDataPacket` (remove favor/rank/deity fields)
- [ ] Update `ReligionListResponsePacket` (remove prestige/rank/deity fields)
- [ ] Update `PlayerReligionInfoResponsePacket` (remove favor/prestige/rank/deity fields)
- [ ] Update `CreateReligionRequestPacket` (remove deity parameter)
- [ ] Update `CreateReligionResponsePacket` (remove deity references)

#### 2.4 Update Persistence (TODO - Phase 3)
- [ ] Update save/load logic in `ReligionManager`
- [ ] Update save/load logic in `PlayerReligionDataManager`
- [ ] Remove rank/favor/prestige from saved data

**Phase 2 Summary:** ✅ Simplified ReligionData and PlayerReligionData (233 lines removed)

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
**Goal**: Single keybind opens simplified guild management dialog

#### 4.1 Add Keybind Registration
- [ ] Register keybind in `PantheonWarsSystem.StartClientSide()`
- [ ] Create hotkey to open `ReligionManagementDialog`
- [ ] User presses assigned key → `ReligionManagementDialog` opens

```csharp
// Example keybind registration
_capi.Input.RegisterHotKey("pantheonwarsreligion", "Open Guild Management",
    GlKeys.G, HotkeyType.GUIOrOtherControls);
_capi.Input.SetHotKeyHandler("pantheonwarsreligion", (bool keyDown) =>
{
    if (keyDown)
    {
        if (_religionDialog == null)
            _religionDialog = new ReligionManagementDialog(_capi, _clientChannel);
        _religionDialog.Toggle();
    }
    return true;
});
```

#### 4.2 Simplify ReligionManagementDialog
- [ ] Remove references to blessings, favor, ranks, deities
- [ ] Remove deity filtering dropdown ❌
- [ ] Update member list display (remove favor/rank columns)
- [ ] Update religion info display (remove prestige/rank/deity)
- [ ] Keep: Browse religions, manage members, create guild

#### 4.3 Update Supporting Dialogs
- [ ] `CreateReligionDialog` - ❌ REMOVE deity selection dropdown
- [ ] `InvitePlayerDialog` - Keep as-is
- [ ] `EditDescriptionDialog` - Keep as-is
- [ ] `BanPlayerDialog` - Keep as-is

#### 4.4 Remove Deleted Dialogs
- [ ] Remove instantiation of `BlessingDialog`
- [ ] Remove instantiation of `FavorHudElement`
- [ ] Remove instantiation of `DeitySelectionDialog`

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
│   ├── ReligionManagementDialog.cs ✅
│   ├── CreateReligionDialog.cs ✅
│   ├── InvitePlayerDialog.cs ✅
│   ├── EditDescriptionDialog.cs ✅
│   └── BanPlayerDialog.cs ✅
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
❌ Systems/PvPManager.cs
❌ Systems/FavorSystem.cs
❌ Systems/ReligionPrestigeManager.cs
❌ Systems/AbilitySystem.cs
❌ Systems/AbilityRegistry.cs
❌ Systems/AbilityCooldownManager.cs
❌ Systems/BuffSystem/ (entire folder)
❌ GUI/BlessingDialog.cs
❌ GUI/BlessingTreeLayout.cs
❌ GUI/BlessingDialogManager.cs
❌ GUI/BlessingDialogEventHandlers.cs
❌ GUI/FavorHudElement.cs
❌ GUI/DeitySelectionDialog.cs
❌ GUI/OverlayCoordinator.cs
❌ Models/Blessing.cs
❌ Models/BlessingNodeState.cs
❌ Models/BlessingTooltipData.cs
❌ Models/Deity.cs
❌ Models/Ability.cs
❌ Models/PlayerFavorProgress.cs
❌ Models/ReligionPrestigeProgress.cs
❌ Data/PlayerAbilityData.cs
❌ Network/Blessing*.cs (all blessing packets)
❌ Constants/BlessingIds.cs
❌ Constants/BlessingCommandConstants.cs
❌ Abilities/ (entire folder)
```

---

## Post-Conversion Summary

### What Users Get
1. **Simple Guild System**: Create and manage guilds with friends
2. **One Keybind**: Press assigned key to open guild management
3. **Core Features**:
   - Create public or private guilds (just name + visibility)
   - Join/leave guilds
   - Invite players
   - Founder can kick/ban members
   - Founder can disband guild
   - Guild descriptions
   - Ban system with expiry
4. **No Themes/Deities**: Pure guild names, no cosmetic themes
5. **No Combat**: Pure social/management system

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
- **Phase 1-2**: 2-3 hours (file deletion, model simplification)
- **Phase 3-4**: 3-4 hours (system updates, GUI work, keybind)
- **Phase 5-6**: 2-3 hours (test cleanup, documentation)
- **Phase 7**: 2-3 hours (final testing)
- **Total**: 9-13 hours

---

## Next Steps
1. Get user approval for plan
2. Create backup branch
3. Start with Phase 1 (file deletion)
4. Work through phases sequentially
5. Test after each phase
6. Commit with clear messages
7. Create PR when complete
