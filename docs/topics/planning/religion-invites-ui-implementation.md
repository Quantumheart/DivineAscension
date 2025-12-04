# Religion Invite Acceptance UI - Implementation Plan

## Overview

Add religion invite acceptance UI to fully match the existing civilization invite pattern. This includes backend persistence, network protocol updates, UI state management, and a new Invites sub-tab in the religion UI.

## Design Decisions

- **Pattern**: Exact mirror of civilization invite system for consistency
- **Persistence**: Full disk persistence with 7-day expiration (matching civilizations)
- **UI Pattern**: Separate "Invites" sub-tab (shown only when player has no religion)
- **Actions**: Explicit accept/decline buttons (matching civilizations)
- **Tab Order**: Browse → My Religion → Activity → Invites → Create

## Implementation Phases

### Phase 1: Backend Data Layer

#### 1.1 Create ReligionInvite Class
**File**: `PantheonWars/Data/ReligionData.cs`

Add new class after `BanEntry`:

```csharp
[ProtoContract]
public class ReligionInvite
{
    public ReligionInvite() { }

    public ReligionInvite(string inviteId, string religionId, string playerUID, DateTime sentDate)
    {
        InviteId = inviteId;
        ReligionId = religionId;
        PlayerUID = playerUID;
        SentDate = sentDate;
        ExpiresDate = sentDate.AddDays(7);
    }

    [ProtoMember(1)] public string InviteId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ReligionId { get; set; } = string.Empty;
    [ProtoMember(3)] public string PlayerUID { get; set; } = string.Empty;
    [ProtoMember(4)] public DateTime SentDate { get; set; }
    [ProtoMember(5)] public DateTime ExpiresDate { get; set; }

    public bool IsValid => DateTime.UtcNow < ExpiresDate;
}
```

**Reference**: `CivilizationData.cs:118-177`

#### 1.2 Create ReligionWorldData Class
**File**: `PantheonWars/Data/ReligionWorldData.cs` (NEW)

Create new world data container for invite persistence:

```csharp
[ProtoContract]
public class ReligionWorldData
{
    public ReligionWorldData()
    {
        PendingInvites = new List<ReligionInvite>();
    }

    [ProtoMember(1)] public List<ReligionInvite> PendingInvites { get; set; }

    public void AddInvite(ReligionInvite invite) => PendingInvites.Add(invite);
    public void RemoveInvite(string inviteId) => PendingInvites.RemoveAll(i => i.InviteId == inviteId);
    public List<ReligionInvite> GetInvitesForPlayer(string playerUID)
        => PendingInvites.Where(i => i.PlayerUID == playerUID && i.IsValid).ToList();
    public ReligionInvite? GetInvite(string inviteId)
        => PendingInvites.FirstOrDefault(i => i.InviteId == inviteId);
    public bool HasPendingInvite(string religionId, string playerUID)
        => PendingInvites.Any(i => i.ReligionId == religionId && i.PlayerUID == playerUID && i.IsValid);
    public void CleanupExpired() => PendingInvites.RemoveAll(i => !i.IsValid);
}
```

**Reference**: `CivilizationWorldData.cs`

#### 1.3 Update ReligionManager
**File**: `PantheonWars/Systems/ReligionManager.cs`

**Changes**:
1. Replace line 18: `private readonly Dictionary<string, List<string>> _invitations`
   With: `private ReligionWorldData _inviteData = new();`

2. Add constant: `private const string INVITE_DATA_KEY = "pantheonwars_religion_invites";`

3. Update `InvitePlayer` (lines 183-206) to create structured invites with Guid and 7-day expiration

4. Update `HasInvitation`, `RemoveInvitation` methods to use `_inviteData`

5. Replace `GetPlayerInvitations` to return `List<ReligionInvite>` and cleanup expired

6. Add new methods:
   - `AcceptInvite(string inviteId, string playerUID)` - Validates and joins religion
   - `DeclineInvite(string inviteId, string playerUID)` - Removes invite

7. Add persistence methods:
   - `LoadInviteData()` - Load from `_sapi.WorldManager.SaveGame.GetData(INVITE_DATA_KEY)`
   - `SaveInviteData()` - Save with `SerializerUtil.Serialize(_inviteData)`
   - Call `LoadInviteData()` in `OnSaveGameLoaded()`
   - Call `SaveInviteData()` in `OnGameWorldSave()` and after invite modifications

**Reference**: `CivilizationManager.cs` invite management

---

### Phase 2: Network Protocol

#### 2.1 Add ReligionInviteInfo to Response Packet
**File**: `PantheonWars/Network/PlayerReligionInfoResponsePacket.cs`

**Changes**:
1. Add field after `BannedPlayers` (after line 34):
   ```csharp
   [ProtoMember(13)] public List<ReligionInviteInfo> PendingInvites { get; set; } = new();
   ```

2. Add nested class after `BanInfo`:
   ```csharp
   [ProtoContract]
   public class ReligionInviteInfo
   {
       [ProtoMember(1)] public string InviteId { get; set; } = string.Empty;
       [ProtoMember(2)] public string ReligionId { get; set; } = string.Empty;
       [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;
       [ProtoMember(4)] public DateTime ExpiresAt { get; set; }
   }
   ```

