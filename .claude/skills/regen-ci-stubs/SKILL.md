---
name: regen-ci-stubs
description: Regenerate the Vintage Story reference assemblies in lib/ci-stubs/ that let CI build Divine Ascension without a VS install. Use after a Vintage Story version upgrade, when CI build fails with missing/changed VS API members, or when build.yml stubs are stale. Runs tools/generate-stubs.sh and validates the stub build.
---

# Regenerating CI stubs

CI (and any build with `UseStubDependencies=true`, auto-set when `CI=true`) does
not have a Vintage Story install. It compiles against trimmed reference
assemblies in `lib/ci-stubs/`, produced from the real VS DLLs by Refasmer.
After a VS upgrade these stubs go stale and CI fails with missing/changed API
members — regenerate and commit them.

Local builds (`UseStubDependencies` unset/false) use the game's bundled DLLs via
`$(VINTAGE_STORY)` instead; see `Directory.Build.props`.

## Prerequisites
- `VINTAGE_STORY` set to a real VS install containing `VintagestoryAPI.dll`,
  `Mods/VSEssentials.dll`, `Mods/VSSurvivalMod.dll`, `Lib/cairo-sharp.dll`.
  (In a cloud/CI container without VS this step **cannot run** — say so and stop;
  it must be done on a machine with the game installed.)
- Refasmer — the script auto-installs `JetBrains.Refasmer.CliTool` if missing,
  but you may need `export PATH="$PATH:$HOME/.dotnet/tools"`.

## Steps
1. Confirm the install:
   ```bash
   echo "$VINTAGE_STORY" && ls "$VINTAGE_STORY/VintagestoryAPI.dll"
   ```
2. Generate:
   ```bash
   ./tools/generate-stubs.sh
   ```
   It writes refasm'd `VintagestoryAPI.dll`, `VSEssentials.dll`,
   `VSSurvivalMod.dll`, `cairo-sharp.dll` into `lib/ci-stubs/`. The script exits
   non-zero if any DLL was missing/failed — fix the path or the missing DLL, don't
   commit a partial set.
3. Validate the **stub** build path (what CI actually does):
   ```bash
   dotnet build DivineAscension.sln -c Debug -p:UseStubDependencies=true
   ```
   Also run a normal `dotnet build` + `dotnet test` to confirm nothing regressed.
4. Commit only the stub DLLs:
   ```bash
   git add lib/ci-stubs/*.dll
   git commit -m "build: regenerate CI stubs for VS <version>"
   ```
   (Conventional-commit `build:` type — commitlint/husky enforces this.)

## Notes
- If only one DLL's API changed, the script still regenerates all four — that's fine; commit them together so the set stays consistent.
- This is the fix referenced in CLAUDE.md troubleshooting: "CI stub build failures → regenerate `lib/ci-stubs/` after VS upgrades."
- If you also bumped the VS API version, update any version references (csproj/props) in the same change.

## Checklist
- [ ] `VINTAGE_STORY` points at a real install with all four DLLs.
- [ ] `tools/generate-stubs.sh` exited 0 (no NOT FOUND / FAILED).
- [ ] Stub build passes: `dotnet build -p:UseStubDependencies=true`.
- [ ] Normal build + tests pass.
- [ ] Only `lib/ci-stubs/*.dll` committed, `build:`-typed message.
