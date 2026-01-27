# Blessing Branching Implementation Plan

## Overview

Implement an **Exclusive Branches** system where blessings belong to named branches within each domain. Once a player unlocks a blessing from a branch, they are locked out of conflicting branches.

## Design Goals

1. **Meaningful Specialization**: Players must commit to a path, creating distinct builds
2. **Clear Feedback**: UI clearly shows which branches are available, committed, or locked
3. **Backward Compatibility**: Existing unlocked blessings are grandfathered in
4. **Per-Domain Branches**: Each deity domain has its own set of branches (not cross-domain)

## Branch Structure Example (Craft Domain)

```
Domain: Craft (Khoras)
├── Branch: Forge (crafting speed, tool durability)
│   ├── Craftsman's Touch (Tier 1)
│   ├── Masterwork Tools (Tier 2)
│   └── Legendary Smith (Tier 3)
│
├── Branch: Endurance (stamina, work duration)
│   ├── Forgeborn Endurance (Tier 1)
│   ├── Tireless Crafter (Tier 2)
│   └── Unyielding (Tier 3)
│
└── Branch: Shared (no exclusivity - available to all)
    └── Avatar of the Forge (Tier 4, capstone)

Exclusivity: Forge ←→ Endurance (mutually exclusive)
Shared branch: No conflicts, can be unlocked regardless of chosen branch
```

## Data Model Changes

### 1. Blessing Model (`Models/Blessing.cs`)

Add two new properties:

```csharp
/// <summary>
/// The branch this blessing belongs to within its domain.
/// Null or empty means "Shared" (no branch restrictions).
/// </summary>
public string? Branch { get; set; }

/// <summary>
/// List of branch names that become locked if this blessing is unlocked.
/// Only applies to the first blessing unlocked in a branch.
/// </summary>
public List<string>? ExclusiveBranches { get; set; }
```

### 2. Blessing DTO (`Models/Dto/BlessingJsonDto.cs`)

Add corresponding JSON properties:

```csharp
[JsonPropertyName("branch")]
public string? Branch { get; set; }

[JsonPropertyName("exclusiveBranches")]
public List<string>? ExclusiveBranches { get; set; }
```

### 3. PlayerProgressionData (`Systems/PlayerProgressionData.cs`)

Add branch commitment tracking:

```csharp
/// <summary>
/// Branches the player has committed to, keyed by domain.
/// Once a branch is committed, exclusive branches become locked.
/// </summary>
[ProtoMember(8)]
private Dictionary<DeityDomain, string> _committedBranches = new();

/// <summary>
/// Branches that are locked out due to exclusive branch choices.
/// Keyed by domain, value is set of locked branch names.
/// </summary>
[ProtoMember(9)]
private Dictionary<DeityDomain, HashSet<string>> _lockedBranches = new();

public bool IsBranchLocked(DeityDomain domain, string branch)
{
    lock (Lock)
    {
        return _lockedBranches.TryGetValue(domain, out var locked)
            && locked.Contains(branch);
    }
}

public string? GetCommittedBranch(DeityDomain domain)
{
    lock (Lock)
    {
        return _committedBranches.GetValueOrDefault(domain);
    }
}

public void CommitToBranch(DeityDomain domain, string branch, IEnumerable<string>? exclusiveBranches)
{
    lock (Lock)
    {
        // Only commit if not already committed to a branch in this domain
        if (_committedBranches.ContainsKey(domain))
            return;

        _committedBranches[domain] = branch;

        if (exclusiveBranches != null)
        {
            if (!_lockedBranches.ContainsKey(domain))
                _lockedBranches[domain] = new HashSet<string>();

            foreach (var excludedBranch in exclusiveBranches)
                _lockedBranches[domain].Add(excludedBranch);
        }
    }
}

public void ClearBranchCommitments()
{
    lock (Lock)
    {
        _committedBranches.Clear();
        _lockedBranches.Clear();
    }
}
```

### 4. BlessingNodeState (`Models/BlessingNodeState.cs`)

Add branch-related visual state:

