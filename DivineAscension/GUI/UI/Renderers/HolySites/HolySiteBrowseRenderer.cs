using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using CivListEvent = DivineAscension.GUI.Events.Civilization.ListEvent;
using DivineAscension.GUI.Events.HolySite;
using DivineAscension.GUI.Models.HolySite.Browse;
using DivineAscension.GUI.Models.HolySite.Table;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.HolySites;

/// <summary>
///     Renderer for browsing holy sites.
///     Shows a table of all holy sites grouped by religion.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HolySiteBrowseRenderer
{
    // Layout dimensions
    private const float TopPadding = 8f;
    private const float RefreshButtonWidth = 100f;
    private const float RefreshButtonHeight = 30f;
    private const float ComponentSpacing = 40f;

    /// <summary>
    ///     Pure renderer: builds visuals from the view model and emits UI events. No state or side effects.
    /// </summary>
    public static HolySiteBrowseRenderResult Draw(
        HolySiteBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var currentY = viewModel.Y + TopPadding;
        var controlsY = currentY;

        // === HEADER CONTROLS ===
        DrawHeaderControls(viewModel, controlsY, drawList, events);
        currentY += ComponentSpacing;

        // === HOLY SITES TABLE ===
        var tableHeight = viewModel.Height - (currentY - viewModel.Y);

        // Flatten sites from dictionary to list for table
        var allSites = new List<Network.HolySite.HolySiteResponsePacket.HolySiteInfo>();
        foreach (var sites in viewModel.SitesByReligion.Values)
        {
            allSites.AddRange(sites);
        }

        var tableVm = new HolySiteTableViewModel(
            x: viewModel.X,
            y: currentY,
            width: viewModel.Width,
            height: tableHeight,
            sites: allSites,
            selectedSiteUID: viewModel.SelectedSiteUID,
            isLoading: viewModel.IsLoading,
            scrollY: viewModel.ScrollY);

        var tableResult = HolySiteTableRenderer.Draw(tableVm, drawList);

        // Translate list events → browse events
        foreach (var le in tableResult.Events)
        {
            switch (le)
            {
                case CivListEvent.ScrollChanged sc:
                    events.Add(new BrowseEvent.ScrollChanged(sc.NewScrollY));
                    break;
                case CivListEvent.ItemClicked ic:
                    events.Add(new BrowseEvent.Selected(ic.CivId, ic.NewScrollY));
                    break;
            }
        }

        // Display error message if present
        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            DrawErrorMessage(viewModel, drawList);
        }

        return new HolySiteBrowseRenderResult(events, viewModel.Height);
    }

    /// <summary>
    ///     Draw header with title and refresh button
    /// </summary>
    private static void DrawHeaderControls(
        HolySiteBrowseViewModel vm,
        float y,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
        // Title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_TITLE);
        TextRenderer.DrawLabel(drawList, titleText, vm.X, y);

        // Refresh button (right-aligned)
        var refreshButtonX = vm.X + vm.Width - RefreshButtonWidth;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_REFRESH),
                refreshButtonX, y - 6f, RefreshButtonWidth, RefreshButtonHeight,
                false, !vm.IsLoading))
        {
            events.Add(new BrowseEvent.RefreshClicked());
        }
    }

    /// <summary>
    ///     Draw error message if present
    /// </summary>
    private static void DrawErrorMessage(HolySiteBrowseViewModel vm, ImDrawListPtr drawList)
    {
        var errorText = vm.ErrorMsg ?? "";
        var textSize = ImGui.CalcTextSize(errorText);
        var errorX = vm.X + (vm.Width - textSize.X) / 2f;
        var errorY = vm.Y + vm.Height - 40f;
        var errorColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(new System.Numerics.Vector2(errorX, errorY), errorColor, errorText);
    }
}
