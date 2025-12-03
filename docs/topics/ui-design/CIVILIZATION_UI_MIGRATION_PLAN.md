# Civilization UI Migration Plan

**Date:** 2025-12-01
**Purpose:** Consolidate civilization UI into BlessingDialog, following the renderer pattern and shared component architecture
**References:**
- [BLESSING_UI_IMPLEMENTATION_PLAN.md](BLESSING_UI_IMPLEMENTATION_PLAN.md)
- [ui-refactoring-plan.md](ui-refactoring-plan.md)
- [ui-refactoring-progress.md](ui-refactoring-progress.md)
- [civilization-system-implementation.md](../planning/civilization-system-implementation.md)

---

## 🎯 Executive Summary

**Goal:** Migrate the standalone CivilizationDialog (630 lines, 4 tabs) into the existing BlessingDialog as a new tab, following the established renderer pattern and shared component architecture.

**Why:**
- Consolidate all religion/deity/civilization management into a single interface
- Reduce code duplication (both dialogs use similar patterns)
- Improve UX by keeping related functionality together
- Follow the established architectural patterns from the refactoring work

**Current State:**
- ✅ **BlessingDialog**: Fully functional with blessing tree view (split player/religion blessings)
- ✅ **UI Refactoring**: Complete (Phases 1-5), shared components available
- ✅ **CivilizationDialog**: Standalone dialog with 4 tabs (Browse, My Civ, Invites, Create)
- ✅ **CivilizationInfoOverlay**: Compact HUD overlay (will be deprecated)

**Estimated Time:** 12-16 hours

---

## 📊 Current Architecture Analysis

### Files to Migrate

1. **CivilizationDialog.cs** (630 lines)
   - 4 tabs: Browse, My Civilization, Invitations, Create
   - Tab management using ImGui.BeginTabBar
   - State: _allCivilizations, _myCivilization, _myInvites, filters
   - Event handlers: 3 civilization packet handlers
   - **Issues:**
     - Duplicate deity color/title methods (already in DeityHelper)
     - Manual tab rendering (TabControl component available)
     - No state separation (mixed with rendering)
     - Large monolithic methods

2. **CivilizationInfoOverlay.cs** (240 lines)
   - Compact overlay (toggle with Shift+C)
   - Shows: civ name, member count, deity indicators
   - Quick "Manage" button opens CivilizationDialog
   - **Issues:**
     - Duplicate deity color method
     - Will become redundant when integrated into BlessingDialog

### Available Shared Components (from Refactoring)

**From Phase 1-5:**
- ✅ `ButtonRenderer` - Unified button drawing
- ✅ `TextInput` - Text input components
- ✅ `Scrollbar` - Scrolling components
- ✅ `Dropdown` - Dropdown selection
- ✅ `TabControl` - Generic tab control
- ✅ `Checkbox` - Checkbox component
- ✅ `ScrollableList` - Generic scrollable list
- ✅ `TextRenderer` - Label, info, error, success text
- ✅ `ColorPalette` - Centralized color definitions
- ✅ `DeityHelper` - Deity colors, titles, names

### BlessingDialog Current Structure

**Main Files:**
- `BlessingDialog.cs` (290 lines) - Window lifecycle
- `BlessingDialogManager.cs` - State management, data access
- `BlessingDialogEventHandlers.cs` (431 lines) - Event handling
- `BlessingDialogState.cs` (38 lines) - Dialog state
- `OverlayCoordinator.cs` (167 lines) - Manages overlays

**Renderers:**
- `BlessingUIRenderer.cs` - Main coordinator
- `ReligionHeaderRenderer.cs` - Header banner
- `BlessingTreeRenderer.cs` - Blessing tree layout
- `BlessingNodeRenderer.cs` - Individual nodes
- `BlessingInfoRenderer.cs` - Info panel
- `BlessingActionsRenderer.cs` - Action buttons
- `TooltipRenderer.cs` - Hover tooltips

**Current Layout:**
```
┌─────────────────────────────────────────────┐
│ Religion Header (deity, favor, prestige)   │
├─────────────────────────────────────────────┤
│ Blessing Tree (split: player | religion)   │
│                                             │
│ [Node] [Node] [Node]  │  [Node] [Node]      │
│ [Node] [Node]         │  [Node]             │
├─────────────────────────────────────────────┤
│ Blessing Info Panel (selected blessing)    │
├─────────────────────────────────────────────┤
│ Action Buttons (unlock, close)             │
└─────────────────────────────────────────────┘
```

---

## 🎨 Proposed Design

### Tab-Based Layout

Add a tab system to BlessingDialog with 3 tabs:

```
┌─────────────────────────────────────────────────────────┐
│ Religion Header (deity, favor, prestige, civ info)     │
├─────────────────────────────────────────────────────────┤
│ [Blessings] [Manage Religion] [Civilization]           │ <- New tab bar
├─────────────────────────────────────────────────────────┤
│                                                         │
│ TAB CONTENT AREA                                        │
│                                                         │
│ (Different renderer for each tab)                       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Tab Breakdown

#### Tab 1: Blessings (Existing)
- Current blessing tree view
- No changes needed
- Renderer: `BlessingTreeRenderer`, `BlessingNodeRenderer`, `BlessingInfoRenderer`

#### Tab 2: Manage Religion (Future - Optional)
- Religion management features (currently in overlays)
- Could consolidate ReligionManagementOverlay here
- **Scope:** Out of scope for this migration (defer to future work)

#### Tab 3: Civilization (New)
- Sub-tabs for civilization features:
  - **Browse**: List all civilizations (filterable by deity)
  - **My Civilization**: Manage members, invites, settings
  - **Invitations**: View and accept invitations
  - **Create**: Form to create new civilization

**Layout for Civilization Tab:**
```
┌─────────────────────────────────────────────────────────┐
│ [Browse] [My Civilization] [Invitations] [Create]      │ <- Sub-tabs
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Sub-tab content (ScrollableList, forms, etc.)          │
│                                                         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Architecture Approach

