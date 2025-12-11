# Ban Kicked Members Feature - Implementation Plan

**Date**: 2025-11-13
**Branch**: `claude/ban-kicked-members-feature-011CV6HbsJBgedQBdvfC88s7`
**Status**: Planning Phase

---

## Executive Summary

This document outlines the implementation plan for adding a **ban system** to the PantheonWars religion feature. Currently, the system supports kicking members from a religion, but kicked members can immediately rejoin. This feature will add the ability for religion founders to permanently ban players, preventing them from rejoining the religion.

### Current State
- ✅ Kick functionality exists (implemented in ReligionCommands, PantheonWarsSystem)
- ✅ Members can be removed from religions
- ✅ Notifications sent to kicked members
- ❌ No ban/blacklist system
- ❌ No restriction on rejoining after being kicked

### Proposed Solution
Implement a comprehensive ban system that:
- Allows founders to ban members (with optional reason and expiration)
- Prevents banned players from rejoining
- Provides ban management UI for founders
- Includes unban functionality
- Tracks ban history with timestamps and reasons

---

## Architecture Overview

### Data Flow
```
Founder clicks "Ban" → ReligionManagementDialog → ReligionActionRequestPacket("ban")
    ↓
PantheonWarsSystem validates founder permission
    ↓
ReligionManager.BanMember() → Updates ReligionData
    ↓
Kicked player removed + added to ban list
    ↓
Response + notifications sent to both players
```

### Validation Flow for Join Attempts
```
Player attempts to join religion
    ↓
ReligionManager.CanJoinReligion()
    ↓
Check if player is banned → IsBanned(religionUID, playerUID)
    ↓
If banned: Return false with reason
If not banned: Continue normal validation
```

---

## Implementation Tasks

### Phase 1: Data Model Updates

#### Task 1.1: Update ReligionData.cs
**File**: `PantheonWars/Data/ReligionData.cs`

**Changes**:
1. Add new properties:
   ```csharp
   public Dictionary<string, BanEntry> BannedPlayers { get; set; } = new();
   ```

2. Add BanEntry class:
   ```csharp
   public class BanEntry
   {
       public string PlayerUID { get; set; } = string.Empty;
       public string Reason { get; set; } = "No reason provided";
       public DateTime BannedAt { get; set; } = DateTime.UtcNow;
       public DateTime? ExpiresAt { get; set; } = null;
       public string BannedByUID { get; set; } = string.Empty;
   }
   ```

3. Add methods:
   - `AddBannedPlayer(string playerUID, BanEntry entry)`
   - `RemoveBannedPlayer(string playerUID)`
   - `IsBanned(string playerUID)`
   - `GetBannedPlayer(string playerUID)`
   - `GetActiveBans()` - Returns bans that haven't expired
   - `CleanupExpiredBans()` - Removes expired bans

**Testing**: Unit tests for ban entry management

---

#### Task 1.2: Add Ban Network Packets
**Location**: `PantheonWars/Network/`

**Option A**: Extend existing packets
- Modify `ReligionActionRequestPacket` to support "ban" and "unban" actions
- Add optional fields: `string Reason`, `int? ExpiryDays`

**Option B**: Create new packets (Recommended)
- Create `BanPlayerPacket.cs`:
  ```csharp
  public class BanPlayerPacket
  {
      public string ReligionUID { get; set; }
      public string TargetPlayerUID { get; set; }
      public string Reason { get; set; } = "";
      public int? ExpiryDays { get; set; } = null;
  }
  ```
- Create `UnbanPlayerPacket.cs`:
  ```csharp
  public class UnbanPlayerPacket
  {
      public string ReligionUID { get; set; }
      public string TargetPlayerUID { get; set; }
  }
  ```

**Testing**: Serialization/deserialization tests

---

### Phase 2: Manager Layer Updates

#### Task 2.1: Update ReligionManager.cs
**File**: `PantheonWars/Systems/ReligionManager.cs`

