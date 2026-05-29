# Bad Actor Mitigation Review: Founders in Religions & Civilizations

**Review Date:** 2026-01-16
**Reviewer:** Claude Code Security Analysis
**Scope:** Religion and Civilization founder privilege systems

---

## Executive Summary

| Category | Grade | Risk Level |
|----------|-------|------------|
| **Overall** | **C+** | Medium-High |
| Religion Founder Controls | C | Medium-High |
| Civilization Founder Controls | B | Medium |
| Cascading Deletion Handling | A- | Low |
| Rate Limiting & Confirmation | F | Critical |
| Audit & Accountability | D | High |

---

## Detailed Findings

### 1. Religion Founder Controls

**Grade: C (Medium-High Risk)**

#### Strengths ✓

1. **Founder cannot voluntarily leave** - Must transfer ownership first
   - `ReligionCommands.cs:304-308` - Blocks leave command
   - `ReligionNetworkHandler.cs:1000-1005` - NET_RELIGION_FOUNDER_CANNOT_LEAVE

2. **Founder role is immutable** - Permissions cannot be modified
   - `RoleManager.cs:156-157` - "Cannot modify Founder role permissions"

3. **Cannot assign founder role arbitrarily** - Only via explicit transfer
   - `RoleManager.cs:189-190` - Blocks direct role assignment

4. **Automatic succession** - When founder is removed by admin, transfers to next member
   - `ReligionManager.cs:739-754` - Auto-transfer logic
   - If no members remain, religion auto-disbands (`ReligionManager.cs:215-222`)

#### Critical Vulnerabilities ✗

1. **CRITICAL: Founder can be kicked/banned by officers**

   The kick and ban handlers do NOT check if the target is the founder:

   ```csharp
   // ReligionNetworkHandler.cs:1023-1041 (Kick)
   // Only checks: HasPermission(KICK_MEMBERS) and "not self"
   // MISSING: Check if target is founder

   // ReligionNetworkHandler.cs:1067-1114 (Ban)
   // Only checks: HasPermission(BAN_PLAYERS) and "not self"
   // MISSING: Check if target is founder
   ```

   **Attack Vector:** Any player with KICK_MEMBERS or BAN_PLAYERS permission can remove the founder, causing automatic succession to themselves or an accomplice.

2. **Transfer requires no consent** - Founder can instantly transfer to any member
   - `RoleManager.cs:202-233` - No waiting period, no confirmation
   - Malicious founder can transfer to alt-account or accomplice

3. **Mass kick has no safeguards**
   - No rate limiting on consecutive kicks
   - Founder can empty entire religion in seconds

---

### 2. Civilization Founder Controls

**Grade: B (Medium Risk)**

#### Strengths ✓

1. **Explicit founder check for all sensitive operations**
   - `CivilizationManager.cs:531` - `if (civ.FounderUID != kickerUID)`
   - Only founder can kick religions, invite, disband

2. **Cannot kick own religion** - Must disband instead
   - `CivilizationManager.cs:551-556`

3. **Cannot leave as founder** - Must disband civilization
   - `CivilizationCommands.cs:308-311`
   - `CivilizationManager.cs:482-486`

#### Vulnerabilities ✗

1. **No civilization leadership transfer** - Founder is permanent
   - If founder's account is compromised, civilization is stuck
   - No emergency succession mechanism

2. **Founding religion deletion auto-disbands civilization**
   - `CivilizationManager.cs:98-104`
   - Malicious religion founder can dissolve entire civilization

---

### 3. Cascading Deletion Handling

**Grade: A- (Low Risk)**

#### Strengths ✓

1. **Proper event-driven cleanup** - No orphaned data
   - `ReligionManager.OnReligionDeleted` → `CivilizationManager.HandleReligionDeleted`
   - `CivilizationManager.OnCivilizationDisbanded` → `DiplomacyManager` cleanup

2. **Membership index consistency** - Player-to-religion index maintained
   - `ReligionManager.cs:550` - Index updated on ban
   - Initialization includes validation pass

3. **Automatic minimum enforcement**
   - Civilizations below MIN_RELIGIONS auto-disband (`CivilizationManager.cs:564-568`)

#### Minor Issues

1. **No "soft delete" or grace period** - Deletions are immediate and permanent
2. **No backup/restore mechanism** for accidentally disbanded organizations

---

### 4. Rate Limiting & Confirmation

**Grade: F (Critical Risk)**

#### Complete Absence of Safeguards

