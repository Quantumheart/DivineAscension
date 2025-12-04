# PantheonWars

Religion-based PvP systems for Vintage Story: custom religions, deities, dual progression (Favor/Prestige), and passive blessing trees. This repository contains the mod/library and a comprehensive xUnit test suite.

Status: Active development (last updated 2025-12-04). See docs for feature specifics and current plans.

## Build and Configuration

- Toolchain: .NET SDK 8.0 (C# 12)
- Solution: `PantheonWars.sln`
- Projects:
  - `PantheonWars` — main mod/library targeting the Vintage Story API
  - `PantheonWars.Tests` — xUnit v3 test project
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
  - `dotnet build PantheonWars.sln -c Debug`

Artifacts are placed under `Releases/` when using the Cake build. See the `Releases/` folder for example outputs.

## Testing

- Framework: xUnit v3 with `Microsoft.NET.Test.Sdk` 17.13 and `xunit.runner.visualstudio` 3.1.5
- Run all tests:
  - `dotnet test` (from repo root), or
  - `dotnet test PantheonWars.Tests/PantheonWars.Tests.csproj`
- Target a subset by fully qualified name (preferred):
  - `dotnet test --filter FullyQualifiedName~PantheonWars.Tests.GUI.UI.Utilities.DeityHelperTests`
- Coverage:
  - `./generate-coverage.sh` (auto-installs ReportGenerator and opens HTML report)
  - Manual: `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings`

Notes
- If `VINTAGE_STORY` is not set correctly, the test project will fail to build. See the dependency section above.
- The main project exposes internals to tests via `[assembly: InternalsVisibleTo("PantheonWars.Tests")]`.

## Documentation

- Documentation index: `docs/README.md`
- Common entry points:
  - Reference: `docs/topics/reference/` (deities, blessings, favor)
  - Implementation guides: `docs/topics/implementation/`
  - UI design: `docs/topics/ui-design/`
  - Testing: `docs/topics/testing/`

## Project structure (top-level)

```
PantheonWars/
├── PantheonWars/         # Main mod project
├── PantheonWars.Tests/   # Tests
├── CakeBuild/            # Build bootstrapper
├── docs/                 # Documentation
├── Releases/             # Example release artifacts
├── build.sh / build.ps1  # Convenience build scripts
└── PantheonWars.sln
```

## Troubleshooting

- Build errors about missing Vintage Story DLLs → verify `VINTAGE_STORY` and directory layout; restart your shell so `dotnet` inherits the env var.
- Coverage script doesn’t open the report → open `coverage-report/index.html` manually.
- Intermittent test failures tied to time/randomness → prefer injecting clocks and random sources.

## Contributing

- Match the existing C# style (C# 12, `ImplicitUsings`, `Nullable` enabled).
- Keep types `internal` when appropriate; tests already have access via `InternalsVisibleTo`.
- Place new tests under `PantheonWars.Tests/<Area>/` and align namespaces with folder structure.

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
- Inspired by the [Karma System mod](https://mods.vintagestory.at/show/mod/28955)
