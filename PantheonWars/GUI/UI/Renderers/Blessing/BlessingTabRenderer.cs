using System;
using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.Models;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Blessing;

internal static class BlessingTabRenderer
{
    internal static void DrawBlessingsTab(
        GuiDialogManager manager,
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
        BlessingInfoRenderer.Draw(manager, x, infoY, width, infoPanelHeight);

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
                var allBlessings = new Dictionary<string, PantheonWars.Models.Blessing>();
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