**New Methods**:
```csharp
// Ban a player from the religion
public bool BanPlayer(string religionUID, string playerUID, string bannedByUID,
                      string reason = "", int? expiryDays = null)
{
    var religion = GetReligion(religionUID);
    if (religion == null) return false;

    var banEntry = new BanEntry
    {
        PlayerUID = playerUID,
        Reason = reason,
        BannedAt = DateTime.UtcNow,
        ExpiresAt = expiryDays.HasValue
            ? DateTime.UtcNow.AddDays(expiryDays.Value)
            : null,
        BannedByUID = bannedByUID
    };

    religion.AddBannedPlayer(playerUID, banEntry);
    MarkDirty(religionUID);
    return true;
}

// Unban a player
public bool UnbanPlayer(string religionUID, string playerUID)
{
    var religion = GetReligion(religionUID);
    if (religion == null) return false;

    religion.RemoveBannedPlayer(playerUID);
    MarkDirty(religionUID);
    return true;
}

// Check if player is banned
public bool IsBanned(string religionUID, string playerUID)
{
    var religion = GetReligion(religionUID);
    if (religion == null) return false;

    religion.CleanupExpiredBans();
    return religion.IsBanned(playerUID);
}

// Get ban details
public BanEntry? GetBanDetails(string religionUID, string playerUID)
{
    var religion = GetReligion(religionUID);
    if (religion == null) return null;

    religion.CleanupExpiredBans();
    return religion.GetBannedPlayer(playerUID);
}

// Get all active bans for a religion
public List<BanEntry> GetBannedPlayers(string religionUID)
{
    var religion = GetReligion(religionUID);
    if (religion == null) return new List<BanEntry>();

    religion.CleanupExpiredBans();
    return religion.GetActiveBans();
}
```

**Modified Methods**:
```csharp
// Update CanJoinReligion to check ban status
public bool CanJoinReligion(string religionUID, string playerUID)
{
    // ... existing validation ...

    // NEW: Check if player is banned
    if (IsBanned(religionUID, playerUID))
    {
        return false;
    }

    // ... rest of existing logic ...
}
```

**Testing**:
- Unit tests for ban/unban operations
- Test ban expiration logic
- Test CanJoinReligion with banned players

---

#### Task 2.2: Add Ban Actions to PantheonWarsSystem.cs
**File**: `PantheonWars/PantheonWarsSystem.cs`

**Changes**: Add new cases to `OnReligionActionRequest` or create new handler:

```csharp
case "ban":
    var religionForBan = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
    if (religionForBan != null && religionForBan.IsFounder(fromPlayer.PlayerUID))
    {
        if (packet.TargetPlayerUID != fromPlayer.PlayerUID)
        {
            // First kick the player if they're still a member
            if (religionForBan.IsMember(packet.TargetPlayerUID))
            {
                _playerReligionDataManager!.LeaveReligion(packet.TargetPlayerUID);
            }

            // Then ban them
            string reason = packet.Data?.ContainsKey("Reason") == true
                ? packet.Data["Reason"].ToString() ?? ""
                : "No reason provided";
            int? expiryDays = packet.Data?.ContainsKey("ExpiryDays") == true
                ? int.Parse(packet.Data["ExpiryDays"].ToString() ?? "0")
                : null;

            _religionManager!.BanPlayer(
                religionForBan.ReligionUID,
                packet.TargetPlayerUID,
                fromPlayer.PlayerUID,
                reason,
                expiryDays
            );

            message = $"Player has been banned from the religion. Reason: {reason}";
            success = true;

            // Notify banned player if online
            var bannedPlayer = _sapi!.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
            if (bannedPlayer != null)
            {
                bannedPlayer.SendMessage(0,
                    $"You have been banned from {religionForBan.ReligionName}. Reason: {reason}",
                    EnumChatType.Notification);
            }
        }
        else
        {
            message = "You cannot ban yourself.";
        }
    }
    else
    {
        message = "Only the founder can ban members.";
    }
    break;

case "unban":
    var religionForUnban = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
    if (religionForUnban != null && religionForUnban.IsFounder(fromPlayer.PlayerUID))
    {
        if (_religionManager!.UnbanPlayer(religionForUnban.ReligionUID, packet.TargetPlayerUID))
        {
            message = "Player has been unbanned.";
            success = true;
        }
        else
        {
            message = "Failed to unban player.";
        }
    }
    else
    {
        message = "Only the founder can unban players.";
    }
    break;
```

