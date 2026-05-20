using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Actions;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.GUI.Models.Blessing.Tree;
using DivineAscension.GUI.UI.Renderers.Blessing.Info;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

[ExcludeFromCodeCoverage]
internal static class BlessingTabRenderer
{
    internal static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
    {
        const float infoPanelHeight = 200f;
        const float padding = 16f;
        const float actionButtonHeight = 36f;
        const float actionButtonPadding = 16f;
        const float strapSpacing = 8f;

        string? hoveringBlessingId = null;

        var topY = vm.Y;
        CrossDeitySummaryRenderer.Draw(vm.X, topY, vm.Width, vm.DeitySummaries);
        topY += CrossDeitySummaryRenderer.Height + strapSpacing;

        var requestedDeity = DeitySelectorRenderer.Draw(vm.X, topY, vm.ActiveDeity, vm.PatronDomain);
        topY += DeitySelectorRenderer.Height + strapSpacing;

        var consumedTop = topY - vm.Y;
        var treeHeight = vm.Height - infoPanelHeight - padding - consumedTop;
        var treeVm = new BlessingTreeViewModel(
            vm.PlayerTreeScrollState,
            vm.ReligionTreeScrollState,
            vm.PlayerBlessingStates,
            vm.ReligionBlessingStates,
            vm.X, topY, vm.Width, treeHeight,
            vm.DeltaTime,
            vm.SelectedBlessingId,
            vm.PlayerFavor,
            vm.ReligionPrestige
        );

        var treeResult = BlessingTreeRenderer.Draw(treeVm);

        foreach (var ev in treeResult.Events)
            if (ev is TreeEvent.Hovered hovered)
                hoveringBlessingId = hovered.BlessingId;

        var infoY = topY + treeHeight + padding;
        var combinedStates = new Dictionary<string, BlessingNodeState>();
        foreach (var kv in vm.PlayerBlessingStates)
            combinedStates.TryAdd(kv.Key, kv.Value);
        foreach (var kv in vm.ReligionBlessingStates)
            combinedStates.TryAdd(kv.Key, kv.Value);

        var infoVm = new BlessingInfoViewModel(
            vm.SelectedBlessingState,
            combinedStates,
            vm.X,
            infoY,
            vm.Width,
            infoPanelHeight,
            vm.PlayerFavor,
            vm.ReligionPrestige);
        BlessingInfoRenderer.Draw(infoVm);

        var buttonY = vm.WindowHeight - actionButtonHeight - actionButtonPadding;
        var buttonX = vm.WindowWidth - actionButtonPadding;
        var actionsVm = new BlessingActionsViewModel(
            vm.SelectedBlessingState,
            ImGui.GetWindowPos().X + buttonX,
            ImGui.GetWindowPos().Y + buttonY,
            vm.PlayerFavor,
            vm.ReligionPrestige
        );

        var actionsResult = BlessingActionsRenderer.Draw(actionsVm);

        if (!string.IsNullOrEmpty(hoveringBlessingId))
        {
            vm.PlayerBlessingStates.TryGetValue(hoveringBlessingId!, out var hoverPlayerState);
            vm.ReligionBlessingStates.TryGetValue(hoveringBlessingId!, out var hoverReligionState);
            var hoveringState = hoverPlayerState ?? hoverReligionState;
            if (hoveringState != null)
            {
                var allBlessings = new Dictionary<string, DivineAscension.Models.Blessing>();
                foreach (var s in vm.PlayerBlessingStates.Values)
                    allBlessings.TryAdd(s.Blessing.BlessingId, s.Blessing);
                foreach (var s in vm.ReligionBlessingStates.Values)
                    allBlessings.TryAdd(s.Blessing.BlessingId, s.Blessing);

                var tooltipData = BlessingTooltipData.FromBlessingAndState(
                    hoveringState.Blessing,
                    hoveringState,
                    allBlessings
                );

                var mousePos = ImGui.GetMousePos();
                TooltipRenderer.Draw(tooltipData, mousePos.X, mousePos.Y, vm.WindowWidth, vm.WindowHeight);
            }
        }

        return new BlessingTabRenderResult(
            treeResult.Events,
            actionsResult.Events,
            hoveringBlessingId,
            vm.Height,
            requestedDeity
        );
    }

}
