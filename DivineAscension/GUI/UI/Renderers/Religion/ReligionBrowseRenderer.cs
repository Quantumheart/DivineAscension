using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Browse;
using DivineAscension.GUI.Models.Religion.Table;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Renderers.Components;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for browsing and joining religions
///     Migrates functionality from ReligionBrowserOverlay
/// </summary>
internal static class ReligionBrowseRenderer
{
    /// <summary>
    /// Pure renderer: builds visuals from the view model and emits UI events. No state or side effects.
    /// </summary>
    public static ReligionBrowseRenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var currentY = y;

        // === DEITY FILTER TABS === (Issue #71: 36px height, 4px spacing)
        const float tabHeight = 36f;

        var currentSelectedIndex = viewModel.GetCurrentFilterIndex();
        if (currentSelectedIndex == -1) currentSelectedIndex = 0; // Default to "All"

        var newSelectedIndex = TabControl.Draw(
            drawList,
            x,
            currentY,
            width,
            tabHeight,
            viewModel.DeityFilters,
            currentSelectedIndex);

        if (newSelectedIndex != currentSelectedIndex)
        {
            var newFilter = viewModel.DeityFilters[newSelectedIndex];
            events.Add(new BrowseEvent.DeityFilterChanged(newFilter));
        }

        currentY += tabHeight + 8f;

        // === RELIGION TABLE ===
        var tableHeight = height - (currentY - y);
        var tableVm = new ReligionTableViewModel(
            Religions: viewModel.Religions,
            IsLoading: viewModel.IsLoading,
            ScrollY: viewModel.ScrollY,
            SelectedReligionUID: viewModel.SelectedReligionUID,
            X: x,
            Y: currentY,
            Width: width,
            Height: tableHeight);

        var tableResult = ReligionTableRenderer.Draw(tableVm, drawList);

        // Translate list events → browse events
        foreach (var le in tableResult.Events)
        {
            switch (le)
            {
                case ListEvent.ScrollChanged sc:
                    events.Add(new BrowseEvent.ScrollChanged(sc.NewScrollY));
                    break;
                case ListEvent.ItemClicked ic:
                    events.Add(new BrowseEvent.Selected(ic.ReligionUID, ic.NewScrollY));
                    break;
            }
        }

        return new ReligionBrowseRenderResult(events, null, height);
    }
}