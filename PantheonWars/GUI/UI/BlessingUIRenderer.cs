using System;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Renderers;
using PantheonWars.GUI.UI.Renderers.Civilization;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI;

/// <summary>
///     Central coordinator that orchestrates all blessing UI renderers
///     Follows XSkillsGilded pattern - calls renderers in correct order with proper layout
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingUIRenderer
{
    /// <summary>
    ///     Draw the complete blessing UI
    /// </summary>
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Total window width</param>
    /// <param name="windowHeight">Total window height</param>
    /// <param name="deltaTime">Time elapsed since last frame (for animations)</param>
    /// <param name="onUnlockClicked">Callback when unlock button clicked</param>
    /// <param name="onCloseClicked">Callback when close button clicked</param>
    /// <param name="onChangeReligionClicked">Callback when Change Religion button clicked</param>
    /// <param name="onManageReligionClicked">Callback when Manage Religion button clicked</param>
    /// <param name="onLeaveReligionClicked">Callback when Leave Religion button clicked</param>
    /// <param name="onManageCivilizationClicked">Callback when Manage Civilization button clicked</param>
    public static void Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        BlessingDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime,
        Action? onUnlockClicked,
        Action? onCloseClicked,
        Action? onChangeReligionClicked = null,
        Action? onManageReligionClicked = null,
        Action? onLeaveReligionClicked = null,
        Action? onManageCivilizationClicked = null)
    {
        const float padding = 16f;
        const float tabHeight = 36f;

        // Get window position for screen-space drawing
        var windowPos = ImGui.GetWindowPos();

        var x = padding;
        var y = padding;
        var width = windowWidth - padding * 2;

        // === 1. RELIGION HEADER (Top Banner, always visible) ===
        var headerHeight = ReligionHeaderRenderer.Draw(
            manager, api,
            windowPos.X + x, windowPos.Y + y, width,
            onChangeReligionClicked,
            onManageReligionClicked,
            onLeaveReligionClicked,
            onManageCivilizationClicked
        );
        y += headerHeight + 8f;

        // === 2. MAIN TABS ===
        var drawList = ImGui.GetWindowDrawList();
        var mainTabs = new[] { "Blessings", "Manage Religion", "Civilization" };
        var newMainTab = TabControl.Draw(
            drawList,
            windowPos.X + x,
            windowPos.Y + y,
            width,
            tabHeight,
            mainTabs,
            state.CurrentMainTab,
            4f
        );

        if (newMainTab != state.CurrentMainTab)
        {
            state.CurrentMainTab = newMainTab;

            if (newMainTab == 2) // Civilization tab
            {
                manager.RequestCivilizationList(manager.CivState.DeityFilter);
                manager.RequestCivilizationInfo("");
            }
        }

        y += tabHeight + 8f;

        // === 3. TAB CONTENT ===
        var contentHeight = windowHeight - y - padding;

        switch (state.CurrentMainTab)
        {
            case 0: // Blessings
                DrawBlessingsTab(manager, api, windowPos.X + x, windowPos.Y + y, width, contentHeight,
                    windowWidth, windowHeight, deltaTime, onUnlockClicked, onCloseClicked);
                break;
            case 1: // Manage Religion (placeholder)
                TextRenderer.DrawInfoText(drawList,
                    "Religion management coming soon!",
                    windowPos.X + x,
                    windowPos.Y + y,
                    width,
                    14f);
                break;
            case 2: // Civilization
                CivilizationTabRenderer.Draw(manager, api, windowPos.X + x, windowPos.Y + y, width, contentHeight);
                break;
        }
    }

    private static void DrawBlessingsTab(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x,
        float y,
        float width,
        float height,
        int windowWidth,
        int windowHeight,
        float deltaTime,
        Action? onUnlockClicked,
        Action? onCloseClicked)
    {
        const float infoPanelHeight = 200f;
        const float padding = 16f;
        const float actionButtonHeight = 36f;
        const float actionButtonPadding = 16f;

        // Track hovering state
        string? hoveringBlessingId = null;

        // Blessing Tree (split view)
        var treeHeight = height - infoPanelHeight - padding;
        BlessingTreeRenderer.Draw(
            manager, api,
            x, y, width, treeHeight,
            deltaTime,
            ref hoveringBlessingId
        );

        // Info Panel
        var infoY = y + treeHeight + padding;
        BlessingInfoRenderer.Draw(manager, api, x, infoY, width, infoPanelHeight);

        // Action Buttons (overlay bottom-right)
        var buttonY = windowHeight - actionButtonHeight - actionButtonPadding;
        var buttonX = windowWidth - actionButtonPadding;
        BlessingActionsRenderer.Draw(
            manager, api,
            x: ImGui.GetWindowPos().X + buttonX,
            y: ImGui.GetWindowPos().Y + buttonY,
            onUnlockClicked,
            onCloseClicked
        );

        // Update manager hover state
        manager.HoveringBlessingId = hoveringBlessingId;

        // Tooltips
        if (!string.IsNullOrEmpty(hoveringBlessingId))
        {
            var hoveringState = manager.GetBlessingState(hoveringBlessingId);
            if (hoveringState != null)
            {
                var allBlessings = new System.Collections.Generic.Dictionary<string, Models.Blessing>();
                foreach (var s in manager.PlayerBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId)) allBlessings[s.Blessing.BlessingId] = s.Blessing;
                foreach (var s in manager.ReligionBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId)) allBlessings[s.Blessing.BlessingId] = s.Blessing;

                var tooltipData = Models.BlessingTooltipData.FromBlessingAndState(
                    hoveringState.Blessing,
                    hoveringState,
                    allBlessings
                );

                var mousePos = ImGui.GetMousePos();
                TooltipRenderer.Draw(tooltipData, mousePos.X, mousePos.Y, windowWidth, windowHeight);
            }
        }
    }
}