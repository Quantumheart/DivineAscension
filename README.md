# Pantheon Wars

**Version:** 1.0.0-beta
**Status:** Release Candidate - Ready for Testing

A religion-based PvP mod for Vintage Story featuring custom religions, competing deities, and passive blessing trees with dual progression systems.

## Overview

Pantheon Wars introduces a comprehensive religion and deity worship system where players create or join custom religions dedicated to different gods. Each religion unlocks unique passive blessing trees that enhance all members. Players earn individual Divine Favor and collective Religion Prestige through PvP combat, unlocking powerful blessings and progressing through dual ranking systems.

**v1.0 ships with all 30 blessings providing functional stat modifiers.** Advanced special effects (lifesteal, poison, critical strikes, etc.) will be added in post-launch patches based on player feedback.

## Features

### Religion System ✅
- **Custom Player-Created Religions**: Create and name your own religions dedicated to any deity
- **Public & Private Religions**: Control who can join your congregation
- **Invitation System**: Invite specific players to join private religions
- **Founder Privileges**: Religion creators manage members and settings
- **Religion Switching**: Change religions with a 7-day cooldown (losing favor and blessings)
- **Single Religion Membership**: Players can only be in one religion at a time

### Deity System ✅
- **3 Unique Deities**: Aethra (Light/Good), Gaia (Nature/Neutral), Morthen (Shadow & Death/Evil)
- **Religion-Based Deity Assignment**: Your deity is determined by your religion
- **Clear Faction Identity**: Simple Good vs Neutral vs Evil alignment system
- **Deity Relationships**: Rival and neutral deity dynamics affect favor and prestige gain
- **Deity-Specific Blessing Trees**: Each deity has unique passive blessings

### Dual Ranking System ✅
- **Player Favor Ranks**: Individual progression (Initiate → Disciple → Zealot → Champion → Avatar)
- **Religion Prestige Ranks**: Collective progression (Fledgling → Established → Renowned → Legendary → Mythic)
- **Divine Favor Currency**: Earned through PvP combat with deity relationship multipliers
- **Religion Prestige**: Earned collectively by all religion members through PvP

### Blessing System ✅ (v1.0 - Stat Modifiers Complete)
- **30 Passive Blessings**: 10 blessings per deity (6 player blessings + 4 religion blessings)
- **Player Blessings**: Unlock based on your individual Favor Rank
- **Religion Blessings**: Unlock based on your religion's Prestige Rank, benefit all members
- **Stat Modifiers**: Blessings provide passive bonuses (damage, defense, speed, health, armor, etc.) ✅ **Working**
- **Special Effects**: Unique deity-themed abilities (lifesteal, poison, critical hits, etc.) ⚠️ **Coming in patches**
- **Blessing Trees**: Command-based tree viewer with unlock status

### PvP Features ⚠️ (Planned - Phase 4)
- **Divine Duels**: Formal 1v1 challenges with favor stakes
- **Crusade Events**: Server-wide deity war events
- **Relic System**: Powerful artifacts that grant temporary dominance
- **Apostate Mechanics**: Penalties and consequences for deity betrayal

## Development Setup

### Prerequisites
- .NET 8 SDK or later
- Vintage Story 1.21.0 or later
- Visual Studio 2022, VS Code, or JetBrains Rider

### Environment Variable
Set the `VINTAGE_STORY` environment variable to your Vintage Story installation directory:

**Windows:**
```powershell
$env:VINTAGE_STORY = "C:\Path\To\Vintage Story"
```

**Linux/Mac:**
```bash
export VINTAGE_STORY="/path/to/vintagestory"
```

### Building

**Windows:**
```powershell
./build.ps1
```

**Linux/Mac:**
```bash
./build.sh
```

This will:
1. Validate all JSON files
2. Build the mod
3. Create a release package in `Release/pantheonwars_x.x.x.zip`

### Debugging

Open `PantheonWars.sln` in your IDE and select either:
- **Vintage Story Client** - Launch client with mod loaded
- **Vintage Story Server** - Launch dedicated server with mod loaded

### Controls

- **K** - Open deity selection dialog

## Documentation

For detailed information about the mod's systems and mechanics, see:

**Documentation Index:**
- **[Documentation Home](docs/README.md)** - Complete documentation index organized by topic