**Testing**: Integration tests for server-side ban handling

---

### Phase 3: Command Updates

#### Task 3.1: Add Ban Commands to ReligionCommands.cs
**File**: `PantheonWars/Commands/ReligionCommands.cs`

**New Commands**:
```csharp
// /religion ban <playername> [reason] [days]
private TextCommandResult OnBanPlayer(TextCommandCallingArgs args)
{
    var targetPlayerName = (string)args[0];
    var reason = args.Length > 1 ? (string)args[1] : "No reason provided";
    int? expiryDays = args.Length > 2 ? int.Parse((string)args[2]) : null;

    var player = args.Caller.Player as IServerPlayer;
    var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

    if (!playerData.HasReligion())
        return TextCommandResult.Error("You are not in any religion");

    var religion = _religionManager.GetReligion(playerData.ReligionUID!);

    if (!religion.IsFounder(player.PlayerUID))
        return TextCommandResult.Error("Only the founder can ban members");

    var targetPlayer = _sapi.World.AllPlayers
        .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

    if (targetPlayer == null)
        return TextCommandResult.Error($"Player {targetPlayerName} not found");

    if (targetPlayer.PlayerUID == player.PlayerUID)
        return TextCommandResult.Error("You cannot ban yourself");

    // Kick if member
    if (religion.IsMember(targetPlayer.PlayerUID))
    {
        _playerReligionDataManager.LeaveReligion(targetPlayer.PlayerUID);
    }

    // Ban the player
    _religionManager.BanPlayer(
        religion.ReligionUID,
        targetPlayer.PlayerUID,
        player.PlayerUID,
        reason,
        expiryDays
    );

    // Notify
    var targetServerPlayer = targetPlayer as IServerPlayer;
    if (targetServerPlayer != null)
    {
        targetServerPlayer.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"You have been banned from {religion.ReligionName}. Reason: {reason}",
            EnumChatType.Notification
        );
    }

    string expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
    return TextCommandResult.Success(
        $"{targetPlayerName} has been banned from {religion.ReligionName}{expiryText}"
    );
}

// /religion unban <playername>
private TextCommandResult OnUnbanPlayer(TextCommandCallingArgs args)
{
    var targetPlayerName = (string)args[0];
    var player = args.Caller.Player as IServerPlayer;

    var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

    if (!playerData.HasReligion())
        return TextCommandResult.Error("You are not in any religion");

    var religion = _religionManager.GetReligion(playerData.ReligionUID!);

    if (!religion.IsFounder(player.PlayerUID))
        return TextCommandResult.Error("Only the founder can unban players");

    var targetPlayer = _sapi.World.AllPlayers
        .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

    if (targetPlayer == null)
        return TextCommandResult.Error($"Player {targetPlayerName} not found");

    if (_religionManager.UnbanPlayer(religion.ReligionUID, targetPlayer.PlayerUID))
    {
        return TextCommandResult.Success($"{targetPlayerName} has been unbanned");
    }
    else
    {
        return TextCommandResult.Error($"{targetPlayerName} is not banned");
    }
}

// /religion banlist - Show all banned players
private TextCommandResult OnListBannedPlayers(TextCommandCallingArgs args)
{
    var player = args.Caller.Player as IServerPlayer;
    var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

    if (!playerData.HasReligion())
        return TextCommandResult.Error("You are not in any religion");

    var religion = _religionManager.GetReligion(playerData.ReligionUID!);

    if (!religion.IsFounder(player.PlayerUID))
        return TextCommandResult.Error("Only the founder can view the ban list");

    var bannedPlayers = _religionManager.GetBannedPlayers(religion.ReligionUID);

    if (bannedPlayers.Count == 0)
        return TextCommandResult.Success("No banned players");

    var sb = new StringBuilder();
    sb.AppendLine($"Banned players in {religion.ReligionName}:");

    foreach (var ban in bannedPlayers)
    {
        var playerName = _sapi.World.PlayerByUid(ban.PlayerUID)?.PlayerName ?? "Unknown";
        var expiry = ban.ExpiresAt.HasValue
            ? $"expires {ban.ExpiresAt.Value:yyyy-MM-dd}"
            : "permanent";
        sb.AppendLine($"- {playerName}: {ban.Reason} ({expiry})");
    }

    return TextCommandResult.Success(sb.ToString());
}
```

