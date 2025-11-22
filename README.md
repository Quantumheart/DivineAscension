# Pantheon Wars

**Version:** 2.0.0-alpha
**Status:** In Development - Guild System Redesign

A guild management mod for Vintage Story featuring player-created guilds and community organization.

## Overview

Pantheon Wars provides a streamlined guild system where players create or join custom guilds to connect with other players. Guilds can be public (open to all) or private (invitation-only), with founder privileges for managing members and settings.

## Features

### Guild System ✅
- **Custom Player-Created Guilds**: Create and name your own guilds
- **Public & Private Guilds**: Control who can join your guild
- **Invitation System**: Invite specific players to join private guilds
- **Founder Privileges**: Guild creators manage members, descriptions, and settings
- **Guild Switching**: Change guilds with a 7-day cooldown
- **Single Guild Membership**: Players can only be in one guild at a time
- **Ban System**: Founders can ban problematic players from guilds
- **Member Management**: Kick members and view complete member lists

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

- **P** - Open guild management dialog

## Project Structure

```
PantheonWars/
├── CakeBuild/              # Build system
│   ├── Program.cs          # Build tasks and packaging
│   └── CakeBuild.csproj
├── docs/                   # Documentation
│   ├── README.md           # Documentation index
│   └── topics/             # Documentation organized by topic
├── PantheonWars/           # Main mod project
│   ├── Commands/           # Chat commands
│   │   └── Religion/       # Guild management commands
│   ├── Data/               # Data models for persistence
│   │   ├── ReligionData.cs
│   │   ├── PlayerReligionData.cs
│   │   └── BanEntry.cs
│   ├── GUI/                # User interface
│   │   ├── GuildManagementDialog.cs
│   │   ├── GuildDialogManager.cs
│   │   ├── GuildDialogEventHandlers.cs
│   │   └── UI/             # UI renderers and components
│   ├── Network/            # Client-server networking
│   │   ├── CreateReligionRequestPacket.cs
│   │   ├── PlayerReligionDataPacket.cs
│   │   ├── PlayerReligionInfoResponsePacket.cs
│   │   └── ReligionListResponsePacket.cs
│   ├── Systems/            # Core game systems
│   │   ├── ReligionManager.cs
│   │   ├── PlayerReligionDataManager.cs
│   │   └── Interfaces/
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

## Current Status (v2.0.0-alpha - Guild System Redesign)

The mod is undergoing a major redesign to simplify from a complex deity/blessing/progression system to a streamlined guild management system.

### Implemented Systems ✅

**Guild Management:**
- ✅ Create custom guilds (public or private)
- ✅ Join, leave, and switch guilds
- ✅ Founder privileges (kick members, disband, set description)
- ✅ 7-day switching cooldown
- ✅ Full persistence and save/load
- ✅ Guild Management GUI with modern interface
- ✅ Invitation system for private guilds
- ✅ Ban system for problematic players
- ✅ Member list viewing

### Available Commands

**Guild Management (13 commands):**
- `/religion create <name> [public/private]` - Create a new guild
- `/religion join <guildname>` - Join an existing guild
- `/religion leave` - Leave your current guild
- `/religion list` - List all public guilds
- `/religion info [name]` - View guild details
- `/religion members` - View members of your guild
- `/religion invite <playername>` - Invite a player to your guild (private guilds)
- `/religion kick <playername>` - Kick a member from your guild (founder only)
- `/religion disband` - Disband your guild (founder only)
- `/religion description <text>` - Set guild description (founder only)
- `/religion ban <playername> [reason] [days]` - Ban a player from your guild (founder only)
- `/religion unban <playername>` - Unban a player from your guild (founder only)
- `/religion banlist` - View banned players in your guild (founder only)

## Contributing

This mod is in active development. Contributions, suggestions, and feedback are welcome! Please open an issue or discussion on the repository.

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