**Phase 3 Documentation:**
- **[Implementation Guide](docs/topics/implementation/implementation_guide.md)** - Development roadmap and phase breakdown
- **[Phase 3 Task Breakdown](docs/topics/planning/phase3_task_breakdown.md)** - Detailed task list and progress tracking
- **[Phase 3 Design Guide](docs/topics/planning/phase3_group_deity_blessings_guide.md)** - Religion and blessing system design specifications

**Reference Documentation:**
- **[Deity Reference](docs/topics/reference/deity_reference.md)** - Complete deity information, relationships, and lore
- **[Favor System Guide](docs/topics/reference/favor_reference.md)** - How favor works, earning methods, and devotion ranks
- **[Blessing Reference](docs/topics/reference/blessing_reference.md)** - Blessing system mechanics
- **[Ability Reference](docs/topics/reference/ability_reference.md)** - Old ability system (deprecated)

## Project Structure

```
PantheonWars/
├── CakeBuild/              # Build system
│   ├── Program.cs          # Build tasks and packaging
│   └── CakeBuild.csproj
├── docs/                   # Documentation
│   ├── README.md           # Documentation index
│   └── topics/             # Documentation organized by topic
│       ├── reference/      # Game system references
│       ├── implementation/ # Implementation guides
│       ├── ui-design/      # UI design documents
│       ├── testing/        # Testing guides
│       ├── art-assets/     # Icon and asset specs
│       ├── planning/       # Phase planning docs
│       ├── integration/    # System integration guides
│       └── analysis/       # External mod analysis
├── PantheonWars/           # Main mod project
│   ├── Abilities/ (legacy) # Old ability system (Phase 1-2)
│   │   ├── Khoras/         # To be removed in Phase 3.5
│   │   └── Lysa/
│   ├── Commands/           # Chat commands
│   │   ├── DeityCommands.cs (legacy)
│   │   ├── AbilityCommands.cs (legacy)
│   │   ├── ReligionCommands.cs ✅ NEW
│   │   └── BlessingCommands.cs ✅ NEW
│   ├── Data/               # Data models for persistence
│   │   ├── PlayerDeityData.cs (legacy)
│   │   ├── ReligionData.cs ✅ NEW
│   │   └── PlayerReligionData.cs ✅ NEW
│   ├── GUI/                # User interface
│   │   ├── DeitySelectionDialog.cs (legacy)
│   │   ├── FavorHudElement.cs (updated for Phase 3) ✅
│   │   ├── ReligionManagementDialog.cs ✅ NEW
│   │   ├── CreateReligionDialog.cs ✅ NEW
│   │   ├── InvitePlayerDialog.cs ✅ NEW
│   │   └── EditDescriptionDialog.cs ✅ NEW
│   ├── Models/             # Core data models
│   │   ├── Deity.cs
│   │   ├── Blessing.cs ✅ NEW
│   │   ├── PrestigeRank.cs ✅ NEW
│   │   ├── FavorRank.cs ✅ NEW
│   │   ├── BlessingType.cs ✅ NEW
│   │   ├── BlessingCategory.cs ✅ NEW
│   │   └── Enums (DeityType, etc.)
│   ├── Network/            # Client-server networking
│   │   ├── PlayerDataPacket.cs (legacy)
│   │   └── PlayerReligionDataPacket.cs ✅ NEW
│   ├── Systems/            # Core game systems
│   │   ├── DeityRegistry.cs
│   │   ├── ReligionManager.cs ✅ NEW
│   │   ├── PlayerReligionDataManager.cs ✅ NEW
│   │   ├── ReligionPrestigeManager.cs ✅ NEW
│   │   ├── BlessingRegistry.cs ✅ NEW
│   │   ├── BlessingEffectSystem.cs ✅ NEW
│   │   ├── BlessingDefinitions.cs ✅ NEW (all 30 blessings)
│   │   │   ├── Aethra (Light - 10 blessings) ✅
│   │   │   ├── Gaia (Nature - 10 blessings) ✅
│   │   │   └── Morthen (Shadow & Death - 10 blessings) ✅
│   │   ├── BuffSystem/ ✅ (Phase 2)
│   │   │   ├── BuffManager.cs
│   │   │   ├── ActiveEffect.cs
│   │   │   └── EntityBehaviorBuffTracker.cs
│   │   ├── PlayerDataManager.cs (legacy)
│   │   ├── AbilityCooldownManager.cs (legacy)
│   │   ├── FavorSystem.cs (updated for Phase 3)
│   │   └── AbilitySystem.cs (legacy - to be removed)
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── assets/
│   │   └── modinfo.json    # Mod metadata
│   ├── PantheonWars.csproj
│   └── PantheonWarsSystem.cs
├── Release/                # Build output
├── .gitignore
├── build.ps1               # Windows build script
├── build.sh                # Linux/Mac build script
├── PantheonWars.sln
└── README.md
```