**Command Registration**: Update command registration in constructor:
```csharp
.BeginSubCommand("ban")
    .WithArgs(api.ChatCommands.Parsers.Word("playername"))
    .WithArgs(api.ChatCommands.Parsers.OptionalWord("reason"))
    .WithArgs(api.ChatCommands.Parsers.OptionalInt("days"))
    .HandleWith(OnBanPlayer)
.EndSubCommand()
.BeginSubCommand("unban")
    .WithArgs(api.ChatCommands.Parsers.Word("playername"))
    .HandleWith(OnUnbanPlayer)
.EndSubCommand()
.BeginSubCommand("banlist")
    .HandleWith(OnListBannedPlayers)
.EndSubCommand()
```

**Testing**: Command execution tests

---

### Phase 4: GUI Updates

#### Task 4.1: Update MemberListRenderer.cs
**File**: `PantheonWars/GUI/UI/Renderers/Components/MemberListRenderer.cs`

**Changes**:
1. Add "Ban" button next to "Kick" button for non-founder members
2. Add visual indicator for banned status (if viewing ban list)
3. Update button layout to accommodate both Kick and Ban

**Implementation**:
```csharp
// Add ban button after kick button
if (!member.IsFounder && member.PlayerUID != currentPlayerUID)
{
    var kickButtonX = x + width - (kickButtonWidth * 2) - (padding * 2);
    var kickButtonY = y + (height - 22f) / 2;

    if (ButtonRenderer.DrawSmallButton(drawList, "Kick", kickButtonX, kickButtonY, kickButtonWidth, 22f))
    {
        api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
            api.World.Player.Entity, null, false, 8f, 0.5f);
        onKickMember.Invoke(member.PlayerUID);
    }

    var banButtonX = kickButtonX + kickButtonWidth + padding;
    if (ButtonRenderer.DrawSmallButton(drawList, "Ban", banButtonX, kickButtonY, kickButtonWidth, 22f))
    {
        api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
            api.World.Player.Entity, null, false, 8f, 0.5f);
        onBanMember.Invoke(member.PlayerUID);
    }
}
```

**Testing**: Visual testing in game

---

#### Task 4.2: Update ReligionManagementDialog.cs
**File**: `PantheonWars/GUI/ReligionManagementDialog.cs`

**Changes**:
1. Add ban callback handler: `OnBanMemberClicked(string playerUID)`
2. Add "Banned Members" tab or section in "My Religion" tab
3. Add ban confirmation dialog with reason input
4. Wire up ban button callbacks