**Follow Established Patterns:**
- ✅ State extraction into dedicated state class
- ✅ Renderer pattern for all UI components
- ✅ Use shared components (TabControl, ScrollableList, ButtonRenderer, etc.)
- ✅ Event-driven updates (subscribe to civilization packets)
- ✅ Manager integration (use BlessingDialogManager for data access)

---

## 📋 Phase Breakdown

### Phase 1: Preparation & State Management (2-3 hours)

**Goal:** Extract civilization state and integrate into BlessingDialogManager

#### Task 1.1: Create CivilizationState.cs
**File:** `PantheonWars/GUI/State/CivilizationState.cs` (~80 lines)

**Purpose:** Centralize all civilization-related UI state

**Properties:**
```csharp
public class CivilizationState
{
    // Browse tab state
    public List<CivilizationListResponsePacket.CivilizationInfo> AllCivilizations { get; set; }
    public string DeityFilter { get; set; } = "";
    public float BrowseScrollY { get; set; }

    // My Civilization tab state
    public CivilizationInfoResponsePacket.CivilizationDetails? MyCivilization { get; set; }
    public string InviteReligionName { get; set; } = "";
    public float MemberScrollY { get; set; }

    // Invitations tab state
    public List<CivilizationInfoResponsePacket.PendingInvite> MyInvites { get; set; }
    public float InvitesScrollY { get; set; }

    // Create tab state
    public string CreateCivName { get; set; } = "";

    // Current sub-tab (0=Browse, 1=My Civ, 2=Invites, 3=Create)
    public int CurrentSubTab { get; set; }

    public void Reset()
    {
        AllCivilizations = new();
        DeityFilter = "";
        BrowseScrollY = 0f;
        MyCivilization = null;
        InviteReligionName = "";
        MemberScrollY = 0f;
        MyInvites = new();
        InvitesScrollY = 0f;
        CreateCivName = "";
        CurrentSubTab = 0;
    }
}
```

**Files Created:**
- `PantheonWars/GUI/State/CivilizationState.cs`

#### Task 1.2: Integrate into BlessingDialogManager
**File:** `PantheonWars/GUI/BlessingDialogManager.cs` (modify)

**Add:**
```csharp
public CivilizationState CivilizationState { get; private set; } = new();

public void RequestCivilizationList(string deityFilter = "")
{
    _pantheonWarsSystem?.RequestCivilizationList(deityFilter);
}

public void RequestCivilizationInfo(string civId)
{
    _pantheonWarsSystem?.RequestCivilizationInfo(civId);
}

public void RequestCivilizationAction(string action, string civId = "", string target = "", string name = "")
{
    _pantheonWarsSystem?.RequestCivilizationAction(action, civId, target, name);
}
```

**Files Modified:**
- `PantheonWars/GUI/BlessingDialogManager.cs`

#### Task 1.3: Add Main Tab State
**File:** `PantheonWars/GUI/State/BlessingDialogState.cs` (modify)

**Add:**
```csharp
// Current main tab (0=Blessings, 1=Manage Religion, 2=Civilization)
public int CurrentMainTab { get; set; }
```

**Files Modified:**
- `PantheonWars/GUI/State/BlessingDialogState.cs`

#### Task 1.4: Add Event Handlers
**File:** `PantheonWars/GUI/BlessingDialogEventHandlers.cs` (modify)

**Add:**
```csharp
private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
{
    Manager.CivilizationState.AllCivilizations = packet.Civilizations;
}

private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
{
    Manager.CivilizationState.MyCivilization = packet.Details;
    Manager.CivilizationState.MyInvites = packet.Details?.PendingInvites ?? new();
}

private void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
{
    if (packet.Success)
    {
        // Refresh data
        Manager.RequestCivilizationList(Manager.CivilizationState.DeityFilter);
        Manager.RequestCivilizationInfo("");
    }
}
```

**Subscribe in BlessingDialog.cs:**
```csharp
public override void OnGuiOpened()
{
    // ... existing code ...

    _pantheonWarsSystem.CivilizationListReceived += OnCivilizationListReceived;
    _pantheonWarsSystem.CivilizationInfoReceived += OnCivilizationInfoReceived;
    _pantheonWarsSystem.CivilizationActionCompleted += OnCivilizationActionCompleted;
}

public override void OnGuiClosed()
{
    // ... existing code ...

    _pantheonWarsSystem.CivilizationListReceived -= OnCivilizationListReceived;
    _pantheonWarsSystem.CivilizationInfoReceived -= OnCivilizationInfoReceived;
    _pantheonWarsSystem.CivilizationActionCompleted -= OnCivilizationActionCompleted;
}
```

**Files Modified:**
- `PantheonWars/GUI/BlessingDialogEventHandlers.cs`
- `PantheonWars/GUI/BlessingDialog.cs`

**Success Criteria:**
- ✅ CivilizationState.cs compiles
- ✅ BlessingDialogManager can request civilization data
- ✅ Event handlers update state correctly
- ✅ No compilation errors

---

### Phase 2: Create Civilization Renderers (6-8 hours)

**Goal:** Create renderer components for civilization UI using shared components

#### Task 2.1: Create CivilizationBrowseRenderer.cs
**File:** `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationBrowseRenderer.cs` (~180 lines)

**Purpose:** Render "Browse" sub-tab - list all civilizations