```csharp
public enum BlessingNodeVisualState
{
    Locked,           // Prerequisites not met
    Unlockable,       // Can unlock (green glow)
    Unlocked,         // Already unlocked (gold)
    BranchLocked      // NEW: Branch is locked out (red/greyed, X overlay)
}
```

## JSON Schema Changes

### Blessing JSON Structure

Update `assets/divineascension/config/blessings/*.json`:

```json
{
  "domain": "Craft",
  "version": 2,
  "blessings": [
    {
      "blessingId": "khoras_craftsmans_touch",
      "name": "Craftsman's Touch",
      "branch": "Forge",
      "exclusiveBranches": ["Endurance"],
      "prerequisiteBlessings": [],
      ...
    },
    {
      "blessingId": "khoras_forgeborn_endurance",
      "name": "Forgeborn Endurance",
      "branch": "Endurance",
      "exclusiveBranches": ["Forge"],
      "prerequisiteBlessings": [],
      ...
    },
    {
      "blessingId": "khoras_avatar_of_forge",
      "name": "Avatar of the Forge",
      "branch": null,
      "exclusiveBranches": null,
      "prerequisiteBranches": [],
      ...
    }
  ]
}
```

### Branch Definitions (New File)

Create `assets/divineascension/config/branches.json`:

```json
{
  "version": 1,
  "domains": {
    "Craft": {
      "branches": [
        {
          "name": "Forge",
          "displayName": "Path of the Forge",
          "description": "Master the art of crafting and tool creation.",
          "iconName": "forge",
          "exclusiveWith": ["Endurance"]
        },
        {
          "name": "Endurance",
          "displayName": "Path of Endurance",
          "description": "Build stamina and tireless dedication.",
          "iconName": "endurance",
          "exclusiveWith": ["Forge"]
        }
      ]
    },
    "Wild": {
      "branches": [
        {
          "name": "Hunt",
          "displayName": "Path of the Hunt",
          "description": "Excel at tracking and slaying beasts.",
          "iconName": "hunt",
          "exclusiveWith": ["Harmony"]
        },
        {
          "name": "Harmony",
          "displayName": "Path of Harmony",
          "description": "Live in balance with nature's creatures.",
          "iconName": "harmony",
          "exclusiveWith": ["Hunt"]
        }
      ]
    }
  }
}
```

## Validation Changes

### BlessingRegistry (`Systems/BlessingRegistry.cs`)

Update `CanUnlockBlessing()` to check branch exclusivity:

```csharp
// After domain match check (around line 159), add:

// Check branch exclusivity
if (!string.IsNullOrEmpty(blessing.Branch))
{
    if (playerData.IsBranchLocked(blessing.Domain, blessing.Branch))
    {
        var committedBranch = playerData.GetCommittedBranch(blessing.Domain);
        return (false, $"Branch '{blessing.Branch}' is locked. You committed to '{committedBranch}'.");
    }
}
```

### BlessingCommands / BlessingNetworkHandler

After successful unlock, commit to branch:

```csharp
// After UnlockPlayerBlessing() call:

if (!string.IsNullOrEmpty(blessing.Branch))
{
    playerData.CommitToBranch(blessing.Domain, blessing.Branch, blessing.ExclusiveBranches);
}
```

## UI Changes

### 1. Tree Layout (`GUI/BlessingTreeLayout.cs`)

Modify layout to group blessings by branch:

```csharp
public void CalculateLayout(List<BlessingNodeState> nodes, float containerWidth)
{
    // Group by branch first, then by tier within branch
    var branchGroups = nodes
        .GroupBy(n => n.Blessing.Branch ?? "Shared")
        .OrderBy(g => g.Key == "Shared" ? 1 : 0) // Shared at end
        .ThenBy(g => g.Key)
        .ToList();

    float currentX = LeftPadding;

    foreach (var branchGroup in branchGroups)
    {
        // Layout this branch's nodes vertically by tier
        var tierGroups = branchGroup.GroupBy(n => n.Tier).OrderBy(g => g.Key);

        float branchWidth = CalculateBranchWidth(branchGroup);

        foreach (var tierGroup in tierGroups)
        {
            LayoutTierWithinBranch(tierGroup, currentX, branchWidth);
        }

        currentX += branchWidth + BranchSpacing;
    }
}

private const float BranchSpacing = 60f; // Gap between branches
```

