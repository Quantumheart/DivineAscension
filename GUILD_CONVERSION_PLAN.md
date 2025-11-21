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
5. **Optional cosmetic deities** - Keep deity selection as guild theme/flavor

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

### 🔄 SIMPLIFY - Make Cosmetic
- **Deities** - Keep as optional guild themes (no mechanical effects)
- **DeityRegistry** - Keep for deity names/descriptions only
- **Data Models** - Remove favor, prestige, ranks, blessings

### ❌ REMOVE - All Progression & Combat Systems
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

#### 1.1 Remove Blessing System Files
- [ ] Delete `/Systems/BlessingRegistry.cs`
- [ ] Delete `/Systems/BlessingEffectSystem.cs`
- [ ] Delete `/Systems/BlessingDefinitions.cs`
- [ ] Delete `/Systems/BlessingDialogManager.cs`
- [ ] Delete `/Commands/BlessingCommands.cs`
- [ ] Delete `/GUI/BlessingDialog.cs`
- [ ] Delete `/GUI/BlessingTreeLayout.cs`
- [ ] Delete `/GUI/BlessingDialogManager.cs`
- [ ] Delete `/GUI/BlessingDialogEventHandlers.cs`
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
- [ ] Delete `/Commands/DeityCommands.cs`
- [ ] Delete `/Models/Ability.cs`
- [ ] Delete `/Data/PlayerAbilityData.cs`
- [ ] Delete entire `/Abilities/` folder (legacy Phase 1-2)

#### 1.4 Remove Buff System
- [ ] Delete `/Systems/BuffSystem/BuffManager.cs`
- [ ] Delete `/Systems/BuffSystem/EntityBehaviorBuffTracker.cs`
- [ ] Delete `/Systems/BuffSystem/ActiveEffect.cs`
- [ ] Delete `/Systems/BuffSystem/Interfaces/` folder

#### 1.5 Remove UI Elements
- [ ] Delete `/GUI/FavorHudElement.cs`
- [ ] Delete `/GUI/DeitySelectionDialog.cs`
- [ ] Delete `/GUI/OverlayCoordinator.cs` (if only used for blessings)

### Phase 2: Simplify Data Models
**Goal**: Remove progression data, keep only guild membership info

#### 2.1 Update ReligionData
```csharp
// Remove:
- Prestige (int)
- TotalPrestige (int)
- PrestigeRank (PrestigeRank enum)
- UnlockedBlessings (Dictionary)

// Keep:
- ReligionUID
- ReligionName
- Deity (cosmetic only)
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

// Keep:
- PlayerUID
- ReligionUID
- ActiveDeity (cosmetic only)
- LastReligionSwitchDate (for cooldown)
- TotalReligionSwitches
```

#### 2.3 Simplify Network Packets
- [ ] Update `PlayerReligionDataPacket` (remove favor/rank fields)
- [ ] Update `ReligionListResponsePacket` (remove prestige/rank fields)
- [ ] Update `PlayerReligionInfoResponsePacket` (remove favor/prestige/rank fields)
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
- [ ] Remove deity/ability/blessing/favor command registration
- [ ] Keep only religion command registration
- [ ] Remove blessing-related network handlers
- [ ] Remove buff entity behavior registration
- [ ] Simplify OnPlayerJoin (no favor/rank data needed)

#### 3.4 Keep Only ReligionCommands
- [ ] Verify all `/religion` commands still work:
  - `/religion create <name> <deity> [public/private]`
  - `/religion join <name>`
  - `/religion leave`
  - `/religion list [deity]`
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
- [ ] Remove references to blessings, favor, ranks
- [ ] Update member list display (remove favor/rank columns)
- [ ] Update religion info display (remove prestige/rank)
- [ ] Keep: Browse religions, manage members, create guild

#### 4.3 Keep Supporting Dialogs
- [ ] `CreateReligionDialog` - Keep as-is (deity is now cosmetic)
- [ ] `InvitePlayerDialog` - Keep as-is
- [ ] `EditDescriptionDialog` - Keep as-is
- [ ] `BanPlayerDialog` - Keep as-is