**Reference**: `CivilizationInfoResponsePacket.cs:70-83`

#### 2.2 Populate Invites in Server Handler
**File**: `PantheonWars/PantheonWarsSystem.cs`

In `OnPlayerReligionInfoRequest` (lines 275-337), add after banned players list (line 323):

```csharp
// Build pending invites list
var playerInvites = _religionManager!.GetPlayerInvitations(fromPlayer.PlayerUID);
foreach (var invite in playerInvites)
{
    var inviteReligion = _religionManager.GetReligion(invite.ReligionId);
    if (inviteReligion != null)
    {
        response.PendingInvites.Add(new PlayerReligionInfoResponsePacket.ReligionInviteInfo
        {
            InviteId = invite.InviteId,
            ReligionId = invite.ReligionId,
            ReligionName = inviteReligion.ReligionName,
            ExpiresAt = invite.ExpiresDate
        });
    }
}
```

#### 2.3 Add Accept/Decline Action Handlers
**File**: `PantheonWars/PantheonWarsSystem.cs`

In `OnReligionActionRequest` switch statement (after line 455), add new cases:

```csharp
case "accept":
    if (string.IsNullOrEmpty(packet.ReligionUID))
    {
        message = "Invalid invite ID provided.";
        break;
    }
    success = _religionManager!.AcceptInvite(packet.ReligionUID, fromPlayer.PlayerUID);
    if (success)
    {
        message = "Successfully joined the religion!";
        SendPlayerDataToClient(fromPlayer);
        var joinedReligion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
        if (joinedReligion != null)
        {
            _serverChannel!.SendPacket(new ReligionStateChangedPacket
            {
                Reason = $"You joined {joinedReligion.ReligionName}",
                HasReligion = true
            }, fromPlayer);
        }
    }
    else
    {
        message = "Failed to accept invitation. It may have expired or you may already have a religion.";
    }
    break;

case "decline":
    if (string.IsNullOrEmpty(packet.ReligionUID))
    {
        message = "Invalid invite ID provided.";
        break;
    }
    success = _religionManager!.DeclineInvite(packet.ReligionUID, fromPlayer.PlayerUID);
    message = success ? "Invitation declined." : "Failed to decline invitation.";
    break;
```

**Note**: InviteId is passed via `packet.ReligionUID` parameter for accept/decline actions

---

### Phase 3: UI State

#### 3.1 Update ReligionSubTab Enum
**File**: `PantheonWars/GUI/State/ReligionTabState.cs`

Replace enum (lines 101-107):

```csharp
public enum ReligionSubTab
{
    Browse = 0,
    MyReligion = 1,
    Activity = 2,
    Invites = 3,
    Create = 4
}
```

#### 3.2 Add Invite State Properties
**File**: `PantheonWars/GUI/State/ReligionTabState.cs`

Add after `ActivityScrollY` (after line 43):

```csharp
// Invites tab state
public List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> MyInvites { get; set; } = new();
public float InvitesScrollY { get; set; } = 0f;
public bool IsInvitesLoading { get; set; } = false;
public string? InvitesError { get; set; }
```

**Reference**: `CivilizationState.cs:31-33, 42`

#### 3.3 Update Reset Method
**File**: `PantheonWars/GUI/State/ReligionTabState.cs`

Add to `Reset()` method (after line 89):

```csharp
// Invites tab
MyInvites.Clear();
InvitesScrollY = 0f;
IsInvitesLoading = false;
InvitesError = null;
```

---

### Phase 4: UI Rendering

#### 4.1 Create ReligionInvitesRenderer
**File**: `PantheonWars/GUI/UI/Renderers/Religion/ReligionInvitesRenderer.cs` (NEW)

Create new renderer mirroring `CivilizationInvitesRenderer.cs`:

```csharp
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

internal static class ReligionInvitesRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        TextRenderer.DrawLabel(drawList, "Your Religion Invitations", x, currentY, 18f, ColorPalette.White);
        currentY += 26f;

        TextRenderer.DrawInfoText(drawList,
            "This tab shows invitations you've received. To send invitations, go to the \"My Religion\" tab (founders only).",
            x, currentY, width);
        currentY += 32f;

        if (state.MyInvites.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList, "No pending invitations.", x, currentY + 8f, width);
            return height;
        }

        state.InvitesScrollY = ScrollableList.Draw(
            drawList, x, currentY, width, height - (currentY - y),
            state.MyInvites, 80f, 10f, state.InvitesScrollY,
            (invite, cx, cy, cw, ch) => DrawInviteCard(invite, cx, cy, cw, ch, manager, api),
            loadingText: state.IsInvitesLoading ? "Loading invitations..." : null
        );

        return height;
    }

    private static void DrawInviteCard(
        PlayerReligionInfoResponsePacket.ReligionInviteInfo invite,
        float x, float y, float width, float height,
        BlessingDialogManager manager, ICoreClientAPI api)
    {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        TextRenderer.DrawLabel(drawList, "Invitation to Religion", x + 12f, y + 8f, 16f);
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"From: {invite.ReligionName}");
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"Expires: {invite.ExpiresAt:yyyy-MM-dd HH:mm}");

        var enabled = !manager.ReligionState.IsInvitesLoading;
        if (ButtonRenderer.DrawButton(drawList, "Accept", x + width - 180f, y + height - 32f, 80f, 28f,
                true, enabled: enabled))
        {
            manager.RequestReligionAction("accept", invite.InviteId, "");
        }

        if (ButtonRenderer.DrawButton(drawList, "Decline", x + width - 90f, y + height - 32f, 80f, 28f,
                isPrimary: false, enabled: enabled))
        {
            manager.RequestReligionAction("decline", invite.InviteId, "");
        }
    }
}
```

