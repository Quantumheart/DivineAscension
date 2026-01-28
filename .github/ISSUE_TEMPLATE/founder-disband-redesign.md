---
name: Founder Disband Redesign
about: Replace instant disband with force-transfer design to protect members
title: "[Security] Redesign founder disband to require transfer when members exist"
labels: security, enhancement, breaking-change
assignees: ''
---

## Problem Statement

The current founder disband functionality is too powerful. A single founder can instantly delete a religion with many members, destroying everyone's progress with no recourse.

**Current behavior:**
- Founder can disband religion instantly via `/religion disband` or UI
- All members lose membership, favor progress, and unlocked blessings immediately
- Cascades to civilization dissolution if founding religion
- No confirmation, no waiting period, no member protection

**Risk scenarios:**
| Scenario | Impact |
|----------|--------|
| Founder account compromised | Instant total loss for all members |
| Founder grief attack | Create → invite → disband |
| Founder rage quit | Destroys community without warning |
| Accidental disband | No undo mechanism |

## Proposed Solution

Replace instant disband with a **force-transfer** model that protects member investment while preserving founder autonomy.

### New Rules

1. **Solo founder** (only member) → Can disband freely
2. **Founder with members** → Must transfer or abandon before leaving
3. **Remove disband option** when other members exist

### New Commands

```
/religion transfer <player>  - Explicit transfer to specific member (existing)
/religion abandon            - Auto-transfer to longest-tenured member, then leave (NEW)
```

### Behavior Matrix

| Situation | Action | Result |
|-----------|--------|--------|
| Solo founder disbands | Allowed | Religion deleted |
| Founder with members disbands | **Blocked** | Error: "Must transfer or abandon" |
| Founder transfers to member | Allowed | New founder, old founder demoted |
| Founder abandons | Allowed | Auto-transfer to first member, founder leaves |
| Last member leaves after founder abandoned | Auto-disband | Religion deleted |

## Implementation Plan

### Phase 1: Block Disband with Members

- [ ] Modify `ReligionManager.DeleteReligion()` to reject when `MemberUIDs.Count > 1`
- [ ] Update `ReligionNetworkHandler.HandleDisbandAction()` with same check
- [ ] Update `ReligionCommands.OnDisbandReligion()` with same check
- [ ] Return clear error message directing to transfer/abandon

**Files:**
- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Commands/ReligionCommands.cs`

### Phase 2: Implement Abandon Command

- [ ] Add `AbandonFoundership()` method to `ReligionManager`
- [ ] Add `/religion abandon` command handler
- [ ] Add `ReligionAbandonRequest` packet type
- [ ] Add network handler for abandon requests
- [ ] Notify new founder of their new status

**New files:**
- `DivineAscension/Network/ReligionAbandonPacket.cs` (if needed)

**Modified files:**
- `DivineAscension/Systems/ReligionManager.cs`
- `DivineAscension/Commands/ReligionCommands.cs`
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`

### Phase 3: Update UI

- [ ] Hide/disable disband button when members exist
- [ ] Add abandon button as alternative
- [ ] Show confirmation dialog for abandon action
- [ ] Update tooltips to explain new behavior

**Files:**
- `DivineAscension/GUI/UI/Renderers/Religion/ReligionInfoRenderer.cs`
- Related state managers

### Phase 4: Testing

- [ ] Unit test: Solo founder can disband
- [ ] Unit test: Founder with members cannot disband
- [ ] Unit test: Abandon transfers to correct member
- [ ] Unit test: Abandon removes founder from religion
- [ ] Unit test: New founder receives notification
- [ ] Integration test: Full abandon flow via network

## API Changes

### Breaking Changes

```csharp
// Before: Always succeeds for founder
religionManager.DeleteReligion(religionId, founderUID);

// After: Fails if members exist
// Returns (false, "Cannot disband religion with members. Use abandon or transfer.")
```

### New Methods

```csharp
// ReligionManager.cs
public (bool success, string error) AbandonFoundership(string religionId, string founderUID);

// Returns successor's UID on success, null on failure
public string? GetSuccessor(string religionId, string currentFounderUID);
```

## Migration

No data migration required. This is a behavioral change only.

## Documentation Updates

- [ ] Update `/religion` command help text
- [ ] Add `/religion abandon` to command reference
- [ ] Update founder privileges documentation
- [ ] Add migration note to changelog

## Acceptance Criteria

- [ ] Solo founder can still disband their religion
- [ ] Founder with 2+ members cannot disband
- [ ] Founder can abandon, transferring to longest-tenured member
- [ ] New founder is notified of status change
- [ ] UI reflects new behavior (disband hidden/disabled when members exist)
- [ ] All existing tests pass
- [ ] New tests cover abandon functionality

## Related Issues

- Fixes founder account compromise vulnerability
- Addresses grief attack vector
- Part of bad actor mitigation improvements

## References

- Security review: `docs/reviews/bad-actor-mitigation-review.md`
- Current disband implementation: `ReligionManager.cs:476-520`
- Current transfer implementation: `RoleManager.cs:202-233`