**New Methods**:
```csharp
private void OnBanMemberClicked(string playerUID)
{
    // Show confirmation dialog with reason input
    ShowBanConfirmationDialog(playerUID);
}

private void ShowBanConfirmationDialog(string playerUID)
{
    // Create dialog with:
    // - Player name display
    // - Reason text input
    // - Expiry days input (optional)
    // - Confirm/Cancel buttons

    // On confirm:
    SendBanRequest(playerUID, reason, expiryDays);
}

private void SendBanRequest(string playerUID, string reason, int? expiryDays)
{
    var packet = new ReligionActionRequestPacket
    {
        Action = "ban",
        ReligionUID = currentReligionUID,
        TargetPlayerUID = playerUID,
        Data = new Dictionary<string, object>
        {
            { "Reason", reason },
            { "ExpiryDays", expiryDays ?? 0 }
        }
    };

    clientChannel.SendPacket(packet);
}

private void RenderBannedMembersSection()
{
    // Fetch banned members from server
    // Display list with:
    // - Player name
    // - Ban reason
    // - Ban date
    // - Expiry date (if applicable)
    // - Unban button (for founder)
}
```

**Testing**: UI interaction tests

---

#### Task 4.3: Add Ban Confirmation Dialog
**File**: Create new `PantheonWars/GUI/BanConfirmationDialog.cs`

**Purpose**: Dedicated dialog for confirming ban action with reason input

**Features**:
- Player name display
- Text input for ban reason
- Optional numeric input for expiry days (0 = permanent)
- Confirm button
- Cancel button

**Testing**: Dialog display and interaction tests

---

### Phase 5: User Feedback & Messaging

#### Task 5.1: Update Join Rejection Messages
**File**: `PantheonWars/Systems/ReligionManager.cs` or command handlers

**Changes**: Provide clear messaging when a banned player attempts to join:

```csharp
public bool CanJoinReligion(string religionUID, string playerUID, out string reason)
{
    // ... existing checks ...

    // Check if banned
    var banDetails = GetBanDetails(religionUID, playerUID);
    if (banDetails != null)
    {
        if (banDetails.ExpiresAt.HasValue)
        {
            reason = $"You are banned from this religion until {banDetails.ExpiresAt.Value:yyyy-MM-dd}. Reason: {banDetails.Reason}";
        }
        else
        {
            reason = $"You are permanently banned from this religion. Reason: {banDetails.Reason}";
        }
        return false;
    }

    // ... rest of validation ...
}
```

**Testing**: Message display verification

---

### Phase 6: Testing

#### Task 6.1: Unit Tests
**Files**: Create/update test files in test project

**Test Coverage**:
1. `ReligionDataTests.cs`:
   - Test adding banned players
   - Test removing banned players
   - Test IsBanned check
   - Test ban expiration cleanup
   - Test GetActiveBans

2. `ReligionManagerTests.cs`:
   - Test BanPlayer method
   - Test UnbanPlayer method
   - Test IsBanned check across religions
   - Test CanJoinReligion with banned players
   - Test ban expiration logic

3. `NetworkPacketTests.cs`:
   - Test ban packet serialization
   - Test unban packet serialization

---

#### Task 6.2: Integration Tests
**Purpose**: Test full ban workflow

**Scenarios**:
1. Founder bans a member:
   - Member is kicked
   - Member is added to ban list
   - Both players receive notifications

2. Banned player attempts to join:
   - Join request is rejected
   - Player receives ban reason message

3. Founder unbans a player:
   - Player is removed from ban list
   - Player can now join

4. Ban expiration:
   - Create ban with expiry
   - Wait for expiry (or mock time)
   - Verify player can rejoin

5. Non-founder attempts to ban:
   - Request is rejected
   - Error message displayed

---

#### Task 6.3: Manual Testing Checklist

**Pre-Release Testing**:
- [ ] Create a religion and invite multiple members
- [ ] As founder, ban a member with reason
- [ ] Verify banned member receives notification
- [ ] Attempt to rejoin as banned member (should fail)
- [ ] Unban the member
- [ ] Verify member can now rejoin
- [ ] Test ban with expiry (1 day)
- [ ] View ban list as founder
- [ ] Test GUI ban button functionality
- [ ] Test ban confirmation dialog
- [ ] Test /religion ban command
- [ ] Test /religion unban command
- [ ] Test /religion banlist command
- [ ] Verify non-founder cannot ban
- [ ] Verify founder cannot ban themselves
- [ ] Test server restart (ban persistence)

