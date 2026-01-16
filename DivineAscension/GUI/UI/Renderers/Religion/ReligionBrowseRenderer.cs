using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Browse;
using DivineAscension.GUI.Models.Religion.Table;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for browsing and joining religions
///     Migrates functionality from ReligionBrowserOverlay
/// </summary>
internal static class ReligionBrowseRenderer
{
    // Layout dimensions
    private const float TopPadding = 8f;
    private const float FilterLabelWidth = 120f;
    private const float DropdownWidth = 200f;
    private const float DropdownHeight = 30f;
    private const float DropdownYOffset = -6f;
    private const float RefreshButtonWidth = 100f;
    private const float RefreshButtonMargin = 12f;
    private const float ComponentSpacing = 40f;
    private const float MenuItemHeight = 30f;

    /// <summary>
    ///     Pure renderer: builds visuals from the view model and emits UI events. No state or side effects.
    /// </summary>
    public static ReligionBrowseRenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var currentY = viewModel.Y + TopPadding;
        var filterControlsY = currentY;

        // === FILTER CONTROLS ===
        DrawFilterControls(viewModel, filterControlsY, drawList, events);
        currentY += ComponentSpacing;

        // === RELIGION TABLE ===
        var tableHeight = viewModel.Height - (currentY - viewModel.Y);
        var tableVm = new ReligionTableViewModel(
            Religions: viewModel.Religions,
            IsLoading: viewModel.IsLoading,
            ScrollY: viewModel.ScrollY,
            SelectedReligionUID: viewModel.SelectedReligionUID,
            X: viewModel.X,
            Y: currentY,
            Width: viewModel.Width,
            Height: tableHeight);

        var tableResult = ReligionTableRenderer.Draw(tableVm, drawList);

        // Track whether dropdown consumed a click
        var dropdownConsumedClick = false;

        // === DROPDOWN MENU (z-ordered on top) ===
        if (viewModel.IsDeityDropdownOpen)
        {
            dropdownConsumedClick = DrawDropdownMenu(viewModel, filterControlsY, drawList, events);
        }

        // Translate list events → browse events - ONLY if dropdown didn't consume the click
        if (!dropdownConsumedClick)
        {
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
        }

        return new ReligionBrowseRenderResult(events, null, viewModel.Height);
    }

    /// <summary>
    ///     Draw filter dropdown and refresh button
    /// </summary>
    private static void DrawFilterControls(
        ReligionBrowseViewModel vm,
        float y,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
        // Filter label
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_FILTER), vm.X, y);

        var selectedIndex = vm.GetCurrentFilterIndex();
        if (selectedIndex == -1) selectedIndex = 0;

        var dropdownX = vm.X + FilterLabelWidth;
        var dropdownY = y + DropdownYOffset;

        // Dropdown button
        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, DropdownWidth, DropdownHeight,
                vm.DomainFilters[selectedIndex], vm.IsDeityDropdownOpen))
            events.Add(new BrowseEvent.DeityDropDownToggled(!vm.IsDeityDropdownOpen));

        // Refresh button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_REFRESH),
                dropdownX + DropdownWidth + RefreshButtonMargin, dropdownY, RefreshButtonWidth, DropdownHeight,
                false, !vm.IsLoading))
            events.Add(new BrowseEvent.RefreshClicked());
    }

    /// <summary>
    ///     Draw dropdown menu overlay when open
    /// </summary>
    /// <returns>True if the dropdown consumed a mouse click</returns>
    private static bool DrawDropdownMenu(
        ReligionBrowseViewModel vm,
        float controlsY,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
        var selectedIndex = vm.GetCurrentFilterIndex();
        if (selectedIndex == -1) selectedIndex = 0;

        var dropdownX = vm.X + FilterLabelWidth;
        var dropdownY = controlsY + DropdownYOffset;

        // Draw menu visual
        Dropdown.DrawMenuVisual(drawList, dropdownX, dropdownY, DropdownWidth, DropdownHeight,
            vm.DomainFilters, selectedIndex, MenuItemHeight);

        // Handle menu interaction
        var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(dropdownX, dropdownY,
            DropdownWidth, DropdownHeight,
            vm.DomainFilters, selectedIndex, MenuItemHeight);

        if (shouldClose)
        {
            events.Add(new BrowseEvent.DeityDropDownToggled(false));

            // Update filter if selection changed
            if (newIndex != selectedIndex)
            {
                var newFilter = newIndex == 0 ? string.Empty : vm.DomainFilters[newIndex];
                events.Add(new BrowseEvent.DeityFilterChanged(newFilter));
            }
        }

        return clickConsumed;
    }
}
