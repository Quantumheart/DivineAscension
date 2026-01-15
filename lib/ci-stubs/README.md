# CI Build Dependencies

This directory contains assemblies needed for CI builds without requiring a full Vintage Story installation.

## File Types

### Reference Assemblies (Stubs)
These contain only API signatures, no implementation code. Safe to distribute for proprietary assemblies.

### Full Assemblies
Open-source libraries copied from VS installation to guarantee exact version match.

## Required Files

**Stubs (generated with Refasmer):**
- `VintagestoryAPI.dll` - Main Vintage Story modding API
- `VSEssentials.dll` - Essential game systems
- `VSSurvivalMod.dll` - Survival mode systems
- `cairo-sharp.dll` - Cairo graphics bindings

**Full assemblies (copied from VS):**
- `protobuf-net.dll` - Protobuf serialization v2.4.9.1 (MIT license)

## Why Copy protobuf-net?

Instead of using a NuGet package, we copy the exact DLL from Vintage Story to:
- Guarantee version match (VS 1.21.6 uses 2.4.9.1)
- Avoid assembly version mismatches
- Ensure same behavior as runtime environment

## Generating Stubs

### Using Refasmer (Recommended)

[Refasmer](https://github.com/JetBrains/Refasmer) is a JetBrains tool that creates reference assemblies.

```bash
# Install Refasmer globally
dotnet tool install -g JetBrains.Refasmer.CliTool

# Generate stubs (run from project root)
./tools/generate-stubs.sh
```

### Manual Generation

If you prefer to generate stubs manually:

```bash
# Set your Vintage Story path
export VINTAGE_STORY="$HOME/.local/share/Vintagestory"

# Generate proprietary API stubs
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/VintagestoryAPI.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Mods/VSEssentials.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Mods/VSSurvivalMod.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Lib/cairo-sharp.dll"

# Copy open-source dependencies (full DLLs)
cp "$VINTAGE_STORY/Lib/protobuf-net.dll" ./lib/ci-stubs/
```

## Updating Stubs

When Vintage Story releases a new version with API changes:

1. Update your local Vintage Story installation
2. Re-run the stub generation script
3. Commit the updated stub DLLs
4. Test that CI builds pass

## Verifying Stubs Work

Test locally that the stubs work:

```bash
# Build using stubs instead of real DLLs
dotnet build -p:UseStubDependencies=true

# Run tests with stubs
dotnet test -p:UseStubDependencies=true
```

## Troubleshooting

**Build fails with missing types/methods:**
- The stubs may be outdated. Regenerate them from a current Vintage Story installation.

**Refasmer fails to process a DLL:**
- Ensure the DLL isn't corrupted
- Try with `-v` flag for verbose output
- Some DLLs with native dependencies may need special handling