---

## File Changes Summary

### New Files
1. `PantheonWars/Data/BanEntry.cs` (new class)
2. `PantheonWars/GUI/BanConfirmationDialog.cs` (new dialog)
3. `PantheonWars/Network/BanPlayerPacket.cs` (optional, if not extending existing)
4. `PantheonWars/Network/UnbanPlayerPacket.cs` (optional, if not extending existing)
5. `Tests/ReligionDataTests.cs` (unit tests)
6. `Tests/ReligionManagerTests.cs` (unit tests)

### Modified Files
1. `PantheonWars/Data/ReligionData.cs` - Add ban tracking
2. `PantheonWars/Systems/ReligionManager.cs` - Add ban methods
3. `PantheonWars/Systems/Interfaces/IReligionManager.cs` - Update interface
4. `PantheonWars/PantheonWarsSystem.cs` - Handle ban/unban requests
5. `PantheonWars/Commands/ReligionCommands.cs` - Add ban commands
6. `PantheonWars/GUI/ReligionManagementDialog.cs` - Add ban UI
7. `PantheonWars/GUI/UI/Renderers/Components/MemberListRenderer.cs` - Add ban button
8. `PantheonWars/Network/ReligionActionRequestPacket.cs` - Support ban actions (if extending)

---

## Edge Cases & Considerations

### Edge Cases to Handle
1. **Founder Ban**: What happens if someone tries to ban the founder?
   - Solution: Prevent founder from being banned (validation check)

2. **Ban After Leaving**: Can you ban someone who already left?
   - Solution: Yes, allow banning ex-members to prevent rejoin

3. **Multiple Bans**: What if someone is banned twice?
   - Solution: Update existing ban entry instead of creating duplicate

4. **Founder Transfer**: What happens to bans when founder leaves?
   - Solution: New founder inherits ban list (no changes needed)

5. **Religion Deleted**: What happens to bans?
   - Solution: Bans are deleted with religion (natural cleanup)

