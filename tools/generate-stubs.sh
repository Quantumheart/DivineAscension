#!/bin/bash
#
# Generate reference assemblies (stubs) for Vintage Story DLLs.
# These stubs enable CI builds without requiring a Vintage Story installation.
#
# Prerequisites:
#   - dotnet tool install -g JetBrains.Refasmer.CliTool
#   - VINTAGE_STORY environment variable set to Vintage Story installation path
#
# Usage:
#   ./tools/generate-stubs.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_ROOT/lib/ci-stubs"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=== Vintage Story Stub Generator ==="
echo ""

# Check for VINTAGE_STORY environment variable
if [ -z "$VINTAGE_STORY" ]; then
    echo -e "${RED}Error: VINTAGE_STORY environment variable is not set.${NC}"
    echo ""
    echo "Please set it to your Vintage Story installation directory:"
    echo "  export VINTAGE_STORY=\"\$HOME/.local/share/Vintagestory\""
    exit 1
fi

if [ ! -d "$VINTAGE_STORY" ]; then
    echo -e "${RED}Error: VINTAGE_STORY directory does not exist: $VINTAGE_STORY${NC}"
    exit 1
fi

# Check for Refasmer
if ! command -v refasmer &> /dev/null; then
    echo -e "${YELLOW}Refasmer not found. Installing...${NC}"
    dotnet tool install -g JetBrains.Refasmer.CliTool

    # Add .NET tools to PATH for this session
    export PATH="$PATH:$HOME/.dotnet/tools"

    if ! command -v refasmer &> /dev/null; then
        echo -e "${RED}Error: Failed to install Refasmer.${NC}"
        echo "Try installing manually: dotnet tool install -g JetBrains.Refasmer.CliTool"
        exit 1
    fi
fi

echo "Using Vintage Story installation: $VINTAGE_STORY"
echo "Output directory: $OUTPUT_DIR"
echo ""

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Define DLLs to process
declare -A DLLS=(
    ["VintagestoryAPI.dll"]="$VINTAGE_STORY/VintagestoryAPI.dll"
    ["VSEssentials.dll"]="$VINTAGE_STORY/Mods/VSEssentials.dll"
    ["VSSurvivalMod.dll"]="$VINTAGE_STORY/Mods/VSSurvivalMod.dll"
    ["cairo-sharp.dll"]="$VINTAGE_STORY/Lib/cairo-sharp.dll"
)

FAILED=0
SUCCEEDED=0

for dll_name in "${!DLLS[@]}"; do
    source_path="${DLLS[$dll_name]}"

    echo -n "Processing $dll_name... "

    if [ ! -f "$source_path" ]; then
        echo -e "${RED}NOT FOUND${NC}"
        echo "  Expected at: $source_path"
        FAILED=$((FAILED + 1))
        continue
    fi

    if refasmer -v -O "$OUTPUT_DIR/" "$source_path" > /dev/null 2>&1; then
        echo -e "${GREEN}OK${NC}"
        SUCCEEDED=$((SUCCEEDED + 1))
    else
        echo -e "${RED}FAILED${NC}"
        echo "  Running with verbose output:"
        refasmer -v -O "$OUTPUT_DIR/" "$source_path" || true
        FAILED=$((FAILED + 1))
    fi
done

echo ""
echo "=== Summary ==="
echo -e "Succeeded: ${GREEN}$SUCCEEDED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -gt 0 ]; then
    echo -e "${YELLOW}Warning: Some stubs failed to generate.${NC}"
    echo "CI builds may fail until all stubs are present."
    exit 1
fi

echo -e "${GREEN}All stubs generated successfully!${NC}"
echo ""
echo "Next steps:"
echo "  1. Test the build: dotnet build -p:UseStubDependencies=true"
echo "  2. Commit the stubs: git add lib/ci-stubs/*.dll"
echo ""
