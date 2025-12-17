# Divine Ascension

Deity-driven religion and faction systems for Vintage Story: create and manage religions, earn player Favor and Religion Prestige, unlock blessing trees, and organize multi‑religion civilizations. This repository contains the mod/library and an extensive xUnit test suite.

Status: Active development (last updated 2025-12-04). See docs for feature specifics and current plans.

## Feature Summary

- Religions and Deities
  - Create, browse, join/leave player‑run religions associated with a deity; server‑authoritative managers and command support.
- Dual progression: Favor and Religion Prestige
  - Player Favor and Religion Prestige ranks with thresholds; rewards from activities and PvP; configurable death penalties; passive Favor accumulation with multipliers.
- Blessing trees and effects
  - Unlockable blessing nodes with prerequisites and categories; server‑validated unlocks; effects applied via the Buff system; in‑game tree layout and icon rendering.
- Civilizations (multi‑religion alliances)
  - Create, browse, and manage civilizations; invite/accept/kick/disband flows; UI tabs and server commands; religion‑level membership.
- PvP integration
  - Kill rewards and death penalties integrated with Favor/Prestige.
- Networking and persistence
  - Protobuf‑net packets for religion, player, blessing, and civilization data; server→client sync on join and on state changes.
- UI
  - Blessing dialog with tooltips, tree layout, and civilization management views; consistent color palette and icon utilities.
- Test suite
  - Large xUnit v3 test project covering systems, GUI utilities, networking, and models.

In progress (see docs/topics/integration/): shrine/prayer mechanics and land‑claim holy‑site bonuses.

## Build and Configuration

- Toolchain: .NET SDK 8.0 (C# 12)
- Solution: `DivineAscension.sln`
- Projects:
  - `DivineAscension` — main mod/library targeting the Vintage Story API
  - `DivineAscension.Tests` — xUnit v3 test project
  - `CakeBuild` — C# Cake bootstrapper used by `build.sh`/`build.ps1`

### Vintage Story dependency

Tests and (optionally) the main project reference Vintage Story game libraries via the `VINTAGE_STORY` environment variable. Point it to a local Vintage Story install that contains at minimum:

- `VintagestoryAPI.dll`
- `Lib/0Harmony.dll`, `Lib/cairo-sharp.dll`, `Lib/Newtonsoft.Json.dll`, `Lib/protobuf-net.dll`
- `Mods/VSEssentials.dll`, `Mods/VSSurvivalMod.dll`

Examples:

Windows PowerShell
```powershell
$env:VINTAGE_STORY = "C:\\Games\\Vintagestory"
```

Linux/macOS
```bash
export VINTAGE_STORY="$HOME/.local/share/Vintagestory"
```

### Quick build

- Full Cake build (packaging):
  - Linux/macOS: `./build.sh`
  - Windows: `./build.ps1`

- Direct solution build:
  - `dotnet build DivineAscension.sln -c Debug`

Artifacts are placed under `Releases/` when using the Cake build. See the `Releases/` folder for example outputs.

## Testing

- Framework: xUnit v3 with `Microsoft.NET.Test.Sdk` 17.13 and `xunit.runner.visualstudio` 3.1.5
- Run all tests:
  - `dotnet test` (from repo root), or
  - `dotnet test DivineAscension.Tests/DivineAscension.Tests.csproj`
- Target a subset by fully qualified name (preferred):
  - `dotnet test --filter FullyQualifiedName~DivineAscension.Tests.GUI.UI.Utilities.DeityHelperTests`
- Coverage:
  - `./generate-coverage.sh` (auto-installs ReportGenerator and opens HTML report)
  - Manual: `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings`

Notes
- If `VINTAGE_STORY` is not set correctly, the test project will fail to build. See the dependency section above.
- The main project exposes internals to tests via `[assembly: InternalsVisibleTo("DivineAscension.Tests")]`.

## Documentation

- Documentation index: `docs/README.md`
- Common entry points:
  - Reference: `docs/topics/reference/` (deities, blessings, favor)
  - Implementation guides: `docs/topics/implementation/`
  - UI design: `docs/topics/ui-design/`
  - Testing: `docs/topics/testing/`

## Project structure (top-level)

```
DivineAscension/
├── DivineAscension/         # Main mod project
├── DivineAscension.Tests/   # Tests
├── CakeBuild/               # Build bootstrapper
├── docs/                    # Documentation
├── Releases/                # Example release artifacts
├── build.sh / build.ps1     # Convenience build scripts
└── DivineAscension.sln
```

## Troubleshooting

- Build errors about missing Vintage Story DLLs → verify `VINTAGE_STORY` and directory layout; restart your shell so `dotnet` inherits the env var.
- Coverage script doesn’t open the report → open `coverage-report/index.html` manually.
- Intermittent test failures tied to time/randomness → prefer injecting clocks and random sources.

## Contributing

- Match the existing C# style (C# 12, `ImplicitUsings`, `Nullable` enabled).
- Keep types `internal` when appropriate; tests already have access via `InternalsVisibleTo`.
- Place new tests under `DivineAscension.Tests/<Area>/` and align namespaces with folder structure.

## License

See `LICENSE` in the repository root.

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
- Heavily inspired by mods: 
  - [Karma System mod](https://mods.vintagestory.at/show/mod/28955)
  - [xSkills Gilded](https://mods.vintagestory.at/show/mod/26936)
  - [xSkills](https://mods.vintagestory.at/show/mod/247)