6. **Player Deleted**: What if banned player's account is deleted?
   - Solution: Keep ban entry (UID won't resolve to player, but prevents future use)

7. **Concurrent Actions**: What if player is banned while joining?
   - Solution: Use proper locking in CanJoinReligion check

### Performance Considerations
1. Ban list should be cleaned up periodically (expired bans)
2. Consider max ban list size (e.g., 100 bans per religion)
3. Index banned player UIDs for fast lookup

### Security Considerations
1. Validate founder permission on server side
2. Sanitize ban reason input (max length, no special chars)
3. Rate limit ban actions (prevent abuse)
4. Log ban actions for moderation review

---

## Future Enhancements

### Phase 2 Improvements (Post-MVP)
1. **Ban Appeals**: System for banned players to appeal
2. **Ban History**: Track all historical bans (even if unbanned)
3. **Auto-Ban Rules**: Automatically ban players based on behavior
4. **Ban Levels**: Temporary vs permanent bans with different UI
5. **Ban Notifications**: Email/notification when ban expires
6. **Moderator Role**: Allow non-founders to manage bans
7. **Ban Import/Export**: Share ban lists between religions
8. **Ban Reasons Presets**: Dropdown with common reasons
9. **Ban Duration Presets**: Quick select (1 day, 7 days, 30 days, permanent)

---

## Implementation Order (Recommended)

### Sprint 1: Foundation (Data & Business Logic)
- [ ] Task 1.1: Update ReligionData with ban tracking
- [ ] Task 2.1: Add ban methods to ReligionManager
- [ ] Task 6.1: Write unit tests for data layer

### Sprint 2: Network & Commands
- [ ] Task 1.2: Add ban network packets
- [ ] Task 2.2: Add ban actions to PantheonWarsSystem
- [ ] Task 3.1: Add ban commands to ReligionCommands
- [ ] Task 6.2: Write integration tests

### Sprint 3: User Interface
- [ ] Task 4.1: Update MemberListRenderer with ban button
- [ ] Task 4.2: Update ReligionManagementDialog
- [ ] Task 4.3: Create BanConfirmationDialog
- [ ] Task 5.1: Update messaging

### Sprint 4: Testing & Polish
- [ ] Task 6.3: Manual testing
- [ ] Bug fixes
- [ ] Documentation
- [ ] Code review

---

## Rollout Plan

### Pre-Release
1. Complete all development tasks
2. Pass all unit and integration tests
3. Complete manual testing checklist
4. Code review by team
5. Update user documentation

### Release
1. Merge to main branch
2. Deploy to staging environment
3. Beta test with select users
4. Monitor for issues
5. Deploy to production

### Post-Release
1. Monitor server logs for errors
2. Gather user feedback
3. Address any critical bugs
4. Plan phase 2 enhancements

---

## Success Metrics

### Functional Metrics
- [ ] Founders can ban members successfully
- [ ] Banned players cannot rejoin
- [ ] Founders can unban players
- [ ] Ban expiration works correctly
- [ ] All commands function as expected
- [ ] GUI reflects ban status accurately

### Quality Metrics
- [ ] 90%+ unit test coverage for ban code
- [ ] Zero critical bugs in production
- [ ] < 100ms ban check performance
- [ ] No false positives/negatives in ban checks

### User Experience Metrics
- [ ] Clear error messages for banned players
- [ ] Intuitive UI for ban management
- [ ] Founder satisfaction with ban controls

---

## References

### Key Files for Reference
- `/home/user/PantheonWars/RELIGION_SYSTEM_OVERVIEW.md`
- `/home/user/PantheonWars/RELIGION_SYSTEM_ARCHITECTURE.md`
- `/home/user/PantheonWars/RELIGION_SYSTEM_CODE_SNIPPETS.md`

### Related Issues/PRs
- (Add links once created)

### Documentation
- User guide: (To be created)
- API documentation: (To be updated)

---

## Questions & Decisions Log

### Decision 1: Ban Storage Location
**Question**: Store bans in ReligionData or separate BanData?
**Decision**: Store in ReligionData for simplicity and locality
**Rationale**: Bans are tightly coupled to religions, easier to manage lifecycle

### Decision 2: Permanent vs Temporary Bans
**Question**: Support only permanent or both?
**Decision**: Support both with optional expiry
**Rationale**: Flexibility for founders, not much extra complexity

### Decision 3: Kick + Ban vs Ban Only
**Question**: Should ban automatically kick if member?
**Decision**: Yes, ban should kick if currently a member
**Rationale**: Simpler UX, expected behavior

### Decision 4: New Packets vs Extend Existing
**Question**: Create new packets or extend ReligionActionRequestPacket?
**Decision**: Extend existing packet (add "ban"/"unban" actions)
**Rationale**: Consistent with current architecture, less code

---

## Appendix: Code Snippets

### Example Ban Usage
```csharp
// Ban a player permanently
_religionManager.BanPlayer(
    religionUID: "religion-123",
    playerUID: "player-456",
    bannedByUID: "founder-789",
    reason: "Spamming chat",
    expiryDays: null
);

// Ban a player for 7 days
_religionManager.BanPlayer(
    religionUID: "religion-123",
    playerUID: "player-456",
    bannedByUID: "founder-789",
    reason: "Rule violation",
    expiryDays: 7
);

// Check if player is banned
if (_religionManager.IsBanned("religion-123", "player-456"))
{
    // Reject join request
}

// Unban a player
_religionManager.UnbanPlayer("religion-123", "player-456");
```

---

**End of Implementation Plan**
