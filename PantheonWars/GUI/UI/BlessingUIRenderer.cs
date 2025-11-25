using System;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.UI.Renderers;
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
        const float infoPanelHeight = 200f;
        const float actionButtonHeight = 36f;
        const float actionButtonPadding = 16f;

        // Get window position for screen-space drawing
        var windowPos = ImGui.GetWindowPos();

        var currentX = padding;
        var currentY = padding;
        var contentWidth = windowWidth - padding * 2;

        // Track hovering state across renderers
        string? hoveringBlessingId = null;

        // === 1. RELIGION HEADER (Top Banner) ===
        var usedHeight = ReligionHeaderRenderer.Draw(
            manager, api,
            windowPos.X + currentX, windowPos.Y + currentY, contentWidth,
            onChangeReligionClicked,
            onManageReligionClicked,
            onLeaveReligionClicked,
            onManageCivilizationClicked
        );
        currentY += usedHeight + padding;

        // === 2. BLESSING TREE (Split View: Player Left, Religion Right) ===
        var treeHeight = windowHeight - currentY - infoPanelHeight - padding * 2;

        BlessingTreeRenderer.Draw(
            manager, api,
            windowPos.X + currentX, windowPos.Y + currentY, contentWidth, treeHeight,
            deltaTime,
            ref hoveringBlessingId
        );
        currentY += treeHeight + padding;

        // === 3. BLESSING INFO PANEL (Bottom Panel) ===
        BlessingInfoRenderer.Draw(
            manager, api,
            windowPos.X + currentX, windowPos.Y + currentY, contentWidth, infoPanelHeight
        );

        // === 4. ACTION BUTTONS (Overlay Bottom-Right) ===
        var buttonY = windowHeight - actionButtonHeight - actionButtonPadding;
        var buttonX = windowWidth - actionButtonPadding;

        BlessingActionsRenderer.Draw(
            manager, api,
            windowPos.X + buttonX, windowPos.Y + buttonY,
            onUnlockClicked,
            onCloseClicked
        );

        // Update manager's hovering state
        manager.HoveringBlessingId = hoveringBlessingId;

        // === 5. TOOLTIPS (Rendered Last, On Top of Everything) ===
        if (!string.IsNullOrEmpty(hoveringBlessingId))
        {
            var hoveringState = manager.GetBlessingState(hoveringBlessingId);
            if (hoveringState != null)
            {
                // Get all blessings for registry (needed for prerequisite names)
                var allBlessings = new System.Collections.Generic.Dictionary<string, Models.Blessing>();
                foreach (var state in manager.PlayerBlessingStates.Values)
                    if (!allBlessings.ContainsKey(state.Blessing.BlessingId))
                        allBlessings[state.Blessing.BlessingId] = state.Blessing;
                foreach (var state in manager.ReligionBlessingStates.Values)
                    if (!allBlessings.ContainsKey(state.Blessing.BlessingId))
                        allBlessings[state.Blessing.BlessingId] = state.Blessing;

                // Build tooltip data
                var tooltipData = Models.BlessingTooltipData.FromBlessingAndState(
                    hoveringState.Blessing,
                    hoveringState,
                    allBlessings
                );

                // Get mouse position
                var mousePos = ImGui.GetMousePos();

                // Render tooltip
                TooltipRenderer.Draw(
                    tooltipData,
                    mousePos.X,
                    mousePos.Y,
                    windowWidth,
                    windowHeight
                );
            }
        }
    }
}