## Current Status (v1.0.0-beta - Release Candidate)

The mod is **ready for launch** with all core religion and blessing systems functional. Stat modifiers work perfectly - special effects deferred to post-launch patches.

### What's New in v1.0 🎉

**Complete Systems:**
- ✅ **3-Deity System Implemented** - 30 blessings fully defined (10 per deity)
- ✅ **Functional Stat Modifiers** - Blessings apply real gameplay bonuses
- ✅ **Religion Management GUI** - Full tabbed interface for managing religions
- ✅ **Automatic Blessing Notifications** - Religion members notified when new blessings unlock
- ✅ **Blessing Effect Refresh** - Stat modifiers auto-apply on unlock
- ✅ **All Core Systems Working** - Religion, progression, blessings, persistence

**System Consolidation (8→3 Deities, 80→30 Blessings):**
- **67% reduction in content** for easier balance and maintenance
- **Stronger deity identity** - Each deity now represents 33% of content vs 12.5%
- **Clear faction system** - Good (Light) vs Neutral (Nature) vs Evil (Shadow & Death)
- **Simple PvP dynamics** - 3-way triangle instead of 28 matchup combinations
- **Achievable endgame** - Players can max out complete deity trees

### Implemented Systems ✅

**Religion Management:**
- ✅ Create custom religions with any deity
- ✅ Public/private religion system with invitations
- ✅ Join, leave, and manage religions
- ✅ Founder privileges (kick members, disband, set description)
- ✅ 7-day switching cooldown with penalties
- ✅ Full persistence and save/load
- ✅ **Religion Management GUI** with tabbed interface

**Dual Ranking System:**
- ✅ Player Favor Ranks (Initiate → Avatar) - Individual progression
- ✅ Religion Prestige Ranks (Fledgling → Mythic) - Collective progression
- ✅ PvP favor/prestige earning with deity relationship multipliers
- ✅ Rank-up notifications for both systems
- ✅ **Automatic blessing unlock notifications** on rank-up
- ✅ Network synchronization

**Blessing System:**
- ✅ BlessingRegistry with **30/30 blessings registered** (100% complete)
- ✅ Blessing unlock validation (rank requirements, prerequisites)
- ✅ **Stat modifier calculation and application working** (using VS Stats API)
- ✅ Blessing persistence across sessions
- ✅ Combined player + religion blessing effects
- ✅ **All 3 deities fully designed** (10 blessings each)
- ⚠️ Special effect handlers deferred to post-launch patches

**Available Deity Blessing Trees (3/3 - All Complete!):**
- ✅ **Aethra (Light)** - 10 blessings (healing, shields, divine protection) - Good Alignment
- ✅ **Gaia (Nature)** - 10 blessings (defense, regeneration, durability) - Neutral Alignment
- ✅ **Morthen (Shadow & Death)** - 10 blessings (lifesteal, poison, shadow magic) - Evil Alignment

**User Interface:**
- ✅ Enhanced HUD showing religion, deity, both ranks, favor/prestige
- ✅ **Religion Management GUI** - Create, browse, and manage religions
- ✅ All commands functional (17 commands total)
- ⚠️ Visual Blessing Tree Viewer - Optional feature, command-based tree works

## Development Roadmap

**Current Status:** Phase 3 Nearly Complete (~90% - v1.0 Release Candidate)

- ✅ **Phase 1:** Foundation (MVP) - Complete
- ✅ **Phase 2:** Combat Integration - Complete
- ✅ **Phase 3:** Religion-Based Deity System with Blessing Trees - 95% Complete (**v1.0 Release**)
  - ✅ Phase 3.1: Foundation (Religion system, commands, persistence)
  - ✅ Phase 3.2: Ranking Systems (Dual progression, PvP integration)
  - ✅ Phase 3.3: Blessing System Core (Registry, stat application, commands)
  - ✅ Phase 3.4: Deity Blessing Trees (3/3 deities complete, 30/30 blessings defined)
  - ✅ Phase 3.5: 3-Deity Consolidation (Simplified from 8→3 deities for better balance)
  - ⚠️ Phase 3.6: Integration & Polish (Religion GUI done, blessing tree viewer optional)