**Features:**
- Deity filter dropdown (using `Dropdown` component)
- Refresh button (using `ButtonRenderer`)
- Scrollable list of civilizations (using `ScrollableList` component)
- Civilization cards with deity indicators
- "View Details" button on each card

**Pseudocode:**
```csharp
internal static class CivilizationBrowseRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivilizationState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // Filter bar
        currentY += 8f;
        TextRenderer.DrawLabel(drawList, x, currentY, "Filter by deity:");

        // Deity filter dropdown
        var deities = new[] { "All", "Khoras", "Lysa", ... };
        var selectedIndex = Array.IndexOf(deities, state.DeityFilter);
        var newIndex = Dropdown.Draw(...);
        if (newIndex != selectedIndex)
        {
            state.DeityFilter = newIndex == 0 ? "" : deities[newIndex];
            manager.RequestCivilizationList(state.DeityFilter);
        }

        // Refresh button
        if (ButtonRenderer.DrawButton(..., "Refresh"))
        {
            manager.RequestCivilizationList(state.DeityFilter);
        }

        currentY += 40f;

        // Scrollable list of civilizations
        state.BrowseScrollY = ScrollableList.Draw(
            drawList, x, currentY, width, height - (currentY - y),
            items: state.AllCivilizations,
            itemHeight: 90f,
            itemSpacing: 8f,
            scrollY: state.BrowseScrollY,
            itemRenderer: (civ, cx, cy, cw, ch) => DrawCivilizationCard(civ, cx, cy, cw, ch, manager),
            emptyText: "No civilizations found."
        );

        return currentY - y + height;
    }

    private static void DrawCivilizationCard(
        CivilizationInfo civ, float x, float y, float width, float height,
        BlessingDialogManager manager)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Card background
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown),
            4f
        );

        // Civilization name
        TextRenderer.DrawLabel(drawList, x + 12f, y + 8f, civ.Name);

        // Member count
        drawList.AddText(ImGui.GetFont(), 13f,
            new Vector2(x + 12f, y + 28f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"Members: {civ.MemberCount}/4 religions"
        );

        // Deity indicators (colored boxes)
        var deityX = x + 12f;
        var deityY = y + 48f;
        foreach (var deity in civ.MemberDeities)
        {
            if (Enum.TryParse<DeityType>(deity, out var deityType))
            {
                var color = DeityHelper.GetDeityColor(deityType);
                drawList.AddRectFilled(
                    new Vector2(deityX, deityY),
                    new Vector2(deityX + 12f, deityY + 12f),
                    ImGui.ColorConvertFloat4ToU32(color),
                    2f
                );
                deityX += 16f;
            }
        }

        // "View Details" button
        if (ButtonRenderer.DrawButton(
            drawList, x + width - 120f, y + 29f, 110f, 32f, "View Details"))
        {
            manager.RequestCivilizationInfo(civ.CivId);
            manager.CivilizationState.CurrentSubTab = 1; // Switch to "My Civ" tab
        }
    }
}
```

**Files Created:**
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationBrowseRenderer.cs`

#### Task 2.2: Create CivilizationManageRenderer.cs
**File:** `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationManageRenderer.cs` (~220 lines)

**Purpose:** Render "My Civilization" sub-tab - manage members and invites

**Features:**
- Display civilization name and created date
- Scrollable member list (using `ScrollableList`)
- Member religion cards with deity indicators
- Founder badge
- Kick button (founder only, using `ButtonRenderer`)
- Invite section (text input + button)
- Pending invites list
- Leave/Disband buttons

**Pseudocode:**
```csharp
internal static class CivilizationManageRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivilizationState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        if (state.MyCivilization == null)
        {
            TextRenderer.DrawInfoText(drawList, x, currentY, width,
                "You are not in a civilization. Join one or create your own!");
            return 50f;
        }

        var civ = state.MyCivilization;

        // Civilization info
        TextRenderer.DrawLabel(drawList, x, currentY, $"Civilization: {civ.Name}");
        currentY += 20f;
        TextRenderer.DrawInfoText(drawList, x, currentY, width, $"Founded: {civ.CreatedDate:yyyy-MM-dd}");
        currentY += 30f;

        // Member list header
        TextRenderer.DrawLabel(drawList, x, currentY, $"Member Religions ({civ.MemberReligions.Count}/4):");
        currentY += 30f;

        // Scrollable member list
        var listHeight = Math.Min(250f, height - (currentY - y) - 200f);
        state.MemberScrollY = ScrollableList.Draw(
            drawList, x, currentY, width, listHeight,
            items: civ.MemberReligions,
            itemHeight: 40f,
            itemSpacing: 4f,
            scrollY: state.MemberScrollY,
            itemRenderer: (member, mx, my, mw, mh) => DrawMemberCard(member, mx, my, mw, mh, civ, manager),
            emptyText: "No members."
        );

        currentY += listHeight + 16f;

        // Invite section (founder only)
        TextRenderer.DrawLabel(drawList, x, currentY, "Invite Religion:");
        currentY += 20f;

        state.InviteReligionName = TextInput.Draw(
            drawList, x, currentY, 300f, state.InviteReligionName, "Religion name...");

        if (ButtonRenderer.DrawButton(drawList, x + 310f, currentY - 4f, 100f, 28f, "Send Invite"))
        {
            if (!string.IsNullOrEmpty(state.InviteReligionName))
            {
                manager.RequestCivilizationAction("invite", civ.CivId, state.InviteReligionName);
                state.InviteReligionName = "";
            }
        }

        currentY += 40f;

        // Pending invites (founder only)
        if (civ.PendingInvites.Count > 0)
        {
            TextRenderer.DrawLabel(drawList, x, currentY, "Pending Invitations:");
            currentY += 20f;

            foreach (var invite in civ.PendingInvites)
            {
                drawList.AddText(ImGui.GetFont(), 13f,
                    new Vector2(x, currentY),
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
                    $"{invite.ReligionName} (expires {invite.ExpiresAt:yyyy-MM-dd})"
                );
                currentY += 20f;
            }

            currentY += 8f;
        }

        // Leave/Disband buttons
        if (ButtonRenderer.DrawButton(drawList, x, currentY, 150f, 32f, "Leave Civilization"))
        {
            manager.RequestCivilizationAction("leave");
        }

        if (ButtonRenderer.DrawButton(drawList, x + 160f, currentY, 170f, 32f, "Disband Civilization",
            customColor: ColorPalette.Red))
        {
            manager.RequestCivilizationAction("disband", civ.CivId);
        }

        currentY += 40f;

        return currentY - y;
    }

    private static void DrawMemberCard(
        MemberReligion member, float x, float y, float width, float height,
        CivilizationDetails civ, BlessingDialogManager manager)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Deity indicator
        if (Enum.TryParse<DeityType>(member.Deity, out var deityType))
        {
            var color = DeityHelper.GetDeityColor(deityType);
            drawList.AddRectFilled(
                new Vector2(x, y),
                new Vector2(x + 12f, y + 12f),
                ImGui.ColorConvertFloat4ToU32(color),
                2f
            );
        }

        // Religion name
        drawList.AddText(ImGui.GetFont(), 13f,
            new Vector2(x + 16f, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"{member.ReligionName} ({member.MemberCount} members)"
        );

        // Founder badge or Kick button
        var isFounder = civ.FounderReligionUID == member.ReligionId;
        if (isFounder)
        {
            drawList.AddText(ImGui.GetFont(), 13f,
                new Vector2(x + width - 150f, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                "=== Founder ==="
            );
        }
        else
        {
            if (ButtonRenderer.DrawSmallButton(drawList, x + width - 80f, y - 4f, 80f, 24f, "Kick"))
            {
                manager.RequestCivilizationAction("kick", civ.CivId, member.ReligionName);
            }
        }
    }
}
```

**Files Created:**
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationManageRenderer.cs`

