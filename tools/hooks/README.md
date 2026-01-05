# Automatic Version Bumping Hook

This repository uses a Git `commit-msg` hook to automatically bump versions and update the changelog based on conventional commit messages.

## How It Works

When you commit with a conventional commit message, the hook will:
1. Detect the commit type (`feat`, `fix`, `BREAKING CHANGE`)
2. Bump the version semantically in `modinfo.json` and `AssemblyInfo.cs`
3. Add an entry to `CHANGELOG.md`
4. Stage the modified files so they're included in your commit

## Conventional Commit Format

Use these formats for your commit messages:

### Patch Bump (1.23.0 → 1.23.1)
```
fix: description of the bug fix
fix(scope): description with scope
```

### Minor Bump (1.23.0 → 1.24.0)
```
feat: description of new feature
feature: description of new feature
feat(scope): description with scope
```

### Major Bump (1.23.0 → 2.0.0)
```
feat!: breaking change description
fix!: breaking change description
BREAKING CHANGE: description
```

## Examples

```bash
# Patch bump - fixes a bug
git commit -m "fix: resolve religion membership desynchronization"
# Result: 1.23.0 → 1.23.1

# Minor bump - adds a feature
git commit -m "feat: add admin repair command for religion membership"
# Result: 1.23.0 → 1.24.0

# Major bump - breaking change
git commit -m "feat!: change religion data structure (breaks saves)"
# Result: 1.23.0 → 2.0.0
```

## What Gets Updated

1. **DivineAscension/modinfo.json**
   - `"version": "X.Y.Z"` field

2. **DivineAscension/Properties/AssemblyInfo.cs**
   - `Version = "X.Y.Z"` in ModInfo attribute

3. **CHANGELOG.md**
   - New version section with date
   - Categorized entry (Added/Fixed/Changed)

## Skipped Commits

The hook will NOT bump versions for:
- Merge commits
- Commits without conventional commit prefixes (e.g., `chore:`, `docs:`, `style:`)
- Commits that start with "Merge"

## Testing the Hook

You can test the hook manually:
```bash
echo "fix: test message" > /tmp/test-msg.txt
bash .git/hooks/commit-msg /tmp/test-msg.txt
```

Then restore files:
```bash
git checkout DivineAscension/modinfo.json DivineAscension/Properties/AssemblyInfo.cs CHANGELOG.md
```

## Troubleshooting

**Hook not running?**
- Ensure the hook is executable: `chmod +x .git/hooks/commit-msg`
- Check that you're using conventional commit format

**Wrong version bump?**
- Check your commit message format
- Use `feat:` for features, `fix:` for fixes, `!` or `BREAKING CHANGE:` for breaking changes

**Files not staged?**
- The hook automatically stages the version files
- Make sure you don't have them in .gitignore