### 2. Branch Headers in Tree Renderer

Add branch labels above each branch column:

```csharp
// In BlessingTreeRenderer, before drawing nodes:
private void DrawBranchHeaders(List<BranchLayoutInfo> branches, PlayerProgressionData playerData)
{
    foreach (var branch in branches)
    {
        var isCommitted = playerData.GetCommittedBranch(domain) == branch.Name;
        var isLocked = playerData.IsBranchLocked(domain, branch.Name);

        var headerColor = isLocked ? LockedBranchColor
                        : isCommitted ? CommittedBranchColor
                        : NeutralBranchColor;

        // Draw branch name header
        DrawText(branch.DisplayName, branch.CenterX, HeaderY, headerColor);

        // Draw lock icon if locked
        if (isLocked)
            DrawLockIcon(branch.CenterX, HeaderY);
    }
}
```

### 3. Node Visual State for Locked Branches

Update `BlessingNodeRenderer.cs`:

```csharp
private uint GetNodeColor(BlessingNodeState state)
{
    return state.VisualState switch
    {
        BlessingNodeVisualState.Unlocked => GoldColor,
        BlessingNodeVisualState.Unlockable => GreenColor,
        BlessingNodeVisualState.BranchLocked => LockedRedColor, // New: distinct from regular locked
        BlessingNodeVisualState.Locked => GreyColor,
        _ => GreyColor
    };
}

// Draw X overlay for branch-locked nodes
if (state.VisualState == BlessingNodeVisualState.BranchLocked)
{
    DrawXOverlay(nodePos, NodeSize, LockedRedColor);
}
```

### 4. Info Panel Updates

Show branch information in `BlessingInfoRenderer.cs`:

```csharp
// In requirements section:
if (!string.IsNullOrEmpty(blessing.Branch))
{
    DrawLabel("Branch:", blessing.Branch);

    if (blessing.ExclusiveBranches?.Any() == true)
    {
        DrawLabel("Locks out:", string.Join(", ", blessing.ExclusiveBranches));
    }
}
```

### 5. Tooltip Updates

Update `BlessingTooltipData.cs`:

```csharp
public string? Branch { get; set; }
public List<string>? ExclusiveBranches { get; set; }
public bool IsBranchLocked { get; set; }
public string? LockedByBranch { get; set; } // Which branch caused the lock
```

## Network Changes

### PlayerDataSyncPacket

Add branch state to sync packet:

```csharp
[ProtoMember(12)]
public Dictionary<int, string>? CommittedBranches { get; set; } // Domain enum as int

[ProtoMember(13)]
public Dictionary<int, List<string>>? LockedBranches { get; set; }
```

### Client State Updates

Ensure `DivineAscensionNetworkClient` updates local state with branch info.

## Migration Strategy

### Existing Players

Players with already-unlocked blessings need branch commitments inferred:

```csharp
public void MigrateBranchCommitments(PlayerProgressionData playerData, IBlessingRegistry registry)
{
    foreach (var blessingId in playerData.UnlockedBlessings)
    {
        var blessing = registry.GetBlessing(blessingId);
        if (blessing == null || string.IsNullOrEmpty(blessing.Branch))
            continue;

        // Commit to this branch if not already committed
        if (playerData.GetCommittedBranch(blessing.Domain) == null)
        {
            playerData.CommitToBranch(blessing.Domain, blessing.Branch, blessing.ExclusiveBranches);
        }
    }
}
```

Run this migration in `PlayerProgressionDataManager` during data load when `DataVersion < 4`.

### Data Version Bump

Increment `DataVersion` to 4 in `PlayerProgressionData`.

## Implementation Phases

### Phase 1: Data Model & Persistence
1. Add `Branch` and `ExclusiveBranches` to `Blessing` model
2. Add `BranchJsonDto` properties to DTO
3. Update `BlessingLoader` to parse new fields
4. Add branch tracking fields to `PlayerProgressionData`
5. Update `PlayerDataSyncPacket` with branch state
6. Write unit tests for branch commitment logic

