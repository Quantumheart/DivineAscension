#!/bin/bash
# Install Git hooks for DivineAscension
# Run this script from the repository root: ./tools/hooks/install-hooks.sh

set -e

# Determine the repository root
REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null)

if [ -z "$REPO_ROOT" ]; then
    echo "❌ Error: Not in a Git repository"
    exit 1
fi

HOOKS_SOURCE="$REPO_ROOT/tools/hooks"
HOOKS_TARGET="$REPO_ROOT/.git/hooks"

if [ ! -d "$HOOKS_SOURCE" ]; then
    echo "❌ Error: hooks directory not found at $HOOKS_SOURCE"
    exit 1
fi

echo "📦 Installing Git hooks for DivineAscension..."
echo "   Source: $HOOKS_SOURCE"
echo "   Target: $HOOKS_TARGET"
echo ""

# Install commit-msg hook
if [ -f "$HOOKS_SOURCE/commit-msg" ]; then
    cp "$HOOKS_SOURCE/commit-msg" "$HOOKS_TARGET/commit-msg"
    chmod +x "$HOOKS_TARGET/commit-msg"
    echo "✅ Installed: commit-msg (automatic version bumping)"
else
    echo "⚠️  Warning: commit-msg hook not found"
fi

# Disable old prepare-commit-msg if it exists
if [ -f "$HOOKS_TARGET/prepare-commit-msg" ]; then
    if ! grep -q "disabled" "$HOOKS_TARGET/prepare-commit-msg"; then
        mv "$HOOKS_TARGET/prepare-commit-msg" "$HOOKS_TARGET/prepare-commit-msg.disabled"
        echo "🔄 Disabled old prepare-commit-msg hook"
    fi
fi

echo ""
echo "✨ Git hooks installed successfully!"
echo ""
echo "💡 Tip: Use conventional commits for automatic versioning:"
echo "   fix:  your message  → patch bump (1.23.0 → 1.23.1)"
echo "   feat: your message  → minor bump (1.23.0 → 1.24.0)"
echo "   feat!: your message → major bump (1.23.0 → 2.0.0)"
echo ""
echo "📖 For more info: cat tools/hooks/README.md"