#### Task 2.3: Create CivilizationInvitesRenderer.cs
**File:** `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationInvitesRenderer.cs` (~120 lines)

**Purpose:** Render "Invitations" sub-tab - view and accept invitations

**Features:**
- Scrollable list of invitations (using `ScrollableList`)
- Invitation cards with details
- Accept/Decline buttons (using `ButtonRenderer`)

**Pseudocode:**
```csharp
internal static class CivilizationInvitesRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivilizationState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        TextRenderer.DrawLabel(drawList, x, currentY, "Your Civilization Invitations");
        currentY += 30f;

        state.InvitesScrollY = ScrollableList.Draw(
            drawList, x, currentY, width, height - (currentY - y),
            items: state.MyInvites,
            itemHeight: 60f,
            itemSpacing: 8f,
            scrollY: state.InvitesScrollY,
            itemRenderer: (invite, ix, iy, iw, ih) => DrawInviteCard(invite, ix, iy, iw, ih, manager),
            emptyText: "No pending invitations."
        );

        return height;
    }

    private static void DrawInviteCard(
        PendingInvite invite, float x, float y, float width, float height,
        BlessingDialogManager manager)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Card background
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown),
            4f
        );

        // Invite details
        TextRenderer.DrawLabel(drawList, x + 12f, y + 8f, "Invitation to civilization");
        drawList.AddText(ImGui.GetFont(), 13f,
            new Vector2(x + 12f, y + 26f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"From: {invite.ReligionName}"
        );
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(x + 12f, y + 42f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            $"Expires: {invite.ExpiresAt:yyyy-MM-dd HH:mm}"
        );

        // Accept button
        if (ButtonRenderer.DrawButton(drawList, x + width - 180f, y + 16f, 80f, 28f, "Accept",
            isPrimary: true))
        {
            manager.RequestCivilizationAction("accept", "", invite.InviteId);
        }

        // Decline button
        if (ButtonRenderer.DrawButton(drawList, x + width - 90f, y + 16f, 80f, 28f, "Decline"))
        {
            // TODO: Add decline action
            api.ShowChatMessage("Decline functionality coming soon!");
        }
    }
}
```