- 🔲 **Phase 4:** Advanced Features - Planned (Divine duels, crusades, relics, apostates)

**Post-Launch Roadmap:**
- **Patch 1.1:** Core special effects (critical strikes, damage reduction, lifesteal)
- **Patch 1.2:** Advanced combat effects (AoE cleave, execute threshold, headshot bonus)
- **Patch 1.3:** Tactical effects (stealth, tracking vision, multishot)
- **Patch 1.4:** Status effects (poison DoT, plague aura, death aura, companions)
- **Patch 1.5+:** Visual blessing tree GUI, balance tuning, community feedback

For detailed phase breakdowns, tasks, and timeline, see the **[Implementation Guide](docs/topics/implementation/implementation_guide.md)**.

### Available Commands

**Religion Management (10 commands):**
- `/religion create <name> <deity> [public/private]` - Create a new religion
- `/religion join <religionname>` - Join an existing religion
- `/religion leave` - Leave your current religion
- `/religion list [deity]` - List all religions (optionally filter by deity)
- `/religion info [name]` - View religion details (defaults to your religion)
- `/religion members` - View members of your religion with ranks
- `/religion invite <playername>` - Invite a player to your religion
- `/religion kick <playername>` - Kick a member from your religion (founder only)
- `/religion disband` - Disband your religion (founder only)
- `/religion description <text>` - Set religion description (founder only)

**Blessing Management (7 commands):**
- `/blessings list` - Show all available blessings for your deity
- `/blessings player` - Show your unlocked player blessings
- `/blessings religion` - Show your religion's unlocked blessings
- `/blessings info <blessingid>` - Get detailed blessing information
- `/blessings tree [player/religion]` - Display blessing tree in text format
- `/blessings unlock <blessingid>` - Unlock a blessing (if requirements met)
- `/blessings active` - Show all active blessings affecting you

**Legacy Commands (Phase 1-2 - Will be removed in future patch):**
- `/deity list` - Show all available deities
- `/deity info <deity>` - Get detailed deity information
- `/deity status` - View your current deity status
- `/favor` - Check your current divine favor
- `/ability list` - Show available abilities (deprecated)

## Known Limitations (v1.0)

**Deferred to Post-Launch Patches:**
- **Special Effects:** Blessings that reference special effects (lifesteal, poison_dot, critical_strike, etc.) currently provide only their stat modifiers. The special mechanics will be added incrementally in patches 1.1-1.4.
- **Visual Blessing Tree:** No GUI blessing tree viewer yet. Use `/blessings tree` command for text-based view.
- **Old Ability System:** Phase 1-2 ability system still exists but will be removed in a future patch.

**What Works Perfectly:**
- All stat modifier bonuses (damage, health, armor, speed, attack speed, walk speed, etc.)
- Religion creation, management, and progression
- Blessing unlocking and persistence
- Dual ranking system (Favor + Prestige)
- All 17 commands
- Religion Management GUI

## Contributing

**v1.0 is now in beta testing!** We're looking for:
- **Testers:** Try the mod and report bugs or balance issues
- **Feedback:** Which special effects should be prioritized in patches?
- **Balance Data:** How do the stat modifiers feel in actual gameplay?
- **Feature Requests:** What would make the religion system more engaging?

Contributions, suggestions, and feedback are welcome! Please open an issue or discussion on the repository.

## License

This project is licensed under the [Creative Commons Attribution 4.0 International License](LICENSE) (CC BY 4.0).

You are free to:
- **Share** — copy and redistribute the material in any medium or format
- **Adapt** — remix, transform, and build upon the material for any purpose, even commercially

Under the following terms:
- **Attribution** — You must give appropriate credit, provide a link to the license, and indicate if changes were made

See the [LICENSE](LICENSE) file for full details.

## Credits

- Built using the official [Vintage Story Mod Template](https://github.com/anegostudios/vsmodtemplate)
- Inspired by the [Karma System mod](https://mods.vintagestory.at/show/mod/28955)