#### 4.4 Remove Deleted Dialogs
- [ ] Remove instantiation of `BlessingDialog`
- [ ] Remove instantiation of `FavorHudElement`
- [ ] Remove instantiation of `DeitySelectionDialog`

### Phase 5: Simplify Deity System
**Goal**: Make deities purely cosmetic guild themes

#### 5.1 Keep DeityRegistry (Names Only)
- [ ] Keep `DeityRegistry` for deity names and descriptions
- [ ] Remove deity relationship logic (allies/rivals)
- [ ] Remove deity multipliers for favor
- [ ] Deities become aesthetic choices for guild theme

#### 5.2 Update Deity Model
```csharp
// Simplify to:
public class Deity
{
    public DeityType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Lore { get; set; }
    // Remove: Allies, Rivals, FavorMultipliers
}
```

### Phase 6: Clean Up Tests
**Goal**: Remove tests for deleted systems, update remaining tests

#### 6.1 Remove Test Files
- [ ] Delete all blessing system tests
- [ ] Delete PvP manager tests
- [ ] Delete favor system tests
- [ ] Delete ability system tests
- [ ] Delete buff system tests
- [ ] Delete prestige manager tests

#### 6.2 Update Remaining Tests
- [ ] Update `ReligionManagerTests` (remove rank/prestige tests)
- [ ] Update `PlayerReligionDataManagerTests` (remove favor tests)
- [ ] Update `ReligionCommandsTests`
- [ ] Update `DeityRegistryTests` (simplified)

### Phase 7: Update Documentation
**Goal**: Rebrand as guild management system

#### 7.1 Update README.md
- [ ] Change title to "Guild Management System for Vintage Story"
- [ ] Remove all references to:
  - Blessings
  - PvP features
  - Favor/Prestige
  - Ranking systems
  - Abilities
- [ ] Focus on guild management features
- [ ] Update keybind documentation
- [ ] Simplify feature list

#### 7.2 Update Mod Metadata
- [ ] Update `assets/modinfo.json`:
  - Change name to "Guild Management System"
  - Update description
  - Change version to 2.0.0 (major rewrite)
- [ ] Update mod ID if needed

#### 7.3 Archive Old Documentation
- [ ] Move old docs to `/docs/archived/`
- [ ] Create new simple documentation for guild features

### Phase 8: Final Testing
**Goal**: Verify all guild features work correctly

#### 8.1 Manual Testing
- [ ] Test keybind opens dialog
- [ ] Test creating guild (public/private)
- [ ] Test joining guild
- [ ] Test leaving guild
- [ ] Test inviting players
- [ ] Test kicking members
- [ ] Test banning/unbanning players
- [ ] Test disbanding guild
- [ ] Test editing description
- [ ] Test guild list display
- [ ] Test member list display
- [ ] Test public vs private guilds
- [ ] Test founder privileges

#### 8.2 Data Persistence
- [ ] Test guild data saves correctly
- [ ] Test player data saves correctly
- [ ] Test ban data persists
- [ ] Test server restart preserves all data

#### 8.3 Multiplayer Testing
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
│   ├── Deity.cs ✅ (simplified)
│   └── Enum/ ✅
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
│   ├── DeityRegistry.cs ✅ (simplified)
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
   - Create public or private guilds
   - Join/leave guilds
   - Invite players
   - Founder can kick/ban members
   - Founder can disband guild
   - Guild descriptions
   - Ban system with expiry
4. **Cosmetic Deities**: Optional guild theme (8 choices)
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
- **Phase 5-6**: 1-2 hours (deity simplification, test cleanup)
- **Phase 7-8**: 2-3 hours (documentation, testing)
- **Total**: 8-12 hours

---

## Next Steps
1. Get user approval for plan
2. Create backup branch
3. Start with Phase 1 (file deletion)
4. Work through phases sequentially
5. Test after each phase
6. Commit with clear messages
7. Create PR when complete