**Files Created:**
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationInvitesRenderer.cs`

#### Task 2.4: Create CivilizationCreateRenderer.cs
**File:** `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationCreateRenderer.cs` (~100 lines)

**Purpose:** Render "Create" sub-tab - form to create new civilization

**Features:**
- Requirements list (bullet points)
- Civilization name input (using `TextInput`)
- Create button (using `ButtonRenderer`)
- Info text about civilizations

**Pseudocode:**
```csharp
internal static class CivilizationCreateRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivilizationState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        TextRenderer.DrawLabel(drawList, x, currentY, "Create a New Civilization");
        currentY += 30f;

        // Requirements
        TextRenderer.DrawInfoText(drawList, x, currentY, width, "Requirements:");
        currentY += 20f;

        var requirements = new[]
        {
            "You must be a religion founder",
            "Your religion must not be in another civilization",
            "Name must be 3-32 characters",
            "No cooldowns active"
        };

        foreach (var req in requirements)
        {
            drawList.AddCircleFilled(new Vector2(x + 8f, currentY + 6f), 2f,
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));
            drawList.AddText(ImGui.GetFont(), 12f,
                new Vector2(x + 16f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                req
            );
            currentY += 18f;
        }

        currentY += 16f;

        // Civilization name input
        TextRenderer.DrawLabel(drawList, x, currentY, "Civilization Name:");
        currentY += 20f;

        state.CreateCivName = TextInput.Draw(
            drawList, x, currentY, 300f, state.CreateCivName,
            placeholder: "Enter name (3-32 characters)...",
            maxLength: 32
        );
        currentY += 36f;

        // Create button
        if (ButtonRenderer.DrawButton(drawList, x, currentY, 180f, 36f, "Create Civilization",
            isPrimary: true))
        {
            if (!string.IsNullOrEmpty(state.CreateCivName) && state.CreateCivName.Length >= 3)
            {
                manager.RequestCivilizationAction("create", "", "", state.CreateCivName);
                state.CreateCivName = "";
            }
            else
            {
                api.ShowChatMessage("Civilization name must be 3-32 characters.");
            }
        }

        // Clear button
        if (ButtonRenderer.DrawButton(drawList, x + 190f, currentY, 80f, 36f, "Clear"))
        {
            state.CreateCivName = "";
        }

        currentY += 50f;

        // Info text
        TextRenderer.DrawInfoText(drawList, x, currentY, width,
            "Once created, you can invite 2-4 religions with different deities to join your civilization. " +
            "Work together to build a powerful alliance!"
        );

        currentY += 40f;

        return currentY - y;
    }
}
```

**Files Created:**
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationCreateRenderer.cs`

#### Task 2.5: Create CivilizationTabRenderer.cs
**File:** `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationTabRenderer.cs` (~80 lines)

**Purpose:** Coordinator renderer for civilization tab with sub-tabs

**Features:**
- Sub-tab control (Browse, My Civ, Invites, Create)
- Delegates to appropriate sub-renderer

**Pseudocode:**
```csharp
internal static class CivilizationTabRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivilizationState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // Sub-tabs
        var subTabs = new[] { "Browse", "My Civilization", "Invitations", "Create" };
        var newSubTab = TabControl.Draw(
            drawList, x, currentY, width, 36f,
            tabs: subTabs,
            selectedIndex: state.CurrentSubTab,
            tabSpacing: 4f
        );

        if (newSubTab != state.CurrentSubTab)
        {
            state.CurrentSubTab = newSubTab;

            // Request data for new tab
            switch (newSubTab)
            {
                case 0: // Browse
                    manager.RequestCivilizationList(state.DeityFilter);
                    break;
                case 1: // My Civilization
                case 2: // Invitations
                    manager.RequestCivilizationInfo("");
                    break;
            }
        }

        currentY += 44f;

        // Content area
        var contentHeight = height - (currentY - y);

        switch (state.CurrentSubTab)
        {
            case 0: // Browse
                CivilizationBrowseRenderer.Draw(manager, api, x, currentY, width, contentHeight);
                break;
            case 1: // My Civilization
                CivilizationManageRenderer.Draw(manager, api, x, currentY, width, contentHeight);
                break;
            case 2: // Invitations
                CivilizationInvitesRenderer.Draw(manager, api, x, currentY, width, contentHeight);
                break;
            case 3: // Create
                CivilizationCreateRenderer.Draw(manager, api, x, currentY, width, contentHeight);
                break;
        }

        return height;
    }
}
```

**Files Created:**
- `PantheonWars/GUI/UI/Renderers/Civilization/CivilizationTabRenderer.cs`

**Success Criteria:**
- ✅ All 5 renderer files compile
- ✅ Each renderer uses shared components
- ✅ No code duplication (DeityHelper, ColorPalette, etc.)
- ✅ Renderers follow established pattern
- ✅ All sub-tabs render correctly

---

### Phase 3: Integrate into BlessingDialog (2-3 hours)

**Goal:** Add main tab system and integrate civilization tab

#### Task 3.1: Add Main Tab Control
**File:** `PantheonWars/GUI/UI/BlessingUIRenderer.cs` (modify)

**Add main tab bar:**
```csharp
public static void Draw(BlessingDialogManager manager, ICoreClientAPI api)
{
    var drawList = ImGui.GetWindowDrawList();
    var windowPos = ImGui.GetWindowPos();
    var windowSize = ImGui.GetWindowSize();

    var x = windowPos.X + 16f;
    var y = windowPos.Y + 16f;
    var width = windowSize.X - 32f;
    var height = windowSize.Y - 32f;

    var currentY = y;

    // 1. Religion Header (always visible)
    var headerHeight = ReligionHeaderRenderer.Draw(manager, api, x, currentY, width);
    currentY += headerHeight + 8f;

    // 2. Main Tabs
    var mainTabs = new[] { "Blessings", "Manage Religion", "Civilization" };
    var newMainTab = TabControl.Draw(
        drawList, x, currentY, width, 36f,
        tabs: mainTabs,
        selectedIndex: manager.State.CurrentMainTab,
        tabSpacing: 4f
    );

    if (newMainTab != manager.State.CurrentMainTab)
    {
        manager.State.CurrentMainTab = newMainTab;

        // Request data for new tab
        if (newMainTab == 2) // Civilization tab
        {
            manager.RequestCivilizationList(manager.CivilizationState.DeityFilter);
            manager.RequestCivilizationInfo("");
        }
    }

    currentY += 44f;

    // 3. Tab Content
    var contentHeight = height - (currentY - y) - 8f;

    switch (manager.State.CurrentMainTab)
    {
        case 0: // Blessings
            DrawBlessingsTab(manager, api, x, currentY, width, contentHeight);
            break;
        case 1: // Manage Religion
            // TODO: Future work - consolidate religion management here
            TextRenderer.DrawInfoText(drawList, x, currentY, width,
                "Religion management coming soon!");
            break;
        case 2: // Civilization
            CivilizationTabRenderer.Draw(manager, api, x, currentY, width, contentHeight);
            break;
    }
}

private static void DrawBlessingsTab(
    BlessingDialogManager manager, ICoreClientAPI api,
    float x, float y, float width, float height)
{
    // Existing blessing tree rendering logic
    // (move existing code here)
    var currentY = y;

    // Blessing tree (split view)
    var treeHeight = height * 0.7f;
    currentY += BlessingTreeRenderer.Draw(manager, api, x, currentY, width, treeHeight);

    // Blessing info panel
    var infoPanelHeight = height * 0.3f - 50f;
    currentY += BlessingInfoRenderer.Draw(manager, api, x, currentY, width, infoPanelHeight);

    // Action buttons
    BlessingActionsRenderer.Draw(manager, api, x, currentY, width, 40f);
}
```