| Action | Rate Limit | Cooldown | Confirmation | Undo Period |
|--------|------------|----------|--------------|-------------|
| Kick Member | ✗ None | ✗ None | ✗ None | ✗ None |
| Ban Member | ✗ None | ✗ None | ✗ None | ✗ None |
| Disband Religion | ✗ None | ✗ None | ✗ None | ✗ None |
| Disband Civilization | ✗ None | ✗ None | ✗ None | ✗ None |
| Transfer Founder | ✗ None | ✗ None | ✗ None | ✗ None |

**Attack Vectors:**
- Compromised founder account can disband everything instantly
- Grief attack: Create religion, invite members, immediately disband
- Mass ban: Remove all members before anyone can react

---

### 5. Audit & Accountability

**Grade: D (High Risk)**

#### Partial Implementation

1. **ActivityLogManager exists** but only for favor/prestige activities
   - Does NOT log: kicks, bans, disbands, transfers, role changes

2. **No immutable audit log** for sensitive governance actions

3. **Server logs exist** but are not player-accessible
   - `ReligionManager.cs:556` - Logs bans to server console only

---

## Critical Security Issues Summary

### Issue #1: Founder Can Be Banned/Kicked (CRITICAL)

**Files:**
- `ReligionNetworkHandler.cs:1023-1065` (HandleKickAction)
- `ReligionNetworkHandler.cs:1067-1139` (HandleBanAction)

**Impact:** Complete subversion of founder protections. Any officer with kick/ban permissions can remove the founder.

**Recommended Fix:**
```csharp
// Add to HandleKickAction and HandleBanAction after permission check:
if (religion.IsFounder(packet.TargetPlayerUID))
    return new ReligionActionResult
    {
        Success = false,
        Message = "Cannot kick/ban the founder"
    };
```

### Issue #2: No Destructive Action Confirmation (HIGH)

**Impact:** Accidental or malicious instant destruction with no recovery.

**Recommended Fix:** Add confirmation dialogs for:
- Disband religion/civilization
- Mass kicks (>3 members)
- Founder transfer

### Issue #3: No Rate Limiting (HIGH)

**Impact:** Mass grief attacks possible in seconds.

**Recommended Fix:** Implement cooldowns:
- 30 seconds between kicks
- 60 seconds between bans
- 5 minute cooldown after disbanding

### Issue #4: Transfer Requires No Consent (MEDIUM)

**Impact:** Malicious transfer to accomplice or alt-account.

**Recommended Fix:**
- Require transferee to accept within 24 hours
- Broadcast notification to all members
- Add to audit log

---

## Comparison: Religion vs Civilization

| Protection | Religion | Civilization |
|------------|----------|--------------|
| Founder can leave | ✗ Blocked | ✗ Blocked |
| Founder can be kicked | ⚠️ **VULNERABLE** | ✓ Protected (N/A) |
| Founder can be banned | ⚠️ **VULNERABLE** | ✓ Protected (N/A) |
| Leadership transfer | ✓ Available | ✗ Not available |
| Immutable founder role | ✓ Yes | ✓ Yes |
| Auto-succession | ✓ On removal | ✗ None (disbands) |

---

## Recommendations by Priority

### P0 - Critical (Fix Immediately)
1. Add founder protection to kick/ban handlers
2. Add server-side validation that founders cannot be targeted

### P1 - High (Fix Soon)
3. Implement confirmation dialogs for destructive actions
4. Add rate limiting for member management actions
5. Create audit log for governance actions

### P2 - Medium (Plan for Future)
6. Add civilization leadership transfer mechanism
7. Implement transfer consent requirement
8. Add "grace period" before disbands take effect

### P3 - Low (Nice to Have)
9. Add activity feed entries for governance actions
10. Create admin tools for organization recovery
11. Implement inactivity detection for founders

---

## Appendix: Code References

| File | Lines | Description |
|------|-------|-------------|
| `ReligionManager.cs` | 523-558 | BanPlayer - missing founder check |
| `ReligionNetworkHandler.cs` | 1023-1065 | HandleKickAction - missing founder check |
| `ReligionNetworkHandler.cs` | 1067-1139 | HandleBanAction - missing founder check |
| `RoleManager.cs` | 202-233 | TransferFounder - no consent required |
| `CivilizationManager.cs` | 530-534 | KickReligion - proper founder check |
| `ReligionCommands.cs` | 304-308 | Founder leave prevention |
| `RoleManager.cs` | 184-186 | Founder demotion prevention |
