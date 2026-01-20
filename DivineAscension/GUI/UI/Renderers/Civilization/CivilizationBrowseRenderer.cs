using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.Models.Civilization.Table;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renderer for browsing and viewing civilizations.
///     Follows separation of concerns pattern from ReligionBrowseRenderer.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationBrowseRenderer
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
    ///     Pure renderer: builds visuals from view model and emits UI events.
    ///     No state or side effects.
    /// </summary>
    /// <param name="vm">View model containing civilization data and layout info</param>
    /// <param name="isDeityDropdownOpen">Whether deity filter dropdown is currently open</param>
    /// <param name="drawList">ImGui draw list for rendering</param>
    /// <returns>Render result with events and final height</returns>
    public static CivilizationBrowseRenderResult Draw(
        CivilizationBrowseViewModel vm,
        bool isDeityDropdownOpen,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var currentY = vm.Y + TopPadding;
        var filterControlsY = currentY;  // Store filter controls Y position for dropdown menu

        // === FILTER CONTROLS ===
        DrawFilterControls(vm, isDeityDropdownOpen, filterControlsY, drawList, events);
        currentY += ComponentSpacing;

        // === CIVILIZATION TABLE ===
        var tableHeight = vm.Height - (currentY - vm.Y);
        var tableVm = new CivilizationTableViewModel(
            vm.Civilizations,
            vm.IsLoading,
            vm.ScrollY,
            vm.SelectedCivId,
            vm.X,
            currentY,
            vm.Width,
            tableHeight);

        var tableResult = CivilizationTableRenderer.Draw(tableVm, drawList);

        // Track whether dropdown consumed a click
        var dropdownConsumedClick = false;

        // === DROPDOWN MENU (z-ordered on top) ===
        if (isDeityDropdownOpen)
        {
            dropdownConsumedClick = DrawDropdownMenu(vm, filterControlsY, drawList, events);
        }

        // Translate table events to browse events - ONLY if dropdown didn't consume the click
        if (!dropdownConsumedClick)
        {
            foreach (var evt in tableResult.Events)
            {
                switch (evt)
                {
                    case ListEvent.ItemClicked ic:
                        // Row clicked → select and auto-navigate to detail view
                        events.Add(new BrowseEvent.Selected(ic.CivId, ic.NewScrollY));
                        break;
                    case ListEvent.ScrollChanged sc:
                        events.Add(new BrowseEvent.ScrollChanged(sc.NewScrollY));
                        break;
                }
            }
        }

        return new CivilizationBrowseRenderResult(events, vm.Height);
    }

    /// <summary>
    ///     Draw filter dropdown and refresh button
    /// </summary>
    private static void DrawFilterControls(
        CivilizationBrowseViewModel vm,
        bool isDropdownOpen,
        float y,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
        // Filter label
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_FILTER), vm.X, y);

        var selectedIndex = vm.GetCurrentFilterIndex();

        var dropdownX = vm.X + FilterLabelWidth;
        var dropdownY = y + DropdownYOffset;

        // Dropdown button
        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, DropdownWidth, DropdownHeight,
                vm.DeityFilters[selectedIndex], isDropdownOpen))
            events.Add(new BrowseEvent.DeityDropDownToggled(!isDropdownOpen));

        // Refresh button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_REFRESH),
                dropdownX + DropdownWidth + RefreshButtonMargin, dropdownY, RefreshButtonWidth, DropdownHeight,
                false, !vm.IsLoading))
            events.Add(new BrowseEvent.RefreshClicked());
    }

    /// <summary>
    ///     Draw dropdown menu overlay when open
    /// </summary>
    /// <returns>True if the dropdown consumed a mouse click</returns>
    private static bool DrawDropdownMenu(
        CivilizationBrowseViewModel vm,
        float controlsY,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
        var selectedIndex = vm.GetCurrentFilterIndex();
        var dropdownX = vm.X + FilterLabelWidth;
        var dropdownY = controlsY + DropdownYOffset;

        // Draw menu visual
        Dropdown.DrawMenuVisual(drawList, dropdownX, dropdownY, DropdownWidth, DropdownHeight,
            vm.DeityFilters, selectedIndex, MenuItemHeight);

        // Handle menu interaction
        var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(dropdownX, dropdownY,
            DropdownWidth, DropdownHeight,
            vm.DeityFilters, selectedIndex, MenuItemHeight);

        if (shouldClose)
        {
            events.Add(new BrowseEvent.DeityDropDownToggled(false));

            // Update filter if selection changed
            if (newIndex != selectedIndex)
            {
                var newFilter = newIndex == 0 ? string.Empty : vm.DeityFilters[newIndex];
                events.Add(new BrowseEvent.DeityFilterChanged(newFilter));
            }
        }

        return clickConsumed;
    }
}