**Files Modified:**
- `PantheonWars/GUI/UI/BlessingUIRenderer.cs`

#### Task 3.2: Update ReligionHeaderRenderer (Optional)
**File:** `PantheonWars/GUI/UI/Renderers/ReligionHeaderRenderer.cs` (modify)

**Add civilization info display (optional):**
```csharp
// After progress bars, add civilization info if player is in one
if (manager.CivilizationState.MyCivilization != null)
{
    var civ = manager.CivilizationState.MyCivilization;

    currentY += 8f;

    // Civilization label
    drawList.AddText(ImGui.GetFont(), 12f,
        new Vector2(x + padding, currentY),
        ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
        "Civilization:"
    );

    // Civilization name
    drawList.AddText(ImGui.GetFont(), 14f,
        new Vector2(x + padding + 90f, currentY - 1f),
        ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
        civ.Name
    );

    // Member count badge
    drawList.AddCircleFilled(new Vector2(x + padding + 90f + nameWidth + 8f, currentY + 6f), 10f,
        ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown));
    drawList.AddText(ImGui.GetFont(), 11f,
        new Vector2(x + padding + 90f + nameWidth + 5f, currentY + 1f),
        ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
        $"{civ.MemberReligions.Count}"
    );

    currentY += 18f;
}
```

**Files Modified:**
- `PantheonWars/GUI/UI/Renderers/ReligionHeaderRenderer.cs` (optional)

**Success Criteria:**
- ✅ Main tab bar displays correctly
- ✅ Tab switching works
- ✅ Blessings tab still functional
- ✅ Civilization tab renders all sub-tabs
- ✅ Data requests trigger on tab change
- ✅ No visual glitches or overlaps

---

### Phase 4: Mark Old Files as Obsolete (1 hour)

**Goal:** Mark old civilization UI files as obsolete without deleting them yet

#### Task 4.1: Mark CivilizationDialog.cs as Obsolete
**File:** `PantheonWars/GUI/CivilizationDialog.cs` (modify)

**Add obsolete attribute:**
```csharp
/// <summary>
///     [OBSOLETE] Full-featured civilization management dialog with tabs
///     DEPRECATED: Civilization UI has been migrated to BlessingDialog (Civilization tab)
///     This file will be removed in a future update.
/// </summary>
[Obsolete("Use BlessingDialog Civilization tab instead. This dialog will be removed in a future update.")]
[ExcludeFromCodeCoverage]
public class CivilizationDialog : ModSystem
{
    // ... existing code ...
}
```

**Files Modified:**
- `PantheonWars/GUI/CivilizationDialog.cs`

#### Task 4.2: Mark CivilizationInfoOverlay.cs as Obsolete
**File:** `PantheonWars/GUI/CivilizationInfoOverlay.cs` (modify)

**Add obsolete attribute:**
```csharp
/// <summary>
///     [OBSOLETE] Compact HUD overlay showing civilization info
///     DEPRECATED: Civilization UI has been migrated to BlessingDialog (Civilization tab)
///     This file will be removed in a future update.
/// </summary>
[Obsolete("Use BlessingDialog Civilization tab instead. This overlay will be removed in a future update.")]
[ExcludeFromCodeCoverage]
public class CivilizationInfoOverlay : ModSystem
{
    // ... existing code ...
}
```

**Files Modified:**
- `PantheonWars/GUI/CivilizationInfoOverlay.cs`

#### Task 4.3: Disable Old Systems (Optional)
**File:** `PantheonWars/PantheonWarsSystem.cs` (modify)

**Optionally disable old systems:**
```csharp
// In StartClientSide() or appropriate init method
// Comment out or conditionally disable old civilization UI systems
// This prevents conflicts while keeping code for reference

// OLD: var civDialog = api.ModLoader.GetModSystem<CivilizationDialog>();
// OLD: var civOverlay = api.ModLoader.GetModSystem<CivilizationInfoOverlay>();
```

**Files Modified:**
- `PantheonWars/PantheonWarsSystem.cs` (optional)

**Success Criteria:**
- ✅ Obsolete warnings appear in code editor
- ✅ XML documentation indicates deprecation
- ✅ Old systems don't conflict with new implementation
- ✅ Code still compiles (warnings, not errors)

---

### Phase 5: Testing & Polish (1-2 hours)

**Goal:** Test all functionality and polish edge cases

#### Task 5.1: Manual Testing
**Test Cases:**
- [x] Main tab switching works (Blessings ↔ Civilization)
- [x] All 4 civilization sub-tabs render correctly
- [x] Browse tab: deity filter works, scrolling works, "View Details" switches tabs
  - [x] refresh
  - [x] filter
  - [X] scroll
  - [x] "View Details" 
- [ ] My Civilization tab: invite input works, kick button works
  - [ ] invite
    - **TEST NOTES: invitation did not work through ui, but did work through commands**
  - [ ] kick
    - **TEST NOTES: does not work**