### Phase 2: Validation & Unlock Logic
1. Update `BlessingRegistry.CanUnlockBlessing()` with branch checks
2. Update `BlessingCommands.Unlock()` to commit branches
3. Update `BlessingNetworkHandler` to commit branches
4. Add migration logic for existing players
5. Write integration tests for unlock flow with branches

### Phase 3: JSON Data
1. Create `branches.json` definition file
2. Update all 5 blessing JSON files with branch assignments
3. Design branch structure for each domain (2-3 branches per domain)
4. Test loading and validation

### Phase 4: UI - Layout
1. Update `BlessingTreeLayout` to group by branch
2. Add `BranchLayoutInfo` model for layout metadata
3. Calculate branch widths and positions
4. Add branch spacing constants

### Phase 5: UI - Rendering
1. Add `BranchLocked` visual state
2. Update `BlessingNodeRenderer` with locked branch styling
3. Add branch header rendering
4. Update info panel with branch details
5. Update tooltips with branch information

### Phase 6: Polish & Testing
1. Add branch icons to assets
2. Localization for branch names/descriptions
3. End-to-end testing of branch commitment flow
4. Test edge cases (respec, religion change, etc.)

## Edge Cases

### Religion Change
When a player leaves a religion and joins another with a different domain:
- Branch commitments are domain-specific
- If new religion has same domain, commitments persist
- If different domain, player starts fresh in that domain

### Admin Commands
Add admin commands for branch management:
- `/da admin blessing resetbranches <player>` - Clear all branch commitments
- `/da admin blessing setbranch <player> <domain> <branch>` - Force branch commitment

### Respec System (Future)
Consider a future respec mechanic:
- High favor cost to reset branch commitments
- Cooldown to prevent abuse
- May require losing unlocked blessings in the old branch

## Testing Checklist

- [ ] Branch commitment persists across save/load
- [ ] Branch commitment syncs to client correctly
- [ ] Cannot unlock blessing in locked branch
- [ ] Exclusive branches lock correctly on first unlock
- [ ] Shared/null branch blessings always unlockable
- [ ] Migration assigns branches to existing unlocks
- [ ] UI shows branch grouping correctly
- [ ] UI shows locked branch visual state
- [ ] Tooltip shows branch lock reason
- [ ] Admin commands work for branch reset

## Files to Modify

| File | Changes |
|------|---------|
| `Models/Blessing.cs` | Add Branch, ExclusiveBranches properties |
| `Models/Dto/BlessingJsonDto.cs` | Add JSON properties |
| `Models/BlessingNodeState.cs` | Add BranchLocked visual state |
| `Services/BlessingLoader.cs` | Parse new fields |
| `Systems/PlayerProgressionData.cs` | Add branch tracking |
| `Systems/BlessingRegistry.cs` | Add branch validation |
| `Commands/BlessingCommands.cs` | Commit branch on unlock |
| `Systems/Networking/Server/BlessingNetworkHandler.cs` | Commit branch on unlock |
| `Network/PlayerDataSyncPacket.cs` | Add branch sync fields |
| `GUI/BlessingTreeLayout.cs` | Group by branch |
| `GUI/UI/Renderers/Blessing/BlessingTreeRenderer.cs` | Branch headers |
| `GUI/UI/Renderers/Blessing/BlessingNodeRenderer.cs` | Locked branch styling |
| `GUI/UI/Renderers/Blessing/Info/BlessingInfoRenderer.cs` | Branch info display |
| `Models/BlessingTooltipData.cs` | Branch tooltip fields |
| `assets/divineascension/config/blessings/*.json` | Add branch data |
| `assets/divineascension/config/branches.json` | New file |

## Open Questions

1. **Multiple branches per domain?** Current plan assumes 2 exclusive branches + shared. Should we support 3+ with partial exclusivity (A excludes B, B excludes C, but A doesn't exclude C)?

2. **Religion blessings?** Should religion-level blessings also have branches, or only player blessings?

3. **Visual branch separators?** Should branches be separated by vertical lines in the UI, or just spacing?

4. **Branch selection UI?** Should there be an explicit "choose your path" dialog before first unlock, or implicit on first branch blessing unlock?
