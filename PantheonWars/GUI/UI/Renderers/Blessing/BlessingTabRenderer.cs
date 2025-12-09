using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Models.Blessing.Actions;
using PantheonWars.GUI.Models.Blessing.Info;
using PantheonWars.GUI.Models.Blessing.Tab;
using PantheonWars.GUI.Models.Blessing.Tree;
using PantheonWars.GUI.UI.Renderers.Blessing.Info;
using PantheonWars.Models;

namespace PantheonWars.GUI.UI.Renderers.Blessing;

internal static class BlessingTabRenderer
{
    internal static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
    {
        const float infoPanelHeight = 200f;
        const float padding = 16f;
        const float actionButtonHeight = 36f;
        const float actionButtonPadding = 16f;

        // Track hovering state
        string? hoveringBlessingId = null;

        // Blessing Tree (split view)
        var treeHeight = vm.Height - infoPanelHeight - padding;
        var treeVm = new BlessingTreeViewModel(
            vm.PlayerTreeScrollState,
            vm.ReligionTreeScrollState,
            vm.PlayerBlessingStates,
            vm.ReligionBlessingStates,
            vm.X, vm.Y, vm.Width, treeHeight,
            vm.DeltaTime,
            vm.SelectedBlessingId
        );

        var treeResult = BlessingTreeRenderer.Draw(treeVm);

        // Extract hovering state from tree events
        foreach (var ev in treeResult.Events)
            if (ev is BlessingTreeEvent.BlessingHovered hovered)
                hoveringBlessingId = hovered.BlessingId;

        // Info Panel
        var infoY = vm.Y + treeHeight + padding;
        var combinedStates = new Dictionary<string, BlessingNodeState>();
        foreach (var kv in vm.PlayerBlessingStates)
            if (!combinedStates.ContainsKey(kv.Key))
                combinedStates[kv.Key] = kv.Value;
        foreach (var kv in vm.ReligionBlessingStates)
            if (!combinedStates.ContainsKey(kv.Key))
                combinedStates[kv.Key] = kv.Value;

        var infoVm = new BlessingInfoViewModel(
            vm.SelectedBlessingState,
            combinedStates,
            vm.X,
            infoY,
            vm.Width,
            infoPanelHeight);
        var infoResult = BlessingInfoRenderer.Draw(infoVm);

        // Action Buttons (overlay bottom-right)
        var buttonY = vm.WindowHeight - actionButtonHeight - actionButtonPadding;
        var buttonX = vm.WindowWidth - actionButtonPadding;
        var actionsVm = new BlessingActionsViewModel(
            vm.SelectedBlessingState,
            ImGui.GetWindowPos().X + buttonX,
            ImGui.GetWindowPos().Y + buttonY
        );

        var actionsResult = BlessingActionsRenderer.Draw(actionsVm);

        // Tooltips (side effect - rendering only, no state changes)
        if (!string.IsNullOrEmpty(hoveringBlessingId))
        {
            vm.PlayerBlessingStates.TryGetValue(hoveringBlessingId!, out var hoverPlayerState);
            vm.ReligionBlessingStates.TryGetValue(hoveringBlessingId!, out var hoverReligionState);
            var hoveringState = hoverPlayerState ?? hoverReligionState;
            if (hoveringState != null)
            {
                var allBlessings = new Dictionary<string, PantheonWars.Models.Blessing>();
                foreach (var s in vm.PlayerBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId))
                        allBlessings[s.Blessing.BlessingId] = s.Blessing;
                foreach (var s in vm.ReligionBlessingStates.Values)
                    if (!allBlessings.ContainsKey(s.Blessing.BlessingId))
                        allBlessings[s.Blessing.BlessingId] = s.Blessing;

                var tooltipData = BlessingTooltipData.FromBlessingAndState(
                    hoveringState.Blessing,
                    hoveringState,
                    allBlessings
                );

                var mousePos = ImGui.GetMousePos();
                TooltipRenderer.Draw(tooltipData, mousePos.X, mousePos.Y, vm.WindowWidth, vm.WindowHeight);
            }
        }

        // Return result with all events
        return new BlessingTabRenderResult(
            treeResult.Events,
            actionsResult.Events,
            hoveringBlessingId,
            vm.Height
        );
    }
}