- [ ] Invitations tab: invite list scrolls, accept/decline buttons work
    - **Testing notes: No invitations user interface to send invitations**
  - [ ] scrolls
  - [ ] accept
  - [ ] decline
- [x] Create tab: name input works, create button validates, clear button works
  - [x] name input
  - [x] clear
  - [x] create
- [ ] Data refreshes after actions (create, invite, accept, leave, kick, disband)
  - [x] create
  - [ ] invite
  - [ ] accept
  - [ ] kick
  - [x] disband
- [x] Event handlers update state correctly
- [x] No visual glitches (overlapping, incorrect sizing, etc.)
- [x] Shared components work correctly (tabs, buttons, inputs, scrolling)
- [x] Deity colors and titles display correctly

#### Task 5.2: Edge Case Testing
**Edge Cases:**
- [x] No civilizations exist (empty list)
- [x] Player not in a civilization (My Civ tab shows info message)
- [x] No pending invitations (Invites tab shows empty message)
- [ ] Civilization at capacity (4/4 members)
- [x] Long civilization names (32 characters)
- [x] Long religion names
- [ ] Many civilizations (scrolling performance)
- [ ] Many members (scrolling performance)
- [ ] Many invites (scrolling performance)

#### Task 5.3: Polish
**Improvements:**
- [ ] Add loading states ("Loading..." text while data fetches)
- [ ] Add error states (display error messages from packets)
- [ ] Add confirmation dialogs for destructive actions (kick, disband)
- [ ] Add tooltips for buttons and actions
- [ ] Add audio feedback (button clicks, tab switches)
- [ ] Ensure consistent spacing and alignment

**Success Criteria:**
- ✅ All test cases pass
- ✅ No crashes or exceptions
- ✅ UI is polished and responsive
- ✅ Performance is acceptable
- ✅ User experience matches or exceeds old implementation

---

### Phase 6: Cleanup & Documentation (1 hour)

**Goal:** Delete old files and update documentation

#### Task 6.1: Delete Obsolete Files
**Files to Delete:**
- `PantheonWars/GUI/CivilizationDialog.cs` (630 lines)
- `PantheonWars/GUI/CivilizationInfoOverlay.cs` (240 lines)

**Total Lines Removed:** 870 lines

#### Task 6.2: Update Documentation
**Files to Update:**
- [ ] Update `docs/topics/planning/civilization-system-implementation.md`
  - Mark Phase 3 Task 3.1 as complete
  - Mark Task 3.2 and 3.3 as obsolete (replaced by integrated approach)
  - Add note about migration to BlessingDialog
- [ ] Update `docs/topics/ui-design/ui-refactoring-progress.md`
  - Add Phase 6: Civilization UI Migration
  - Document files created, modified, deleted
  - Update total impact metrics
- [ ] Create `docs/topics/ui-design/civilization-tab-usage.md` (optional)
  - User guide for civilization tab
  - Screenshots and examples
  - Common workflows

#### Task 6.3: Update Comments and TODOs
**Search and remove:**
- [ ] Any TODOs referencing old CivilizationDialog
- [ ] Comments about future civilization UI work (now complete)
- [ ] Update any code that referenced old systems

**Success Criteria:**
- ✅ Old files deleted
- ✅ Documentation updated
- ✅ No broken references
- ✅ Clean commit history

---

## 📊 Expected Results

### Code Metrics

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| **Civilization UI Files** | 2 files (870 lines) | 0 standalone files | -870 lines |
| **New Renderer Files** | 0 | 5 files (~600 lines) | +600 lines |
| **Net Code Reduction** | - | - | **-270 lines** |
| **Code Duplication** | ~80 lines (deity helpers, colors) | 0 lines | -80 lines |
| **Shared Components Used** | 0 | 10 components | Reuse |

**Total Impact:**
- **Lines Removed:** 870 lines (2 standalone dialogs)
- **Lines Added:** ~600 lines (5 renderers + state + integration)
- **Net Reduction:** ~270 lines
- **Eliminated Duplication:** ~80 lines
- **Shared Component Reuse:** 10 components

### Architecture Improvements

**Before:**
- Standalone CivilizationDialog (monolithic, 630 lines)
- Standalone CivilizationInfoOverlay (redundant, 240 lines)
- Duplicate deity helper methods
- No state separation
- Manual tab rendering

**After:**
- Integrated into BlessingDialog (unified interface)
- State-based architecture (CivilizationState)
- Renderer pattern (5 focused renderers)
- Shared components (TabControl, ScrollableList, ButtonRenderer, etc.)
- Consistent with existing patterns
- No code duplication

### User Experience Improvements

**Before:**
- Separate dialogs for blessings and civilizations
- Need to remember different keybinds
- Context switching between interfaces
- Inconsistent UI patterns

**After:**
- Single unified interface for all progression systems
- Tab-based navigation (Blessings, Religion, Civilization)
- Consistent UI patterns and styling
- Better discoverability
- More polished and professional

---

## 🎯 Success Criteria

### Minimum Viable (Phase 1-3)
- ✅ Civilization tab appears in BlessingDialog
- ✅ All 4 sub-tabs render correctly
- ✅ Basic functionality works (browse, manage, invites, create)
- ✅ Event handlers update state
- ✅ Shared components used throughout
- ✅ No compilation errors

### Polished (Phase 4-5)
- ✅ Old files marked as obsolete
- ✅ All edge cases handled
- ✅ Loading and error states implemented
- ✅ Performance is acceptable
- ✅ Visual polish (spacing, alignment, colors)
- ✅ Tooltips and feedback

