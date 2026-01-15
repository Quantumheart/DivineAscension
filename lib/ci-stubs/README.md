# CI Stub Assemblies

This directory contains reference assemblies (stubs) for Vintage Story's proprietary DLLs. These stubs allow the project to build in CI environments without requiring a full Vintage Story installation.

## What are Reference Assemblies?

Reference assemblies contain only the public API surface (types, methods, properties) without any implementation code. They're legally safe to distribute because they contain no proprietary code—just interface definitions that allow compilation.

## Required Stub Files

The following stub assemblies are needed for CI builds:

- `VintagestoryAPI.dll` - Main Vintage Story modding API
- `VSEssentials.dll` - Essential game systems
- `VSSurvivalMod.dll` - Survival mode systems
- `cairo-sharp.dll` - Cairo graphics bindings

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

# Generate each stub
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/VintagestoryAPI.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Mods/VSEssentials.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Mods/VSSurvivalMod.dll"
refasmer -v --omit-non-api-members true -O ./lib/ci-stubs/ "$VINTAGE_STORY/Lib/cairo-sharp.dll"
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
