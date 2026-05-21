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

        // III.ii — Blessings hosts only the personal tree. The religion-side tree
        // moved to I.iii — Vows of the Order. The Selected/Info/Actions panels
        // below scope to PlayerBlessingStates so a selection lingering from the
        // Vows page can't drive an action on this one.
        var treeResult = BlessingTreeRenderer.Draw(treeVm, BlessingKind.Player);

        foreach (var ev in treeResult.Events)
            if (ev is TreeEvent.Hovered hovered)
                hoveringBlessingId = hovered.BlessingId;

        var infoY = topY + treeHeight + padding;
        var playerOnlyStates = new Dictionary<string, BlessingNodeState>(vm.PlayerBlessingStates);

        var personalSelected = vm.SelectedBlessingState != null
                               && vm.SelectedBlessingState.Blessing.Kind == BlessingKind.Player
            ? vm.SelectedBlessingState
            : null;

        var infoVm = new BlessingInfoViewModel(
            personalSelected,
            playerOnlyStates,
            vm.X,
            infoY,
            vm.Width,
            infoPanelHeight,
            vm.PlayerFavor,
            vm.ReligionPrestige);
        BlessingInfoRenderer.Draw(infoVm);

        // Action buttons anchor to the bottom-right of the content rect (not the
        // whole window). vm.X / vm.Y are already absolute screen-space coords now
        // that the layout coordinator hands renderers a content-only rect.
        var buttonX = vm.X + vm.Width - actionButtonPadding;
        var buttonY = vm.Y + vm.Height - actionButtonHeight - actionButtonPadding;
        var actionsVm = new BlessingActionsViewModel(
            personalSelected,
            buttonX,
            buttonY,
            vm.PlayerFavor,
            vm.ReligionPrestige,
            isReligionFounder: false
        );

        var actionsResult = BlessingActionsRenderer.Draw(actionsVm);

        if (!string.IsNullOrEmpty(hoveringBlessingId)
            && vm.PlayerBlessingStates.TryGetValue(hoveringBlessingId!, out var hoveringState)
            && hoveringState != null)
        {
            var allBlessings = new Dictionary<string, DivineAscension.Models.Blessing>();
            foreach (var s in vm.PlayerBlessingStates.Values)
                allBlessings.TryAdd(s.Blessing.BlessingId, s.Blessing);

            var tooltipData = BlessingTooltipData.FromBlessingAndState(
                hoveringState.Blessing,
                hoveringState,
                allBlessings
            );

            var mousePos = ImGui.GetMousePos();
            TooltipRenderer.Draw(tooltipData, mousePos.X, mousePos.Y, vm.WindowWidth, vm.WindowHeight);
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