### Complete (Phase 6)
- ✅ Old files deleted
- ✅ Documentation updated
- ✅ Clean commit history
- ✅ No regressions in existing features

---

## 🚨 Risks & Mitigation

### Risk 1: Breaking Existing Functionality
**Mitigation:**
- Test blessings tab thoroughly after integration
- Keep old files until fully tested
- Incremental migration (phases)

### Risk 2: Performance Issues with Complex Layouts
**Mitigation:**
- Use viewport culling in ScrollableList
- Test with maximum data (many civilizations, members, invites)
- Profile rendering if issues arise

### Risk 3: Shared Component Limitations
**Mitigation:**
- Extend shared components if needed
- Keep renderer-specific logic in renderers
- Don't force-fit components where they don't make sense

### Risk 4: State Management Complexity
**Mitigation:**
- Clear state separation (CivilizationState)
- Well-defined data flow (events → state → renderers)
- Document state lifecycle

---

## 📋 Implementation Checklist

### Phase 1: Preparation & State Management
- [ ] Create CivilizationState.cs
- [ ] Integrate into BlessingDialogManager
- [ ] Add CurrentMainTab to BlessingDialogState
- [ ] Add civilization event handlers
- [ ] Subscribe to civilization events in BlessingDialog
- [ ] Test state updates

### Phase 2: Create Civilization Renderers
- [ ] Create CivilizationBrowseRenderer.cs
- [ ] Create CivilizationManageRenderer.cs
- [ ] Create CivilizationInvitesRenderer.cs
- [ ] Create CivilizationCreateRenderer.cs
- [ ] Create CivilizationTabRenderer.cs
- [ ] Test each renderer independently

### Phase 3: Integrate into BlessingDialog
- [ ] Add main tab control to BlessingUIRenderer
- [ ] Move blessing rendering to DrawBlessingsTab()
- [ ] Wire up civilization tab to CivilizationTabRenderer
- [ ] (Optional) Add civilization info to ReligionHeaderRenderer
- [ ] Test tab switching
- [ ] Test data flow

### Phase 4: Mark Old Files as Obsolete
- [ ] Add [Obsolete] attribute to CivilizationDialog.cs
- [ ] Add [Obsolete] attribute to CivilizationInfoOverlay.cs
- [ ] Update XML documentation
- [ ] (Optional) Disable old systems

### Phase 5: Testing & Polish
- [ ] Manual testing (all test cases)
- [ ] Edge case testing
- [ ] Add loading states
- [ ] Add error states
- [ ] Add confirmation dialogs
- [ ] Add tooltips
- [ ] Polish spacing and alignment

### Phase 6: Cleanup & Documentation
- [ ] Delete CivilizationDialog.cs
- [ ] Delete CivilizationInfoOverlay.cs
- [ ] Update civilization-system-implementation.md
- [ ] Update ui-refactoring-progress.md
- [ ] (Optional) Create civilization-tab-usage.md
- [ ] Remove TODOs and old references
- [ ] Final testing

---

## 📅 Timeline Estimate

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| **Phase 1** | State management | 2-3 hours |
| **Phase 2** | Civilization renderers | 6-8 hours |
| **Phase 3** | Integration | 2-3 hours |
| **Phase 4** | Mark obsolete | 1 hour |
| **Phase 5** | Testing & polish | 1-2 hours |
| **Phase 6** | Cleanup & docs | 1 hour |
| **Total** | **All phases** | **13-18 hours** |

**Conservative Estimate:** 16 hours (2 days full-time, or 1 week part-time)

**Fast Track:** 12 hours (skip optional tasks, minimal polish)

---

## 🔗 Dependencies

**Required:**
- ✅ UI refactoring complete (Phases 1-5)
- ✅ Shared components available
- ✅ BlessingDialog functional
- ✅ Civilization backend complete (Phase 1-2)

**Optional:**
- Phase 5 progress indicators (can integrate after)
- Religion management tab (future work)

---

## 📝 Notes

### Design Decisions

1. **Tab-Based Approach:** Easier to navigate, consistent with modern UI patterns
2. **Sub-Tabs for Civilization:** Keeps related features grouped, follows original design
3. **Renderer Pattern:** Maintains consistency with BlessingDialog architecture
4. **Shared Components:** Reduces code, improves maintainability
5. **State Separation:** Enables testing, clear data flow

### Future Enhancements

**Post-Migration (Optional):**
- Add "Manage Religion" tab to consolidate ReligionManagementOverlay
- Add civilization bonuses display
- Add civilization progression/levels UI
- Add civilization chat integration
- Add advanced filtering/searching

### Alternative Approaches Considered

**Option A: Keep Separate Dialogs**
- Pros: Less refactoring, no risk to existing code
- Cons: Fragmented UX, code duplication, inconsistent patterns
- **Verdict:** Rejected

**Option B: Single Overlay (No Tabs)**
- Pros: Simpler implementation
- Cons: Cluttered UI, poor organization
- **Verdict:** Rejected

**Option C: Tab-Based Integration (Selected)**
- Pros: Unified interface, clean organization, consistent architecture
- Cons: More initial work
- **Verdict:** **Selected**

---

## ✅ Final Checklist

Before marking as complete:
- [ ] All code compiles with 0 errors
- [ ] All test cases pass
- [ ] No regressions in blessing functionality
- [ ] Performance is acceptable
- [ ] Code follows established patterns
- [ ] Documentation updated
- [ ] Obsolete files deleted
- [ ] Commit history is clean
- [ ] Ready for code review

---

**Document Created:** 2025-12-01
**Author:** Claude (Code Assistant)
**Status:** Ready for Implementation
**Priority:** High
**Estimated Completion:** 2-3 days (part-time) or 2 days (full-time)