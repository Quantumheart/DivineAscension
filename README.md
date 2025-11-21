# Guild Management System

**Version:** 2.0.0
**Status:** Stable

A streamlined guild management system for Vintage Story that lets players create and manage social groups.

## Overview

Guild Management System provides a simple yet powerful way for players to organize into guilds. Create public guilds that anyone can join, or private guilds that require invitations. Founders have full control over their guilds with kick, ban, and management privileges.

## Features

### Core Guild Management
- **Create Custom Guilds**: Name your guild and set it as public or private
- **Public & Private Guilds**: Control who can join your guild
- **Invitation System**: Invite specific players to join private guilds
- **Founder Privileges**: Guild creators have full management control
- **Member Management**: View all guild members and their roles
- **Guild Switching**: Change guilds with a 7-day cooldown period
- **Single Guild Membership**: Players can only be in one guild at a time

### Management Features
- **Kick Members**: Founders can remove members from the guild
- **Ban System**: Ban players from joining with optional expiry dates
- **Guild Descriptions**: Set custom descriptions for your guild
- **Disband Guilds**: Founders can permanently disband their guilds
- **Browse Guilds**: View all available guilds on the server

### User Interface
- **Quick Access**: Press **Ctrl+G** to open the Guild Management dialog
- **Tabbed Interface**: Browse guilds and manage your guild from one window
- **Create Guild Dialog**: Simple form for creating new guilds
- **Invite Dialog**: Easy player invitation system

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

- **Ctrl+G** - Open Guild Management dialog

## Project Structure

```
PantheonWars/
├── CakeBuild/              # Build system
│   ├── Program.cs          # Build tasks and packaging
│   └── CakeBuild.csproj
├── PantheonWars/           # Main mod project
│   ├── Commands/           # Chat commands
│   │   └── ReligionCommands.cs
│   ├── Data/               # Data models for persistence
│   │   ├── ReligionData.cs
│   │   └── PlayerReligionData.cs
│   ├── GUI/                # User interface
│   │   ├── ReligionManagementDialog.cs
│   │   ├── CreateReligionDialog.cs
│   │   ├── InvitePlayerDialog.cs
│   │   ├── EditDescriptionDialog.cs
│   │   └── BanPlayerDialog.cs
│   ├── Network/            # Client-server networking
│   │   ├── ReligionListRequestPacket.cs
│   │   ├── ReligionListResponsePacket.cs
│   │   ├── PlayerReligionInfoRequestPacket.cs
│   │   ├── PlayerReligionInfoResponsePacket.cs
│   │   ├── ReligionActionRequestPacket.cs
│   │   ├── ReligionActionResponsePacket.cs
│   │   ├── CreateReligionRequestPacket.cs
│   │   ├── CreateReligionResponsePacket.cs
│   │   ├── EditDescriptionRequestPacket.cs
│   │   ├── EditDescriptionResponsePacket.cs
│   │   ├── ReligionStateChangedPacket.cs
│   │   └── PlayerReligionDataPacket.cs
│   ├── Systems/            # Core systems
│   │   ├── ReligionManager.cs
│   │   └── PlayerReligionDataManager.cs
│   ├── modinfo.json        # Mod metadata
│   ├── PantheonWars.csproj
│   └── PantheonWarsSystem.cs
├── PantheonWars.Tests/     # Unit tests
├── Release/                # Build output
├── .gitignore
├── build.ps1               # Windows build script
├── build.sh                # Linux/Mac build script
├── PantheonWars.sln
└── README.md
```

## Available Commands

All commands use the `/religion` prefix:

### Guild Management Commands
- `/religion create <name> [public/private]` - Create a new guild (defaults to public)
- `/religion join <guildname>` - Join an existing public guild
- `/religion leave` - Leave your current guild
- `/religion list` - List all available guilds on the server
- `/religion info [name]` - View guild details (defaults to your guild)
- `/religion members` - View all members in your guild

### Founder-Only Commands
- `/religion invite <playername>` - Invite a player to your private guild
- `/religion kick <playername>` - Remove a member from your guild
- `/religion ban <playername> [reason] [days]` - Ban a player from joining (optional expiry)
- `/religion unban <playername>` - Remove a player from the ban list
- `/religion banlist` - View all banned players
- `/religion disband` - Permanently disband your guild
- `/religion description <text>` - Set your guild's description

## Guild Features Explained

### Public vs Private Guilds
- **Public Guilds**: Anyone can join using `/religion join <name>`
- **Private Guilds**: Only invited players can join

### Guild Switching Cooldown
- Players must wait 7 days between switching guilds
- Prevents guild hopping and maintains commitment
- Cooldown displayed when attempting to join a new guild

### Ban System
- Founders can ban players from joining their guild
- Bans can be permanent or temporary (specify days)
- Optional reason can be provided for transparency
- Banned players cannot join the guild even if invited

### Founder Privileges
The player who creates a guild has special permissions:
- Invite players to private guilds
- Kick members from the guild
- Ban and unban players
- Edit guild description
- Disband the guild

Founder status cannot be transferred and is tied to the original creator.

## Use Cases

### Social Organization
- Create friend groups for coordinated play
- Organize trading companies
- Form exploration parties
- Build community factions

### Server Management
- Clan/guild systems for PvP servers
- Town/settlement member tracking
- Alliance and faction management
- Community event organization

## Contributing

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