**Reference**: Exact mirror of `CivilizationInvitesRenderer.cs`

#### 4.2 Update ReligionTabRenderer
**File**: `PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs`

**Changes**:

1. **Add Invites tab button** (lines 35-41):
   ```csharp
   DrawTabButton("Browse", (int) ReligionSubTab.Browse);
   DrawTabButton("My Religion", (int) ReligionSubTab.MyReligion);
   DrawTabButton("Activity", (int) ReligionSubTab.Activity);
   if (!manager.HasReligion())
   {
       DrawTabButton("Invites", (int) ReligionSubTab.Invites);
   }
   if (!manager.HasReligion())
   {
       DrawTabButton("Create", (int) ReligionSubTab.Create);
   }
   ```

2. **Add error clearing** (in tab change switch):
   ```csharp
   case 3: // Invites
       state.InvitesError = null;
       break;
   case 4: // Create
       state.CreateError = null;
       break;
   ```

3. **Add error banner case**:
   ```csharp
   case ReligionSubTab.Invites:
       bannerMessage = state.InvitesError;
       showRetry = bannerMessage != null;
       break;
   ```

4. **Add retry handler case**:
   ```csharp
   case 3: // Invites
       manager.RequestPlayerReligionInfo();
       break;
   ```

5. **Add dismiss handler case**:
   ```csharp
   case 3: // Invites
       state.InvitesError = null;
       break;
   ```

6. **Add routing case**:
   ```csharp
   case ReligionSubTab.Invites:
       ReligionInvitesRenderer.Draw(manager, api, x, contentY, width, contentHeight);
       break;
   ```

**Reference**: `CivilizationTabRenderer.cs:31-40` for tab visibility pattern

---

### Phase 5: Manager Integration

#### 5.1 Update BlessingDialogManager
**File**: `PantheonWars/GUI/BlessingDialogManager.cs`

In `UpdatePlayerReligionInfo` method (around line 435-441), add after updating `MyReligionInfo`:

```csharp
// Populate pending invites
ReligionState.MyInvites.Clear();
ReligionState.MyInvites.AddRange(info.PendingInvites);
ReligionState.IsInvitesLoading = false;
```

**Note**: `RequestReligionAction` signature already supports passing inviteId via `religionUID` parameter - no changes needed.

---

## Critical Files

### Reference Files (Read these first):
- `PantheonWars/Data/CivilizationData.cs:118-177` - CivilizationInvite pattern
- `PantheonWars/Data/CivilizationWorldData.cs` - World data persistence pattern
- `PantheonWars/GUI/State/CivilizationState.cs` - State structure
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationInvitesRenderer.cs` - UI pattern
- `PantheonWars/Network/Civilization/CivilizationInfoResponsePacket.cs:70-83` - PendingInvite structure

### Files to Modify:
1. `PantheonWars/Data/ReligionData.cs` - Add ReligionInvite class
2. `PantheonWars/Data/ReligionWorldData.cs` - NEW file
3. `PantheonWars/Systems/ReligionManager.cs` - Replace invite storage, add persistence
4. `PantheonWars/Network/PlayerReligionInfoResponsePacket.cs` - Add PendingInvites field
5. `PantheonWars/PantheonWarsSystem.cs` - Update handlers
6. `PantheonWars/GUI/State/ReligionTabState.cs` - Add invite state
7. `PantheonWars/GUI/UI/Renderers/Religion/ReligionInvitesRenderer.cs` - NEW file
8. `PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs` - Add tab routing
9. `PantheonWars/GUI/BlessingDialogManager.cs` - Populate invites

## Testing Checklist

- [ ] Religion invites persist across server restarts
- [ ] Invites expire after 7 days
- [ ] Invites tab only visible when player has no religion
- [ ] Accept button joins religion and hides tab
- [ ] Decline button removes invite
- [ ] Multiple invites display correctly
- [ ] Loading states work correctly
- [ ] Error messages display properly

## Implementation Order

1. **Backend** (Phase 1) - Data structures and persistence
2. **Network** (Phase 2) - Protocol updates
3. **State** (Phase 3) - UI state management
4. **UI** (Phase 4) - Rendering components
5. **Integration** (Phase 5) - Wire everything together
