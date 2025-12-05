using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Renderers;
using PantheonWars.GUI.UI.Renderers.Civilization;
using PantheonWars.GUI.UI.Renderers.Religion;
using PantheonWars.Models;
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
    public static void Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        BlessingDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime,
        Action? onUnlockClicked,
        Action? onCloseClicked)
    {
        const float padding = 16f;
        const float tabHeight = 36f;

        // Get window position for screen-space drawing
        var windowPos = ImGui.GetWindowPos();

        var x = padding;
        var y = padding;
        var width = windowWidth - padding * 2;

        // === 1. RELIGION HEADER (Top Banner, always visible) ===
        // Top-level religion action buttons have been removed; only pass civilization callback
        var headerHeight = ReligionHeaderRenderer.Draw(
            manager, api,
            windowPos.X + x, windowPos.Y + y, width
        );
        y += headerHeight + 8f;

        // === 2. MAIN TABS ===
        var drawList = ImGui.GetWindowDrawList();
        var mainTabs = new[] { "Religion", "Blessings", "Civilization" };
        var newMainTab = TabControl.Draw(
            drawList,
            windowPos.X + x,
            windowPos.Y + y,
            width,
            tabHeight,
            mainTabs,
            (int)state.CurrentMainTab
        );

        if (newMainTab != (int)state.CurrentMainTab)
        {
            state.CurrentMainTab = (MainDialogTab)newMainTab;

            if (newMainTab == 0) // Religion tab
            {
                // Request both browse and my religion data
                manager.ReligionStateManager.State.IsBrowseLoading = true;
                manager.ReligionStateManager.RequestReligionList(manager.ReligionStateManager.State.DeityFilter);


                // Request player religion info (includes invitations if player has no religion)
                if (manager.HasReligion())
                {
                    manager.ReligionStateManager.State.IsMyReligionLoading = true;
                }
                else
                {
                    manager.ReligionStateManager.State.IsInvitesLoading = true;
                }

                manager.ReligionStateManager.RequestPlayerReligionInfo();
            }
            else if (newMainTab == 2) // Civilization tab
            {
                manager.RequestCivilizationList(manager.CivState.DeityFilter);
                manager.RequestCivilizationInfo();
            }
        }

        y += tabHeight + 8f;

        // === 3. TAB CONTENT ===
        var contentHeight = windowHeight - y - padding;

        switch (state.CurrentMainTab)
        {
            case MainDialogTab.ManageReligion: // Manage Religion
                ReligionTabRenderer.Draw(manager, api, windowPos.X + x, windowPos.Y + y, width, contentHeight);
                break;
            case MainDialogTab.Blessings: // Blessings
                DrawBlessingsTab(manager, api, windowPos.X + x, windowPos.Y + y, width, contentHeight,
                    windowWidth, windowHeight, deltaTime, onUnlockClicked, onCloseClicked);
                break;
            case MainDialogTab.Civilization: // Civilization
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
            ImGui.GetWindowPos().X + buttonX,
            ImGui.GetWindowPos().Y + buttonY,
            onUnlockClicked,
            onCloseClicked
        );

        // Update manager hover state
        manager.HoveringBlessingId = hoveringBlessingId;

        // Tooltips
        if (!string.IsNullOrEmpty(hoveringBlessingId))
        {
            var hoveringState = manager.ReligionStateManager.GetBlessingState(hoveringBlessingId);
            if (hoveringState != null)
            {
                var allBlessings = new Dictionary<string, Blessing>();
                foreach (var s in manager.ReligionStateManager.PlayerBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId))
                        allBlessings[s.Blessing.BlessingId] = s.Blessing;
                foreach (var s in manager.ReligionStateManager.ReligionBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId))
                        allBlessings[s.Blessing.BlessingId] = s.Blessing;

                var tooltipData = BlessingTooltipData.FromBlessingAndState(
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